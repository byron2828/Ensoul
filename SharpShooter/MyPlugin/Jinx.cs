namespace SharpShooter.MyPlugin
{
    #region

    using System;
    using System.Linq;

    using EnsoulSharp;
    using EnsoulSharp.SDK;
    using EnsoulSharp.SDK.MenuUI.Values;
    using EnsoulSharp.SDK.Prediction;
    using EnsoulSharp.SDK.Events;
    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using Keys = System.Windows.Forms.Keys;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    public class Jinx : MyLogic
    {
        private static float bigGunRange { get; set; }
        private static float rCoolDown { get; set; }

        public Jinx()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 525f);

            W = new Spell(SpellSlot.W, 1500f);
            W.SetSkillshot(0.60f, 60f, 3300f, true, false, SkillshotType.Line);

            E = new Spell(SpellSlot.E, 900f);
            E.SetSkillshot(1.20f, 100f, 1750f, false, false, SkillshotType.Circle);

            R = new Spell(SpellSlot.R, 3000f);
            R.SetSkillshot(0.70f, 140f, 1500f, true, false, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSolo", "Use R| Solo Mode");
            ComboOption.AddBool("ComboRTeam", "Use R| Team Fight");

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddW();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddW();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddW();
            MiscOption.AddBool("W", "AutoW", "Auto W| CC");
            MiscOption.AddE();
            MiscOption.AddBool("E", "AutoE", "Auto E| CC");
            MiscOption.AddBool("E", "AutoETP", "Auto E| Teleport");
            MiscOption.AddR();
            MiscOption.AddKey("R", "rMenuSemi", "Semi-manual R Key", Keys.T, KeyBindType.Press);
            MiscOption.AddSlider("R", "rMenuMin", "Use R| Min Range >= x", 1000, 500, 2500);
            MiscOption.AddSlider("R", "rMenuMax", "Use R| Max Range <= x", 3000, 1500, 3500);

            DrawOption.AddMenu();
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddDamageIndicatorToHero(false, true, false, true, true);

            Tick.OnTick += OnUpdate;
            Orbwalker.OnAction += OnAction;
            //Gapcloser.OnGapcloser += OnGapcloser;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Me.IsWindingUp)
            {
                return;
            }

            if (Q.Level > 0)
            {
                bigGunRange = Q.Range + new[] { 75, 100, 125, 150, 175 }[Q.Level - 1];
            }

            if (W.Level > 0)
            {
                //https://github.com/ensoulsharp-io/EnsoulSharp.Assemblies/blob/master/Third_Port/Mask/Executable/OneKeyToWin_AIO_Sebby/Champions/Jinx.cs#L128-L131
                W.Delay = Math.Max(0.4f, (600 - Me.PercentAttackSpeedMod / 2.5f * 200) / 1000f);
            }

            if (R.Level > 0)
            {
                R.Range = MiscOption.GetSlider("R", "rMenuMax").Value;
            }

            rCoolDown = R.Level > 0
                ? (Me.Spellbook.GetSpell(SpellSlot.R).CooldownExpires - Game.Time < 0
                    ? 0
                    : Me.Spellbook.GetSpell(SpellSlot.R).CooldownExpires - Game.Time)
                : -1;

            if (MiscOption.GetKey("R", "rMenuSemi").Active)
            {
                SemiRLogic();
            }

            AutoLogic();
            KillSteal();

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    FarmHarass();
                    break;
            }
        }

        private static void AutoLogic()
        {
            if (MiscOption.GetBool("W", "AutoW").Enabled && W.IsReady())
            {
                foreach (
                    var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(W.Range) && !x.CanMoveMent()))
                {
                    if (target.IsValidTarget(W.Range))
                    {
                        W.Cast(target);
                    }
                }
            }

            if (MiscOption.GetBool("E", "AutoE").Enabled && E.IsReady())
            {
                foreach (
                    var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && !x.CanMoveMent()))
                {
                    if (target.IsValidTarget(E.Range))
                    {
                        E.Cast(target.PreviousPosition);
                    }
                }
            }

            if (MiscOption.GetBool("E", "AutoETP").Enabled && E.IsReady())
            {
                foreach (
                    var obj in
                    ObjectManager.Get<AIBaseClient>()
                        .Where(
                            x =>
                                x.IsEnemy && x.DistanceToPlayer() < E.Range &&
                                (x.HasBuff("teleport_target") || x.HasBuff("Pantheon_GrandSkyfall_Jump"))))
                {
                    if (obj.IsValidTarget(E.Range))
                    {
                        E.Cast(obj.PreviousPosition);
                    }
                }
            }
        }

        private static void SemiRLogic()
        {
            Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (!R.IsReady())
            {
                return;
            }

            var target = MyTargetSelector.GetTarget(R.Range);
            if (target.IsValidTarget(R.Range))
            {
                var rPred = R.GetPrediction(target);

                if (rPred.Hitchance >= HitChance.High)
                {
                    R.Cast(rPred.CastPosition);
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseW && W.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(W.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.W)))
                {
                    if (target.InAutoAttackRange() && target.Health <= Me.GetAutoAttackDamage(target) * 2)
                    {
                        continue;
                    }

                    if (target.IsValidTarget(W.Range) && !target.IsUnKillable())
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.Hitchance >= HitChance.High)
                        {
                            W.Cast(wPred.UnitPosition);
                        }
                    }
                }
            }

            if (KillStealOption.UseR && R.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(R.Range) && x.DistanceToPlayer() > MiscOption.GetSlider("R", "rMenuMin").Value &&
                            KillStealOption.GetKillStealTarget(x.CharacterName) &&
                            x.Health < Me.GetSpellDamage(x, SpellSlot.R)))
                {
                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.Hitchance >= HitChance.High)
                        {
                            R.Cast(rPred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.UseW && W.IsReady())
            {
                var target = MyTargetSelector.GetTarget(W.Range);

                if (target.IsValidTarget(W.Range) && target.DistanceToPlayer() > Q.Range
                    && Me.CountEnemyHeroesInRange(W.Range - 300) <= 2)
                {
                    var wPred = W.GetPrediction(target);

                    if (wPred.Hitchance >= HitChance.High)
                    {
                        W.Cast(wPred.UnitPosition);
                    }
                }
            }

            if (ComboOption.UseE && E.IsReady())
            {
                var target = MyTargetSelector.GetTarget(E.Range);

                if (target.IsValidTarget(E.Range))
                {
                    if (!target.CanMoveMent())
                    {
                        E.Cast(target);
                    }
                    else
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.Hitchance >= HitChance.High)
                        {
                            E.Cast(ePred.CastPosition);
                        }
                    }
                }
            }

            if (ComboOption.UseQ && Q.IsReady())
            {
                var target = MyTargetSelector.GetTarget(bigGunRange);

                if (Me.HasBuff("JinxQ"))
                {
                    if (Me.Mana < (rCoolDown == -1 ? 100 : (rCoolDown > 10 ? 130 : 150)))
                    {
                        if (Orbwalker.CanAttack())
                        {
                            Q.Cast();
                        }
                    }

                    if (Me.CountEnemyHeroesInRange(1500) == 0)
                    {
                        Q.Cast();
                    }

                    if (target == null)
                    {
                        if (Orbwalker.CanAttack())
                        {
                            Q.Cast();
                        }
                    }
                    else if (target.IsValidTarget(bigGunRange))
                    {
                        if (target.Health < Me.GetAutoAttackDamage(target) * 3 &&
                            target.DistanceToPlayer() <= Q.Range + 60)
                        {
                            if (Orbwalker.CanAttack())
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
                else
                {
                    if (target.IsValidTarget(bigGunRange))
                    {
                        if (Me.CountEnemyHeroesInRange(Q.Range) == 0 &&
                            Me.CountEnemyHeroesInRange(bigGunRange) > 0 &&
                            Me.Mana > R.Mana + W.Mana + Q.Mana * 2)
                        {
                            if (Orbwalker.CanAttack())
                            {
                                Q.Cast();
                            }
                        }

                        if (target.CountEnemyHeroesInRange(150) >= 2 &&
                            Me.Mana > R.Mana + Q.Mana * 2 + W.Mana &&
                            target.DistanceToPlayer() > Q.Range)
                        {
                            if (Orbwalker.CanAttack())
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
            }

            if (ComboOption.UseR && R.IsReady())
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(1200)))
                {
                    if (ComboOption.GetBool("ComboRTeam").Enabled && target.IsValidTarget(600) &&
                        Me.CountEnemyHeroesInRange(600) >= 2 &&
                        target.CountAllyHeroesInRange(200) <= 3 && target.HealthPercent < 50)
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.Hitchance >= HitChance.High)
                        {
                            R.Cast(rPred.CastPosition);
                        }
                    }

                    if (ComboOption.GetBool("ComboRSolo").Enabled && Me.CountEnemyHeroesInRange(1500) <= 2 &&
                        target.DistanceToPlayer() > Q.Range &&
                        target.DistanceToPlayer() < bigGunRange && target.Health > Me.GetAutoAttackDamage(target) &&
                        target.Health < Me.GetSpellDamage(target, SpellSlot.R) + Me.GetAutoAttackDamage(target) * 3)
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.Hitchance >= HitChance.High)
                        {
                            R.Cast(rPred.CastPosition);
                        }
                    }
                }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseW && W.IsReady())
                {
                    var target = HarassOption.GetTarget(W.Range);

                    if (target.IsValidTarget(W.Range) && target.DistanceToPlayer() > Q.Range)
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.Hitchance >= HitChance.High)
                        {
                            W.Cast(wPred.UnitPosition);
                        }
                    }
                }

                if (HarassOption.UseQ && Q.IsReady())
                {
                    var target = HarassOption.GetTarget(bigGunRange);

                    if (target.IsValidTarget(bigGunRange) && Orbwalker.CanAttack())
                    {
                        if (target.CountEnemyHeroesInRange(150) >= 2 &&
                            Me.Mana > R.Mana + Q.Mana * 2 + W.Mana &&
                            target.DistanceToPlayer() > Q.Range)
                        {
                            if (Orbwalker.CanAttack())
                            {
                                Q.Cast();
                            }
                        }

                        if (target.DistanceToPlayer() > Q.Range &&
                            Me.Mana > R.Mana + Q.Mana * 2 + W.Mana)
                        {
                            if (Orbwalker.CanAttack())
                            {
                                Q.Cast();
                            }
                        }
                    }
                    else
                    {
                        if (Me.HasBuff("JinxQ") && Q.IsReady())
                        {
                            Q.Cast();
                        }
                    }
                }
                else if (Me.HasBuff("JinxQ") && Q.IsReady())
                {
                    Q.Cast();
                }
            }
        }

        private static void FarmHarass()
        {
            if (MyManaManager.SpellHarass)
            {
                Harass();
            }

            if (MyManaManager.SpellFarm)
            {
                JungleClear();
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                var mobs =
                    GameObjects.Jungle.Where(x => x.IsValidTarget(bigGunRange) && x.GetJungleType() != JungleType.Unknown)
                        .OrderByDescending(x => x.MaxHealth)
                        .ToList();

                if (mobs.Any())
                {
                    if (JungleClearOption.UseW && W.IsReady() &&
                        mobs.FirstOrDefault(x => x.GetJungleType() != JungleType.Unknown) != null)
                    {
                        W.Cast(mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini")));
                    }

                    if (JungleClearOption.UseQ && Q.IsReady())
                    {
                        if (Me.HasBuff("JinxQ"))
                        {
                            foreach (var mob in mobs)
                            {
                                var count = ObjectManager.Get<AIMinionClient>().Count(x => x.Distance(mob) <= 150);

                                if (mob.DistanceToPlayer() <= bigGunRange)
                                {
                                    if (count < 2)
                                    {
                                        if (Orbwalker.CanAttack())
                                        {
                                            Q.Cast();
                                        }
                                    }
                                    else if (mob.Health > Me.GetAutoAttackDamage(mob) * 1.1f)
                                    {
                                        if (Orbwalker.CanAttack())
                                        {
                                            Q.Cast();
                                        }
                                    }
                                }
                            }

                            if (mobs.Count < 2)
                            {
                                if (Orbwalker.CanAttack())
                                {
                                    Q.Cast();
                                }
                            }
                        }
                        else
                        {
                            foreach (var mob in mobs)
                            {
                                var count = ObjectManager.Get<AIMinionClient>().Count(x => x.Distance(mob) <= 150);

                                if (mob.DistanceToPlayer() <= bigGunRange)
                                {
                                    if (count >= 2)
                                    {
                                        if (Orbwalker.CanAttack())
                                        {
                                            Q.Cast();
                                        }
                                    }
                                    else if (mob.Health < Me.GetAutoAttackDamage(mob) * 1.1f &&
                                             mob.DistanceToPlayer() > Q.Range)
                                    {
                                        if (Orbwalker.CanAttack())
                                        {
                                            Q.Cast();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Me.HasBuff("JinxQ") && Q.IsReady())
                    {
                        Q.Cast();
                    }
                }
            }
            else
            {
                if (Me.HasBuff("JinxQ") && Q.IsReady())
                {
                    Q.Cast();
                }
            }
        }

        private static void OnAction(object sender, OrbwalkerActionArgs Args)
        {
            if (Args.Type != OrbwalkerType.BeforeAttack)
            {
                return;
            }

            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0)
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.AIHeroClient:
                    {
                        var target = (AIHeroClient)Args.Target;
                        if (target != null && target.IsValidTarget())
                        {
                            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                            {
                                if (ComboOption.UseQ && Q.IsReady())
                                {
                                    if (Me.HasBuff("JinxQ"))
                                    {
                                        if (target.Health < Me.GetAutoAttackDamage(target) * 3 &&
                                            target.DistanceToPlayer() <= Q.Range + 60)
                                        {
                                            Q.Cast();
                                        }
                                        else if (Me.Mana < (rCoolDown == -1 ? 100 : (rCoolDown > 10 ? 130 : 150)))
                                        {
                                            Q.Cast();
                                        }
                                        else if (target.IsValidTarget(Q.Range))
                                        {
                                            Q.Cast();
                                        }
                                    }
                                    else
                                    {
                                        if (target.CountEnemyHeroesInRange(150) >= 2 &&
                                            Me.Mana > R.Mana + Q.Mana * 2 + W.Mana &&
                                            target.DistanceToPlayer() > Q.Range)
                                        {
                                            Q.Cast();
                                        }
                                    }
                                }
                            }
                            else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass ||
                                     Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && MyManaManager.SpellHarass)
                            {
                                if (HarassOption.HasEnouguMana())
                                {
                                    if (HarassOption.UseQ && Q.IsReady())
                                    {
                                        if (Me.HasBuff("JinxQ"))
                                        {
                                            if (target.DistanceToPlayer() >= bigGunRange)
                                            {
                                                Q.Cast();
                                            }
                                        }
                                        else
                                        {
                                            if (target.CountEnemyHeroesInRange(150) >= 2 &&
                                                Me.Mana > R.Mana + Q.Mana * 2 + W.Mana &&
                                                target.DistanceToPlayer() > Q.Range)
                                            {
                                                Q.Cast();
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (Me.HasBuff("JinxQ") && Q.IsReady())
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.AIMinionClient:
                    {
                        var minion = (AIMinionClient)Args.Target;
                        if (minion != null && minion.IsMinion())
                        {
                            if (LaneClearOption.HasEnouguMana() && Q.IsReady() && LaneClearOption.UseQ)
                            {
                                var min = (AIBaseClient)Args.Target;
                                var minions =
                                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(bigGunRange) && x.IsMinion())
                                        .ToList();

                                if (minions.Any() && min != null)
                                {
                                    foreach (var m in minions.Where(x => x.NetworkId != min.NetworkId))
                                    {
                                        var count =
                                            ObjectManager.Get<AIMinionClient>().Count(x => x.Distance(m) <= 150);

                                        if (m.DistanceToPlayer() <= bigGunRange)
                                        {
                                            if (Me.HasBuff("JinxQ"))
                                            {
                                                if (LaneClearOption.GetSlider("LaneClearQCount").Value > count)
                                                {
                                                    Q.Cast();
                                                }
                                                else if (min.Health > Me.GetAutoAttackDamage(min) * 1.1f)
                                                {
                                                    Q.Cast();
                                                }
                                            }
                                            else if (!Me.HasBuff("JinxQ"))
                                            {
                                                if (LaneClearOption.GetSlider("LaneClearQCount").Value <= count)
                                                {
                                                    Q.Cast();
                                                }
                                                else if (min.Health < Me.GetAutoAttackDamage(min) * 1.1f &&
                                                         min.DistanceToPlayer() > Q.Range)
                                                {
                                                    Q.Cast();
                                                }
                                            }
                                        }
                                    }

                                    if (minions.Count <= 2 && Me.HasBuff("JinxQ"))
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                            else
                            {
                                if (Me.HasBuff("JinxQ") && Q.IsReady())
                                {
                                    Q.Cast();
                                }
                            }
                        }
                    }
                    break;
            }
        }

        //private static void OnGapcloser(AIHeroClient target, GapcloserArgs Args)
        //{
        //    if (target != null && target.IsValidTarget())
        //    {
        //        if (E.IsReady() && target.IsValidTarget(E.Range))
        //        {
        //            switch (Args.Type)
        //            {
        //                case SpellType.Melee:
        //                    if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100) && !Args.HaveShield)
        //                    {
        //                        E.Cast(Me.PreviousPosition);
        //                    }
        //                    break;
        //                case SpellType.Dash:
        //                case SpellType.SkillShot:
        //                case SpellType.Targeted:
        //                    {
        //                        if (target.InAutoAttackRange() && !Args.HaveShield)
        //                        {
        //                            var ePred = E.GetPrediction(target);
        //                            E.Cast(ePred.UnitPosition);
        //                        }
        //                    }
        //                    break;
        //            }
        //        }
        //    }
        //}
    }
}