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

    public class Ezreal : MyLogic
    {
        public Ezreal()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 1150f);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, false, SkillshotType.Line);

            W = new Spell(SpellSlot.W, 950f);
            W.SetSkillshot(0.25f, 60f, 1200f, false, false, SkillshotType.Line);

            E = new Spell(SpellSlot.E, 475f) { Delay = 0.65f };

            R = new Spell(SpellSlot.R, 5000f);
            R.SetSkillshot(1.05f, 160f, 2200f, false, false, SkillshotType.Line);

            EQ = new Spell(SpellSlot.Q, 1625f);
            EQ.SetSkillshot(0.90f, 60f, 1350f, true, false, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddBool("ComboECheck", "Use E |Safe Check");
            ComboOption.AddBool("ComboEWall", "Use E |Wall Check");
            ComboOption.AddR();

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddW();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddBool("LaneClearQLH", "Use Q| Only LastHit", false);
            LaneClearOption.AddW();
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddMana();

            LastHitOption.AddMenu();
            LastHitOption.AddQ();
            LastHitOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddR();
            MiscOption.AddBool("R", "AutoR", "Auto R");
            MiscOption.AddSlider("R", "RRange", "Auto R |Min Cast Range >= x", 800, 0, 1500);
            MiscOption.AddSlider("R", "RMaxRange", "Auto R |Max Cast Range >= x", 3000, 1500, 5000);
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key", Keys.T, KeyBindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddDamageIndicatorToHero(true, true, true, true, true);

            Tick.OnTick += OnUpdate;
            //Gapcloser.OnGapcloser += OnGapcloser;
            Orbwalker.OnAction += OnAction;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (R.Level > 0)
            {
                R.Range = MiscOption.GetSlider("R", "RMaxRange").Value;
            }

            if (MiscOption.GetKey("R", "SemiR").Active)
            {
                OneKeyCastR();
            }

            if (MiscOption.GetBool("R", "AutoR").Enabled && R.IsReady() && Me.CountEnemyHeroesInRange(1000) == 0)
            {
                AutoRLogic();
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
                case OrbwalkerMode.LastHit:
                    LastHit();
                    break;
            }
        }

        private static void OneKeyCastR()
        {
            Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (!R.IsReady())
            {
                return;
            }

            var target = MyTargetSelector.GetTarget(R.Range);
            if (target.IsValidTarget(R.Range) && !target.IsValidTarget(MiscOption.GetSlider("R", "RRange").Value))
            {
                var rPred = R.GetPrediction(target);

                if (rPred.Hitchance >= HitChance.High)
                {
                    R.Cast(rPred.CastPosition);
                }
            }
        }

        private static void AutoRLogic()
        {
            foreach (
                var target in
                GameObjects.EnemyHeroes.Where(
                    x =>
                        x.IsValidTarget(R.Range) && x.DistanceToPlayer() >= MiscOption.GetSlider("R", "RRange").Value))
            {
                if (!target.CanMoveMent() && target.IsValidTarget(EQ.Range) &&
                    Me.GetSpellDamage(target, SpellSlot.R) + Me.GetSpellDamage(target, SpellSlot.Q) * 3 >=
                    target.Health + target.HPRegenRate * 2)
                {
                    R.Cast(target);
                }

                if (Me.GetSpellDamage(target, SpellSlot.R) > target.Health + target.HPRegenRate * 2 &&
                    target.Path.Count < 2 &&
                    R.GetPrediction(target).Hitchance >= HitChance.High)
                {
                    R.Cast(target);
                }
            }
        }

        private static void KillSteal()
        {
            foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range)))
            {
                if (KillStealOption.UseQ && Me.GetSpellDamage(target, SpellSlot.Q) > target.Health &&
                    target.IsValidTarget(Q.Range))
                {
                    if (!target.IsUnKillable())
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(EQ.Range);

            if (target.IsValidTarget(EQ.Range))
            {
                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(EQ.Range))
                {
                    ComboELogic(target);
                }

                if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range))
                {
                    var wPred = W.GetPrediction(target);

                    if (wPred.Hitchance >= HitChance.High)
                    {
                        if (Q.IsReady())
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.Hitchance >= HitChance.High)
                            {
                                W.Cast(qPred.CastPosition);
                            }
                        }

                        if (Orbwalker.CanAttack() && target.InAutoAttackRange())
                        {
                            W.Cast(wPred.CastPosition);
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

                if (ComboOption.UseR && R.IsReady())
                {
                    if (Me.IsUnderEnemyTurret() || Me.CountEnemyHeroesInRange(800) > 1)
                    {
                        return;
                    }

                    foreach (var rTarget in GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(R.Range) &&
                            x.DistanceToPlayer() >= MiscOption.GetSlider("R", "RRange").Value))
                    {
                        if (rTarget.Health < Me.GetSpellDamage(rTarget, SpellSlot.R) &&
                            R.GetPrediction(rTarget).Hitchance >= HitChance.High &&
                            rTarget.DistanceToPlayer() > Q.Range + E.Range / 2)
                        {
                            R.Cast(target);
                        }

                        if (rTarget.IsValidTarget(Q.Range + E.Range) &&
                            Me.GetSpellDamage(rTarget, SpellSlot.R) +
                            (Q.IsReady() ? Me.GetSpellDamage(rTarget, SpellSlot.Q) : 0) +
                            (W.IsReady() ? Me.GetSpellDamage(rTarget, SpellSlot.W) : 0) >
                            rTarget.Health + rTarget.HPRegenRate * 2)
                        {
                            R.Cast(rTarget);
                        }
                    }
                }
            }
        }

        private static void ComboELogic(AIHeroClient target)
        {
            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            if (ComboOption.GetBool("ComboECheck").Enabled && !Me.IsUnderEnemyTurret() && Me.CountEnemyHeroesInRange(1200f) <= 2)
            {
                if (target.DistanceToPlayer() > Me.GetRealAutoAttackRange(target) && target.IsValidTarget())
                {
                    if (target.Health < Me.GetSpellDamage(target, SpellSlot.E) + Me.GetAutoAttackDamage(target) &&
                        target.PreviousPosition.Distance(Game.CursorPos) < Me.PreviousPosition.Distance(Game.CursorPos))
                    {
                        var CastEPos = Me.PreviousPosition.Extend(target.PreviousPosition, 475f);

                        if (ComboOption.GetBool("ComboEWall").Enabled)
                        {
                            if (!CastEPos.IsWall())
                            {
                                E.Cast(Me.PreviousPosition.Extend(target.PreviousPosition, 475f));
                            }
                        }
                        else
                        {
                            E.Cast(Me.PreviousPosition.Extend(target.PreviousPosition, 475f));
                        }
                        return;
                    }

                    if (target.Health <
                        Me.GetSpellDamage(target, SpellSlot.E) + Me.GetSpellDamage(target, SpellSlot.W) &&
                        W.IsReady() &&
                        target.PreviousPosition.Distance(Game.CursorPos) + 350 < Me.PreviousPosition.Distance(Game.CursorPos))
                    {
                        var CastEPos = Me.PreviousPosition.Extend(target.PreviousPosition, 475f);

                        if (ComboOption.GetBool("ComboEWall").Enabled)
                        {
                            if (!CastEPos.IsWall())
                            {
                                E.Cast(Me.PreviousPosition.Extend(target.PreviousPosition, 475f));
                            }
                        }
                        else
                        {
                            E.Cast(Me.PreviousPosition.Extend(target.PreviousPosition, 475f));
                        }
                        return;
                    }

                    if (target.Health <
                        Me.GetSpellDamage(target, SpellSlot.E) + Me.GetSpellDamage(target, SpellSlot.Q) &&
                        Q.IsReady() &&
                        target.PreviousPosition.Distance(Game.CursorPos) + 300 < Me.PreviousPosition.Distance(Game.CursorPos))
                    {
                        var CastEPos = Me.PreviousPosition.Extend(target.PreviousPosition, 475f);

                        if (ComboOption.GetBool("ComboEWall").Enabled)
                        {
                            if (!CastEPos.IsWall())
                            {
                                E.Cast(Me.PreviousPosition.Extend(target.PreviousPosition, 475f));
                            }
                        }
                        else
                        {
                            E.Cast(Me.PreviousPosition.Extend(target.PreviousPosition, 475f));
                        }
                    }
                }
            }

        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseQ && Q.IsReady())
                {
                    var target = HarassOption.GetTarget(Q.Range);
                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        var qPred = Q.GetPrediction(target);
                        if (qPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void FarmHarass()
        {
            if (Me.IsWindingUp)
            {
                return;
            }

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
                        foreach (var minion in minions.Where(x => !x.IsDead && x.Health > 0))
                        {
                            if (LaneClearOption.GetBool("LaneClearQLH").Enabled)
                            {
                                if (minion.Health < Me.GetSpellDamage(minion, SpellSlot.Q))
                                {
                                    Q.Cast(minion);
                                }
                            }
                            else
                            {
                                Q.Cast(minion);
                            }
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseW && W.IsReady())
                {
                    var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() == JungleType.Legendary)
                        .OrderByDescending(x => x.MaxHealth)
                        .ToList();
                    foreach (var mob in mobs)
                    {
                        W.CastIfHitchanceEquals(mob, HitChance.Medium);
                    }
                }

                if (JungleClearOption.UseQ && Q.IsReady())
                {
                    var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() != JungleType.Unknown)
                        .OrderByDescending(x => x.MaxHealth)
                        .ToList();
                    foreach (var mob in mobs)
                    {
                        Q.CastIfHitchanceEquals(mob, HitChance.Medium);
                    }
                }
            }
        }

        private static void LastHit()
        {
            if (LastHitOption.HasEnouguMana)
            {
                if (LastHitOption.UseQ && Q.IsReady())
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                            .Where(
                                x =>
                                    x.DistanceToPlayer() <= Q.Range &&
                                    x.DistanceToPlayer() > Me.GetRealAutoAttackRange(x) &&
                                    x.Health < Me.GetSpellDamage(x, SpellSlot.Q)).ToList();

                    if (minions.Any())
                    {
                        Q.CastIfHitchanceEquals(minions[0], HitChance.Medium);
                    }
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
        //                {
        //                    if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
        //                    {
        //                        E.Cast(Me.PreviousPosition.Extend(target.PreviousPosition, -E.Range));
        //                    }
        //                }
        //                break;
        //            case SpellType.Dash:
        //                {
        //                    if (Args.EndPosition.DistanceToPlayer() <= 250 ||
        //                        target.PreviousPosition.DistanceToPlayer() <= 300)
        //                    {
        //                        E.Cast(Me.PreviousPosition.Extend(target.PreviousPosition, -E.Range));
        //                    }
        //                }
        //                break;
        //            case SpellType.SkillShot:
        //            case SpellType.Targeted:
        //                {
        //                    if (target.PreviousPosition.DistanceToPlayer() <= 300)
        //                    {
        //                        E.Cast(Me.PreviousPosition.Extend(target.PreviousPosition, -E.Range));
        //                    }
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
                        if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                        {
                            var target = (AIHeroClient) Args.Target;
                            if (target != null && target.IsValidTarget(W.Range))
                            {
                                if (ComboOption.UseW && W.IsReady())
                                {
                                    var pred = W.GetPrediction(target);
                                    if (pred.Hitchance >= HitChance.High)
                                    {
                                        W.Cast(pred.CastPosition);
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
                        if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && MyManaManager.SpellFarm &&
                            LaneClearOption.HasEnouguMana(true))
                        {
                            if (LaneClearOption.UseW && W.IsReady())
                            {
                                W.Cast(Args.Target.Position);
                            }
                        }
                    }
                        break;
                }
            }

            if (Args.Type == OrbwalkerType.AfterAttack)
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
                                    if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                                    {
                                        var qPred = Q.GetPrediction(target);
                                        if (qPred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(qPred.CastPosition);
                                        }
                                    }
                                }
                                else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass ||
                                         Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && MyManaManager.SpellHarass)
                                {
                                    if (!HarassOption.HasEnouguMana() ||
                                        !HarassOption.GetHarassTargetEnabled(target.CharacterName))
                                    {
                                        return;
                                    }

                                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                                    {
                                        var qPred = Q.GetPrediction(target);
                                        if (qPred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(qPred.CastPosition);
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