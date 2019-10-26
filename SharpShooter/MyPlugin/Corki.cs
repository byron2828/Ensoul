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

    public class Corki : MyLogic
    {
        private static float rRange => Me.HasBuff("CorkiMissileBarrageCounterBig") ? 1500f : 1300f;

        public Corki()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 825f);
            Q.SetSkillshot(0.30f, 200f, 1000f, false, false, SkillshotType.Circle);

            W = new Spell(SpellSlot.W, 800f);

            E = new Spell(SpellSlot.E, 600f);

            R = new Spell(SpellSlot.R, rRange);
            R.SetSkillshot(0.20f, 50f, 2000f, true, false, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRLimit", "Use R|Limit Stack >= x", 0, 0, 7);
            ComboOption.AddSlider("ComboRHP", "Use R|Target HealthPercent <= x%", 100, 1, 101);

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddR();
            HarassOption.AddSlider("HarassRLimit", "Use R|Limit Stack >= x", 4, 0, 7);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q|Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E|Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddR();
            LaneClearOption.AddSlider("LaneClearRCount", "Use R|Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddSlider("LaneClearRLimit", "Use R|Limit Stack >= x", 4, 0, 7);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddR();
            JungleClearOption.AddSlider("JungleClearRLimit", "Use R|Limit Stack >= x", 0, 0, 7);
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddR();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key", Keys.T, KeyBindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(true, false, true, true, true);

            Tick.OnTick += OnUpdate;
            Orbwalker.OnAction += OnAction;
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

            if (R.Level > 0)
            {
                R.Range = rRange;
            }

            if (MiscOption.GetKey("R", "SemiR").Active)
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
            Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (R.IsReady() && R.Ammo > 0)
            {
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
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }
            }

            if (KillStealOption.UseR && R.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(R.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.R)))
                {
                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.Hitchance >= HitChance.High)
                        {
                            R.Cast(rPred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(R.Range);

            if (target.IsValidTarget(R.Range) && !target.IsUnKillable() && (!target.InAutoAttackRange() || !Orbwalker.CanAttack()))
            {
                if (ComboOption.UseR && R.IsReady() &&
                    R.Ammo >= ComboOption.GetSlider("ComboRLimit").Value &&
                    target.IsValidTarget(R.Range) && target.HealthPercent <= ComboOption.GetSlider("ComboRHP").Value)
                {
                    var rPred = R.GetPrediction(target);

                    if (rPred.Hitchance >= HitChance.High)
                    {
                        R.Cast(rPred.UnitPosition);
                    }
                    else if (rPred.Hitchance == HitChance.Collision)
                    {
                        foreach (var collsion in rPred.CollisionObjects.Where(x => x.IsValidTarget(R.Range)))
                        {
                            if (collsion.DistanceSquared(target) <= Math.Pow(85, 2))
                            {
                                R.Cast(collsion.PreviousPosition);
                            }
                        }
                    }
                }

                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    var qPred = Q.GetPrediction(target);

                    if (qPred.Hitchance >= HitChance.High)
                    {
                        Q.Cast(qPred.CastPosition);
                    }
                }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                {
                    E.Cast(Me.PreviousPosition);
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                var target = HarassOption.GetTarget(R.Range);

                if (target.IsValidTarget(R.Range))
                {
                    if (HarassOption.UseR && R.IsReady() &&
                        R.Ammo >= HarassOption.GetSlider("HarassRLimit").Value &&
                        target.IsValidTarget(R.Range))
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.Hitchance >= HitChance.High)
                        {
                            R.Cast(rPred.UnitPosition);
                        }
                    }

                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }

                    if (HarassOption.UseE && E.IsReady() && target.InAutoAttackRange())
                    {
                        E.Cast(Me.PreviousPosition);
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
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToList();
                    if (minions.Any())
                    {
                        var qFarm = Q.GetCircularFarmLocation(minions);

                        if (qFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearQCount").Value)
                        {
                            Q.Cast(qFarm.Position);
                        }
                    }
                }

                if (LaneClearOption.UseE && E.IsReady())
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion()).ToList();

                    if (minions.Any() && minions.Count >= LaneClearOption.GetSlider("LaneClearECount").Value)
                    {
                        E.Cast(Me.PreviousPosition);
                    }
                }

                if (LaneClearOption.UseR && R.IsReady() &&
                    R.Ammo >= LaneClearOption.GetSlider("LaneClearRLimit").Value)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(R.Range) && x.IsMinion()).ToList();

                    if (minions.Any())
                    {
                        var rFarm = R.GetLineFarmLocation(minions);

                        if (rFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearRCount").Value)
                        {
                            R.Cast(rFarm.Position);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                var mobs =
                    GameObjects.Jungle.Where(x => x.IsValidTarget(R.Range) && x.GetJungleType() != JungleType.Unknown && !x.InAutoAttackRange())
                        .OrderByDescending(x => x.MaxHealth)
                        .ToList();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseR && R.IsReady() &&
                        R.Ammo >= JungleClearOption.GetSlider("JungleClearRLimit").Value &&
                        mob.IsValidTarget(R.Range) && !mob.InAutoAttackRange())
                    {
                        R.CastIfHitchanceEquals(mob, HitChance.Medium);
                    }

                    if (JungleClearOption.UseQ && Q.IsReady() &&
                        mob.IsValidTarget(Q.Range) && !mob.InAutoAttackRange())
                    {
                        Q.CastIfHitchanceEquals(mob, HitChance.Medium);
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
                        if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                        {
                            var target = (AIHeroClient)Args.Target;
                            if (target != null && target.IsValidTarget() && !target.IsUnKillable())
                            {
                                if (ComboOption.UseR && R.IsReady() &&
                                    R.Ammo >= ComboOption.GetSlider("ComboRLimit").Value &&
                                    target.IsValidTarget(R.Range) &&
                                    target.HealthPercent <= ComboOption.GetSlider("ComboRHP").Value)
                                {
                                    var rPred = R.GetPrediction(target);

                                    if (rPred.Hitchance >= HitChance.High)
                                    {
                                        R.Cast(rPred.UnitPosition);
                                    }
                                }
                                else if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.Hitchance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                                else if (ComboOption.UseE && E.IsReady() && target.InAutoAttackRange())
                                {
                                    E.Cast(Me.PreviousPosition);
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
                                if (mob != null && mob.IsValidTarget() && mob.GetJungleType() != JungleType.Unknown)
                                {
                                    if (JungleClearOption.HasEnouguMana())
                                    {
                                        if (JungleClearOption.UseR && R.IsReady() &&
                                            R.Ammo >=
                                            JungleClearOption.GetSlider("JungleClearRLimit").Value)
                                        {
                                            R.CastIfHitchanceEquals(mob,  HitChance.Medium);
                                        }
                                        else if (JungleClearOption.UseQ && Q.IsReady() && mob.IsValidTarget(Q.Range))
                                        {
                                            Q.CastIfHitchanceEquals(mob, HitChance.Medium);
                                        }
                                        else if (JungleClearOption.UseE && E.IsReady() && mob.InAutoAttackRange())
                                        {
                                            E.Cast(Me.PreviousPosition);
                                        }
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