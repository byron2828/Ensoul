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
    using SharpShooter.MyLibrary;

    using Keys = System.Windows.Forms.Keys;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    public class MissFortune : MyLogic
    {
        private static int lastRTime;

        public MissFortune()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 700f) { Delay = 0.25f, Speed = 1400f };

            Q2 = new Spell(SpellSlot.Q, 1300f);
            Q2.SetSkillshot(0.25f, 70f, 1500f, true, false, SkillshotType.Line);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E, 1000f);
            E.SetSkillshot(0.5f, 200f, float.MaxValue, false, false, SkillshotType.Circle);

            R = new Spell(SpellSlot.R, 1350f);
            R.SetSkillshot(0.25f, 50f, 3000f, false, false, SkillshotType.Cone);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboQ1", "Use Q Extend");
            ComboOption.AddW();
            ComboOption.AddE();

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddBool("HarassQ1", "Use Q Extend");
            HarassOption.AddE();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min Hit Counts >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddE();

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key", Keys.T, KeyBindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(true, false, true, true, true);

            Tick.OnTick += OnUpdate;
            Orbwalker.OnAction += OnAction;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            //Gapcloser.OnGapcloser += OnGapcloser;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Variables.GameTimeTickCount - lastRTime < 3000)
            {
                Orbwalker.AttackState = false;
                Orbwalker.MovementState = false;
                return;
            }

            Orbwalker.AttackState = true;
            Orbwalker.MovementState = true;

            if (Me.IsWindingUp)
            {
                return;
            }

            if (MiscOption.GetKey("R", "SemiR").Active && R.IsReady())
            {
                SemiRLogic();
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

        private static void SemiRLogic()
        {
            var target = MyTargetSelector.GetTarget(R.Range - 150);

            if (target.IsValidTarget(R.Range))
            {
                var rPred = R.GetPrediction(target);

                if (rPred.Hitchance >= HitChance.High)
                {
                    R.Cast(rPred.UnitPosition);
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q2.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                {
                    if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                    {
                        QLogic(target, true);
                    }
                }
            }

            if (KillStealOption.UseE && E.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.E)))
                {
                    if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.Hitchance >= HitChance.High)
                        {
                            E.Cast(ePred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(Q2.Range);

            if (target.IsValidTarget(Q2.Range))
            {
                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q2.Range))
                {
                    QLogic(target, ComboOption.GetBool("ComboQ1").Enabled);
                }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                {
                    var ePred = E.GetPrediction(target);

                    if (ePred.Hitchance >= HitChance.High)
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
                var target = HarassOption.GetTarget(Q2.Range);

                if (target.IsValidTarget(Q2.Range))
                {
                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(Q2.Range))
                    {
                        QLogic(target, HarassOption.GetBool("HarassQ1").Enabled);
                    }

                    if (HarassOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.Hitchance >= HitChance.High)
                        {
                            E.Cast(ePred.UnitPosition);
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
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion()).ToList();

                if (minions.Any())
                {
                    if (LaneClearOption.UseE && E.IsReady())
                    {
                        var eFarm = E.GetCircularFarmLocation(minions);

                        if (eFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearECount").Value)
                        {
                            E.Cast(eFarm.Position);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseE && E.IsReady())
                {
                    var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range) && x.GetJungleType() != JungleType.Unknown).ToList();

                    if (mobs.Any())
                    {
                        var bigmob = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

                        if (bigmob != null && bigmob.IsValidTarget(E.Range) && (!bigmob.InAutoAttackRange() || !Orbwalker.CanAttack()))
                        {
                            E.Cast(bigmob);
                        }
                        else
                        {
                            var eMobs = mobs.Where(x => x.IsValidTarget(E.Range)).ToList();
                            var eFarm = E.GetCircularFarmLocation(eMobs);

                            if (eFarm.MinionsHit >= 2)
                            {
                                E.Cast(eFarm.Position);
                            }
                        }
                    }
                }
            }
        }

        private static void OnAction(object sender, OrbwalkerActionArgs Args)
        {
            if (Args.Type != OrbwalkerType.AfterAttack)
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
                                    Q.CastOnUnit(target);
                                }
                                else if (ComboOption.UseW && W.IsReady())
                                {
                                    W.Cast();
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
                            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                            {
                                if (JungleClearOption.HasEnouguMana())
                                {
                                    if (JungleClearOption.UseQ && Q.IsReady())
                                    {
                                        Q.CastOnUnit(mob);
                                    }
                                    else if (JungleClearOption.UseW && W.IsReady())
                                    {
                                        W.Cast();
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender != null && sender.IsMe)
            {
                if (args.Slot == SpellSlot.R)
                {
                    lastRTime = Variables.GameTimeTickCount;
                    Orbwalker.AttackState = false;
                    Orbwalker.MovementState = false;
                }
            }
        }

        //private static void OnGapcloser(AIHeroClient target, GapcloserArgs Args)
        //{
        //    if (E.IsReady() && target != null && target.IsValidTarget(E.Range))
        //    {
        //        switch (Args.Type)
        //        {
        //            case SpellType.Melee:
        //                if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
        //                {
        //                    var ePred = E.GetPrediction(target);
        //                    E.Cast(ePred.UnitPosition);
        //                }
        //                break;
        //            case SpellType.Dash:
        //            case SpellType.SkillShot:
        //            case SpellType.Targeted:
        //                {
        //                    var ePred = E.GetPrediction(target);
        //                    E.Cast(ePred.UnitPosition);
        //                }
        //                break;
        //        }
        //    }
        //}

        private static void QLogic(AIHeroClient target, bool UseQ1 = false)// SFX Challenger MissFortune QLogic (im so lazy, kappa)
        {
            if (target != null)
            {
                if (target.IsValidTarget(Q.Range))
                {
                    Q.CastOnUnit(target);
                }
                else if (UseQ1 && target.IsValidTarget(Q2.Range) && target.DistanceToPlayer() > Q.Range)
                {
                    var heroPositions = (from t in GameObjects.EnemyHeroes
                                         where t.IsValidTarget(Q2.Range)
                                         let prediction = Q.GetPrediction(t)
                                         select new CPrediction.Position(t, prediction.UnitPosition)).Where(
                        t => t.UnitPosition.Distance(Me.Position) < Q2.Range).ToList();

                    if (heroPositions.Any())
                    {
                        var minions =
                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q2.Range) && (x.GetJungleType() != JungleType.Unknown || x.IsMinion()))
                                .ToList();

                        if (minions.Any(m => m.IsMoving) && !heroPositions.Any(h => h.Hero.HasBuff("missfortunepassive")))
                        {
                            return;
                        }

                        var outerMinions = minions.Where(m => m.Distance(Me) > Q.Range).ToList();
                        var innerPositions = minions.Where(m => m.Distance(Me) < Q.Range).ToList();

                        foreach (var minion in innerPositions)
                        {
                            var lMinion = minion;
                            var coneBuff = new Geometry.Sector(
                                minion.Position,
                                Me.Position.Extend(minion.Position, Me.Distance(minion) + Q.Range * 0.5f),
                                (float)(40 * Math.PI / 180), Q2.Range - Q.Range);
                            var coneNormal = new Geometry.Sector(
                                minion.Position,
                                Me.Position.Extend(minion.Position, Me.Distance(minion) + Q.Range * 0.5f),
                                (float)(60 * Math.PI / 180), Q2.Range - Q.Range);

                            foreach (var enemy in
                                heroPositions.Where(
                                    m => m.UnitPosition.Distance(lMinion.Position) < Q2.Range - Q.Range))
                            {
                                if (coneBuff.IsInside(enemy.Hero) && enemy.Hero.HasBuff("missfortunepassive"))
                                {
                                    Q.CastOnUnit(minion);
                                    return;
                                }
                                if (coneNormal.IsInside(enemy.UnitPosition))
                                {
                                    var insideCone =
                                        outerMinions.Where(m => coneNormal.IsInside(m.Position)).ToList();

                                    if (!insideCone.Any() ||
                                        enemy.UnitPosition.Distance(minion.Position) <
                                        insideCone.Select(
                                                m => m.Position.Distance(minion.Position) - m.BoundingRadius)
                                            .DefaultIfEmpty(float.MaxValue)
                                            .Min())
                                    {
                                        Q.CastOnUnit(minion);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}