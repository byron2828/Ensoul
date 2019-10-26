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

    public class Ashe : MyLogic
    {
        public Ashe()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q);

            W = new Spell(SpellSlot.W, 1225f);
            W.SetSkillshot(0.25f, 60f, 2000f, true, true, SkillshotType.Cone);

            E = new Spell(SpellSlot.E, 5000f);
            E.SetSkillshot(0.25f, 300f, 1400f, false, false, SkillshotType.Line);

            R = new Spell(SpellSlot.R, 2000f);
            R.SetSkillshot(0.25f, 130f, 1550f, true, true, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboSaveMana", "Use Q |Save Mana");
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSolo", "Use R |Solo Mode");
            ComboOption.AddBool("ComboRTeam", "Use R |Team Fight");

            HarassOption.AddMenu();
            HarassOption.AddW();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddW();
            LaneClearOption.AddSlider("LaneClearWCount", "Use W |Min Hit Count >= x", 3, 1, 5);
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
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key", Keys.T, KeyBindType.Press);
            MiscOption.AddBool("R", "AutoR", "Auto R| Anti Gapcloser");

            DrawOption.AddMenu();
            DrawOption.AddW(W);
            DrawOption.AddR(R);
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

            if (MiscOption.GetKey("R", "SemiR").Active)
            {
                OneKeyR();
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

        private static void OneKeyR()
        {
            Orbwalker.Move(Game.CursorPos);

            if (R.IsReady())
            {
                var target = MyTargetSelector.GetTarget(R.Range);

                if (target != null && !target.HasBuffOfType(BuffType.SpellShield) && target.IsValidTarget(R.Range))
                {
                    var rPred = R.GetPrediction(target,
                        collisionable: CollisionObjects.Heroes | CollisionObjects.YasuoWall);

                    if (rPred.Hitchance >= HitChance.High)
                    {
                        R.Cast(rPred.CastPosition);
                    }
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseW && W.IsReady())
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(W.Range)))
                {
                    if (!target.IsValidTarget(W.Range) || !(target.Health < Me.GetSpellDamage(target, SpellSlot.W)))
                    {
                        continue;
                    }

                    if (target.InAutoAttackRange() && (Me.HasBuff("AsheQAttack") || Me.HasBuff("asheqcastready")))
                    {
                        continue;
                    }

                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.Hitchance >= HitChance.High)
                        {
                            W.Cast(wPred.CastPosition);
                        }
                    }
                }
            }

            if (KillStealOption.UseR && R.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(2000) && KillStealOption.GetKillStealTarget(x.CharacterName)))
                {
                    if (!(target.DistanceToPlayer() > 800) || !(target.Health < Me.GetSpellDamage(target, SpellSlot.R)) ||
                        target.HasBuffOfType(BuffType.SpellShield))
                    {
                        continue;
                    }

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
            if (ComboOption.UseQ && Q.IsReady() && Orbwalker.GetTarget() != null)
            {
                var target = Orbwalker.GetTarget() as AIHeroClient;
                if (target != null && !target.IsDead && target.InAutoAttackRange())
                {
                    if (Me.HasBuff("asheqcastready"))
                    {
                        Q.Cast();
                    }
                }
            }

            if (ComboOption.UseR && R.IsReady())
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(1200)))
                {
                    if (ComboOption.GetBool("ComboRTeam").Enabled)
                    {
                        if (target.IsValidTarget(600) && Me.CountEnemyHeroesInRange(600) >= 3 &&
                            target.CountAllyHeroesInRange(200) <= 2)
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.Hitchance >= HitChance.High)
                            {
                                R.Cast(rPred.CastPosition);
                            }
                        }
                    }

                    if (ComboOption.GetBool("ComboRSolo").Enabled)
                    {
                        if (Me.CountEnemyHeroesInRange(800) == 1 &&
                            !target.InAutoAttackRange() &&
                            target.DistanceToPlayer() <= 700 &&
                            target.Health > Me.GetAutoAttackDamage(target) &&
                            target.Health < Me.GetSpellDamage(target, SpellSlot.R) + Me.GetAutoAttackDamage(target) * 3 &&
                            !target.HasBuffOfType(BuffType.SpellShield))
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.Hitchance >= HitChance.High)
                            {
                                R.Cast(rPred.CastPosition);
                            }
                        }

                        if (target.DistanceToPlayer() <= 1000 &&
                            (!target.CanMoveMent() || target.HasBuffOfType(BuffType.Stun) ||
                             R.GetPrediction(target).Hitchance == HitChance.Immobile))
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

            if (ComboOption.UseW && W.IsReady() && !Me.HasBuff("AsheQAttack"))
            {
                if (ComboOption.GetBool("ComboSaveMana").Enabled &&
                    Me.Mana > (R.IsReady() ? R.Mana : 0) + W.Mana + Q.Mana ||
                    !ComboOption.GetBool("ComboSaveMana").Enabled)
                {
                    var target = MyTargetSelector.GetTarget(W.Range);

                    if (target.IsValidTarget(W.Range))
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.Hitchance >= HitChance.High)
                        {
                            W.Cast(wPred.CastPosition);
                        }
                    }
                }
            }

            if (ComboOption.UseE && E.IsReady())
            {
                var target = MyTargetSelector.GetTarget(1000);

                if (target != null)
                {
                    var ePred = E.GetPrediction(target);

                    if (ePred.UnitPosition.IsGrass() || target.PreviousPosition.IsGrass())
                    {
                        E.Cast(ePred.UnitPosition);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseW && W.IsReady() && !Me.HasBuff("AsheQAttack"))
                {
                    var target = HarassOption.GetTarget(W.Range);

                    if (target.IsValidTarget(W.Range))
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.Hitchance >= HitChance.High)
                        {
                            W.Cast(wPred.CastPosition);
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
                LaneClear();
                JungleClear();
            }
        }

        private static void LaneClear()
        {
            if (LaneClearOption.HasEnouguMana())
            {
                if (LaneClearOption.UseW && W.IsReady())
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion()).ToList();

                    if (minions.Any())
                    {
                        var wFarm = W.GetLineFarmLocation(minions);

                        if (wFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearWCount").Value)
                        {
                            W.Cast(wFarm.Position);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseW && !Me.HasBuff("AsheQAttack"))
                {
                    var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range) && x.GetJungleType() !=  JungleType.Unknown)
                        .OrderByDescending(x => x.MaxHealth)
                        .ToList();

                    if (mobs.Any())
                    {
                        var wFarm = W.GetLineFarmLocation(mobs);

                        if (wFarm.MinionsHit >= 2 || mobs.Any(x => x.GetJungleType() != JungleType.Small) && wFarm.MinionsHit >= 1)
                        {
                            W.Cast(wFarm.Position);
                        }
                    }
                }
            }
        }

        private static void OnAction(object sender, OrbwalkerActionArgs Args)
        {
            if (Args.Type != OrbwalkerType.BeforeAttack)
            {
                return;
            }

            if (Args.Target == null || Me.IsDead || Args.Target.IsDead || !Args.Target.IsValidTarget())
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.AIHeroClient:
                    {
                        if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                        {
                            if (ComboOption.UseQ && Q.IsReady())
                            {
                                var target = (AIHeroClient)Args.Target;

                                if (!target.IsDead && target.InAutoAttackRange())
                                {
                                    if (Me.HasBuff("asheqcastready"))
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
                        if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                        {
                            if (Args.Target is AIMinionClient)
                            {
                                var mob = (AIMinionClient)Args.Target;
                                if (mob != null && mob.GetJungleType() != JungleType.Unknown)
                                {
                                    if (JungleClearOption.HasEnouguMana() && JungleClearOption.UseQ && Q.IsReady())
                                    {
                                        if (!mob.InAutoAttackRange() ||
                                            !(mob.Health > Me.GetAutoAttackDamage(mob) * 2) ||
                                            !(mob.GetJungleType() == JungleType.Legendary || mob.GetJungleType() == JungleType.Large))
                                        {
                                            return;
                                        }

                                        if (Me.HasBuff("asheqcastready"))
                                        {
                                            Q.Cast();
                                        }
                                    }
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
                        if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                        {
                            if (LaneClearOption.HasEnouguMana(true) && LaneClearOption.UseQ)
                            {
                                if (Me.CountEnemyHeroesInRange(850) == 0)
                                {
                                    if (Me.HasBuff("asheqcastready"))
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

        //private static void OnGapcloser(AIHeroClient target, GapcloserArgs Args)
        //{
        //    if (MiscOption.GetBool("R", "AutoR").Enabled && R.IsReady() && target != null && target.IsValidTarget(R.Range) && !Args.HaveShield)
        //    {
        //        switch (Args.Type)
        //        {
        //            case SpellType.SkillShot:
        //                {
        //                    if (target.IsValidTarget(300))
        //                    {
        //                        var rPred = R.GetPrediction(target);

        //                        R.Cast(rPred.UnitPosition);
        //                    }
        //                }
        //                break;
        //            case SpellType.Melee:
        //            case SpellType.Dash:
        //            case SpellType.Targeted:
        //                {
        //                    if (target.IsValidTarget(400))
        //                    {
        //                        var rPred = R.GetPrediction(target);

        //                        R.Cast(rPred.UnitPosition);
        //                    }
        //                }
        //                break;
        //        }
        //    }
        //}
    }
}