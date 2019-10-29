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

    public class Tristana : MyLogic
    {
        public Tristana()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q);

            W = new Spell(SpellSlot.W, 900f);
            W.SetSkillshot(0.50f, 250f, 1400f, false, false, SkillshotType.Circle);

            E = new Spell(SpellSlot.E, 700f);

            R = new Spell(SpellSlot.R, 700f);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboQAlways", "Use Q| Always Cast it(Off = Logic Cast)", false);
            ComboOption.AddE();
            ComboOption.AddBool("ComboEOnlyAfterAA", "Use E| Only After Attack Cast it");
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRHp", "Use R| Player HealthPercent <= x%(Save mySelf)", 25, 0, 100);

            HarassOption.AddMenu();
            HarassOption.AddE(false);
            HarassOption.AddBool("HarassEToMinion", "Use E| Cast Low Hp Minion");
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddE();
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddE();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddE();
            MiscOption.AddKey("E", "SemiE", "Semi-manual E Key", Keys.T, KeyBindType.Press);
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcus", "Forcustarget", "Forcus Attack Passive Target");

            DrawOption.AddMenu();
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(false, false, true, true, true);

            Tick.OnTick += OnUpdate;
            //Gapcloser.OnGapcloser += OnGapcloser;
            Orbwalker.OnAction += OnAction;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Variables.GameTimeTickCount - LastForcusTime > Me.AttackCastDelay * 1000f)
            {
                if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                {
                    Orbwalker.ForceTarget = null;
                }
            }

            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Me.IsWindingUp)
            {
                return;
            }

            if (E.Level > 0)
            {
                E.Range = 630 + 7 * (Me.Level - 1);
            }

            if (R.Level > 0)
            {
                R.Range = 630 + 7 * (Me.Level - 1);
            }

            if (MiscOption.GetKey("E", "SemiE").Active && E.IsReady())
            {
                OneKeyCastE();
            }

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

        private static void OneKeyCastE()
        {
            var target = MyTargetSelector.GetTarget(E.Range);

            if (target.IsValidTarget(E.Range))
            {
                if (target.Health <
                    Me.GetSpellDamage(target, SpellSlot.E) * (target.GetBuffCount("TristanaECharge") * 0.30) +
                    Me.GetSpellDamage(target, SpellSlot.E))
                {
                    E.CastOnUnit(target);
                }

                if (Me.CountEnemyHeroesInRange(1200) == 1)
                {
                    if (Me.HealthPercent >= target.HealthPercent && Me.Level + 1 >= target.Level)
                    {
                        E.CastOnUnit(target);
                    }
                    else if (Me.HealthPercent + 20 >= target.HealthPercent &&
                        Me.HealthPercent >= 40 && Me.Level + 2 >= target.Level)
                    {
                        E.CastOnUnit(target);
                    }
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseE && E.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && x.Health <
                                                   Me.GetSpellDamage(x, SpellSlot.E) *
                                                   (x.GetBuffCount("TristanaECharge") * 0.30) +
                                                   Me.GetSpellDamage(x, SpellSlot.E)))
                {
                    if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                    {
                        E.CastOnUnit(target);
                    }
                }
            }

            if (KillStealOption.UseR && R.IsReady())
            {
                if (KillStealOption.UseE && E.IsReady())
                {
                    foreach (
                        var target in
                        from x in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && KillStealOption.GetKillStealTarget(x.CharacterName))
                        let etargetstacks = x.Buffs.Find(buff => buff.Name == "TristanaECharge")
                        where Me.GetSpellDamage(x, SpellSlot.R) + Me.GetSpellDamage(x, SpellSlot.E) + etargetstacks?.Count * 0.30 * Me.GetSpellDamage(x, SpellSlot.E) >= x.Health
                        select x)
                    {
                        if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                        {
                            R.CastOnUnit(target);
                            return;
                        }
                    }
                }
                else
                {
                    var target = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && KillStealOption.GetKillStealTarget(x.CharacterName))
                        .OrderByDescending(x => x.Health).FirstOrDefault(x => x.Health < Me.GetSpellDamage(x, SpellSlot.R));

                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                    {
                        R.CastOnUnit(target);
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(E.Range);

            if (target.IsValidTarget(E.Range))
            {
                if (ComboOption.UseQ && Q.IsReady())
                {
                    if (!ComboOption.GetBool("ComboQAlways").Enabled)
                    {
                        if (!E.IsReady() && target.HasBuff("TristanaECharge"))
                        {
                            Q.Cast();
                        }
                        else if (!E.IsReady() && !target.HasBuff("TristanaECharge") && E.CooldownTime > 4)
                        {
                            Q.Cast();
                        }
                    }
                    else
                    {
                        Q.Cast();
                    }
                }

                if (ComboOption.UseE && E.IsReady() && !ComboOption.GetBool("ComboQAlways").Enabled && target.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(target);
                }

                if (ComboOption.UseR && R.IsReady() && Me.HealthPercent <= ComboOption.GetSlider("ComboRHp").Value)
                {
                    var dangerenemy =
                        GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range))
                            .OrderBy(x => x.Distance(Me))
                            .FirstOrDefault();

                    if (dangerenemy != null)
                    {
                        R.CastOnUnit(dangerenemy);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (E.IsReady())
                {
                    if (HarassOption.UseE)
                    {
                        var target = HarassOption.GetTarget(E.Range);

                        if (target.IsValidTarget(E.Range))
                        {
                            E.CastOnUnit(target);
                        }
                    }

                    if (HarassOption.GetBool("HarassEToMinion").Enabled)
                    {
                        foreach (
                            var minion in
                            GameObjects.EnemyMinions.Where(
                                x =>
                                    x.IsValidTarget(E.Range) && x.IsMinion() && x.Health < Me.GetAutoAttackDamage(x) &&
                                    x.CountEnemyHeroesInRange(x.BoundingRadius + 150) >= 1))
                        {
                            var target = HarassOption.GetTarget(E.Range);

                            if (target != null)
                            {
                                return;
                            }

                            E.CastOnUnit(minion);
                            Orbwalker.ForceTarget = minion;
                            LastForcusTime = Variables.GameTimeTickCount;
                        }
                    }
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
                var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range) && x.GetJungleType() != JungleType.Unknown && x.GetJungleType() != JungleType.Small).ToList();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        E.CastOnUnit(mob);
                    }

                    if (JungleClearOption.UseQ && Q.IsReady() && !E.IsReady())
                    {
                        Q.Cast();
                    }
                }
            }
        }

        //private static void OnGapcloser(AIHeroClient target, GapcloserArgs Args)
        //{
        //    if (R.IsReady() && target != null && target.IsValidTarget(R.Range))
        //    {
        //        switch (Args.Type)
        //        {
        //            case SpellType.Melee:
        //                if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
        //                {
        //                    R.CastOnUnit(target);
        //                }
        //                break;
        //            case SpellType.Dash:
        //            case SpellType.SkillShot:
        //            case SpellType.Targeted:
        //                {
        //                    R.CastOnUnit(target);
        //                }
        //                break;
        //        }
        //    }
        //}

        private static void OnAction(object sender, OrbwalkerActionArgs Args)
        {
            if (Args.Type == OrbwalkerType.BeforeAttack)
            {
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
                                    if (MiscOption.GetBool("Forcus", "Forcustarget").Enabled)
                                    {
                                        foreach (
                                            var forcusTarget in
                                            GameObjects.EnemyHeroes.Where(
                                                x => x.InAutoAttackRange() && x.HasBuff("TristanaEChargeSound")))
                                        {
                                            Orbwalker.ForceTarget = forcusTarget;
                                            LastForcusTime = Variables.GameTimeTickCount;
                                        }
                                    }

                                    if (ComboOption.UseQ && Q.IsReady())
                                    {
                                        if (target.HasBuff("TristanaEChargeSound") || target.HasBuff("TristanaECharge"))
                                        {
                                            Q.Cast();
                                        }

                                        if (ComboOption.GetBool("ComboQAlways").Enabled)
                                        {
                                            Q.Cast();
                                        }
                                    }
                                }
                                else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass ||
                                         Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && MyManaManager.SpellFarm)
                                {
                                    if (MiscOption.GetBool("Forcus", "Forcustarget").Enabled)
                                    {
                                        foreach (
                                            var forcusTarget in
                                            GameObjects.EnemyHeroes.Where(
                                                x => x.InAutoAttackRange() && x.HasBuff("TristanaEChargeSound")))
                                        {
                                            Orbwalker.ForceTarget = forcusTarget;
                                            LastForcusTime = Variables.GameTimeTickCount;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case GameObjectType.AIMinionClient:
                        {
                            var mob = (AIMinionClient)Args.Target;
                            if (mob != null && mob.IsValidTarget() && mob.GetJungleType() != JungleType.Unknown)
                            {
                                if (JungleClearOption.HasEnouguMana())
                                {
                                    if (JungleClearOption.UseQ && Q.IsReady())
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            if (Args.Type == OrbwalkerType.AfterAttack)
            {
                Orbwalker.ForceTarget = null;

                if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0)
                {
                    return;
                }

                switch (Args.Target.Type)
                {
                    case GameObjectType.AIHeroClient:
                        {
                            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                            {
                                var target = (AIHeroClient)Args.Target;
                                if (target != null && target.IsValidTarget(E.Range))
                                {
                                    if (ComboOption.UseE && E.IsReady() && ComboOption.GetBool("ComboEOnlyAfterAA").Enabled)
                                    {
                                        E.CastOnUnit(target);
                                    }
                                }
                            }
                        }
                        break;
                    case GameObjectType.AITurretClient:
                    case GameObjectType.HQClient:
                    case GameObjectType.Barracks:
                    case GameObjectType.BarracksDampenerClient:
                    case GameObjectType.BuildingClient:
                        {
                            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && MyManaManager.SpellFarm && LaneClearOption.HasEnouguMana(true))
                            {
                                if (Me.CountEnemyHeroesInRange(800) == 0)
                                {
                                    if (LaneClearOption.UseE && E.IsReady())
                                    {
                                        E.CastOnUnit(Args.Target as AIBaseClient);

                                        if (LaneClearOption.UseQ && Q.IsReady())
                                        {
                                            Q.Cast();
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}