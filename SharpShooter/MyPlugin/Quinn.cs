namespace SharpShooter.MyPlugin
{
    #region

    using System;
    using System.Linq;

    using EnsoulSharp;
    using EnsoulSharp.SDK;
    using EnsoulSharp.SDK.Prediction;
    using EnsoulSharp.SDK.Events;
    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    public class Quinn : MyLogic
    {
        public Quinn()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 1000f);
            Q.SetSkillshot(0.25f, 90f, 1550f, true, false, SkillshotType.Line);

            W = new Spell(SpellSlot.W, 2000f);

            E = new Spell(SpellSlot.E, 700f) { Delay = 0.25f };

            R = new Spell(SpellSlot.R, 550f);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q|Min Hit Count >= ", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddE();
            MiscOption.AddBool("E", "AutoE", "Auto E| AntiGapcloser");
            MiscOption.AddR();
            MiscOption.AddBool("R", "AutoR", "Auto R");
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcus", "Forcus", "Forcus Attack Passive Target");

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddE(E);
            DrawOption.AddDamageIndicatorToHero(true, false, true, false, true);

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

            if (Variables.GameTimeTickCount - LastForcusTime > Me.AttackCastDelay * 1000f)
            {
                if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                {
                    Orbwalker.ForceTarget = null;
                }
            }

            if (Me.IsWindingUp)
            {
                return;
            }

            KillSteal();

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.None:
                    AutoR();
                    break;
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

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                {
                    if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(qPred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(Q.Range);

            if (target.IsValidTarget(Q.Range))
            {
                if (ComboOption.UseE && E.IsReady() && Me.HasBuff("QuinnR") && target.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(target);
                }

                if (ComboOption.UseQ && Q.IsReady() && !Me.HasBuff("QuinnR"))
                {
                    if (target.DistanceToPlayer() <= Me.AttackRange + Me.BoundingRadius + target.BoundingRadius - 50 && HavePassive(target))
                    {
                        return;
                    }

                    var qPred = Q.GetPrediction(target);

                    if (qPred.Hitchance >= HitChance.High)
                    {
                        Q.Cast(qPred.UnitPosition);
                    }
                }

                if (ComboOption.UseW && W.IsReady())
                {
                    if (target.PreviousPosition.IsGrass())
                    {
                        W.Cast();
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseQ)
                {
                    var target = HarassOption.GetTarget(Q.Range);

                    if (target.IsValidTarget(Q.Range))
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(qPred.UnitPosition);
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
                if (LaneClearOption.UseQ && Q.IsReady())
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToList();

                    if (minions.Any())
                    {
                        var QFarm = Q.GetCircularFarmLocation(minions);

                        if (QFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearQCount").Value)
                        {
                            Q.Cast(QFarm.Position);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() != JungleType.Unknown).ToList();

                if (mobs.Any())
                {
                    if (JungleClearOption.UseQ && Q.IsReady())
                    {
                        var QFarm = Q.GetCircularFarmLocation(mobs);

                        if (QFarm.MinionsHit >= 1)
                        {
                            Q.Cast(QFarm.Position);
                        }
                    }

                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        var mob =
                            mobs.FirstOrDefault(
                                x => !x.Name.ToLower().Contains("mini") && x.Health >= Me.GetSpellDamage(x, SpellSlot.E));

                        if (mob != null && mob.IsValidTarget(E.Range))
                        {
                            E.CastOnUnit(mob);
                        }
                    }
                }
            }
        }

        private static void AutoR()
        {
            if (MiscOption.GetBool("R", "AutoR").Enabled && R.IsReady() && R.Name == "QuinnR")
            {
                if (!Me.IsDead && Me.InFountain())
                {
                    R.Cast();
                }
            }
        }

        private static void OnAction(object sender, OrbwalkerActionArgs Args)
        {
            if (Args.Type == OrbwalkerType.BeforeAttack)
            {
                if (MiscOption.GetBool("Forcus", "Forcus").Enabled)
                {
                    if (Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(x => !x.IsDead && HavePassive(x)))
                        {
                            if (enemy.InAutoAttackRange())
                            {
                                Orbwalker.ForceTarget = enemy;
                                LastForcusTime = Variables.GameTimeTickCount;
                            }
                        }
                    }
                    else if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                    {
                        var all =
                            GameObjects.EnemyMinions.Where(x => x.InAutoAttackRange() && HavePassive(x))
                                .OrderByDescending(x => x.MaxHealth)
                                .FirstOrDefault();

                        if (all.InAutoAttackRange())
                        {
                            Orbwalker.ForceTarget = all;
                            LastForcusTime = Variables.GameTimeTickCount;
                        }
                    }
                }
            }

            if (Args.Type != OrbwalkerType.AfterAttack)
            {
                Orbwalker.ForceTarget = null;

                if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() ||
                    Args.Target.Health <= 0 || Orbwalker.ActiveMode == OrbwalkerMode.None)
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
                                if (target != null && target.IsValidTarget())
                                {
                                    if (ComboOption.UseE && E.IsReady())
                                    {
                                        E.CastOnUnit(target);
                                    }
                                    else if (ComboOption.UseQ && Q.IsReady() && !Me.HasBuff("QuinnR"))
                                    {
                                        var qPred = Q.GetPrediction(target);

                                        if (qPred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(qPred.UnitPosition);
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
                                var mob = (AIMinionClient)Args.Target;
                                if (mob != null && mob.IsValidTarget() && mob.GetJungleType() != JungleType.Unknown && JungleClearOption.HasEnouguMana())
                                {
                                    if (JungleClearOption.UseE && E.IsReady() && mob.IsValidTarget(E.Range))
                                    {
                                        E.CastOnUnit(mob);
                                    }
                                    else if (JungleClearOption.UseQ && Q.IsReady() && mob.IsValidTarget(Q.Range) &&
                                             !Me.HasBuff("QuinnR"))
                                    {
                                        Q.Cast(mob);
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        //private static void OnGapcloser(AIHeroClient target, GapcloserArgs Args)
        //{
        //    if (MiscOption.GetBool("E", "AutoE").Enabled && E.IsReady() && target != null && target.IsValidTarget(E.Range))
        //    {
        //        switch (Args.Type)
        //        {
        //            case SpellType.Melee:
        //                if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
        //                {
        //                    E.CastOnUnit(target);
        //                }
        //                break;
        //            case SpellType.Dash:
        //            case SpellType.SkillShot:
        //            case SpellType.Targeted:
        //                {
        //                    E.CastOnUnit(target);
        //                }
        //                break;
        //        }
        //    }
        //}

        private static bool HavePassive(AIBaseClient target)
        {
            return target.HasBuff("quinnw");
        }
    }
}