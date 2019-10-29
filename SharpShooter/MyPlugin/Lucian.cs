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

    public class Lucian : MyLogic
    {
        private static bool havePassive = false;
        private static int lastCastTime = 0;

        public Lucian()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 650f) { Delay = 0.35f };

            Q2 = new Spell(SpellSlot.Q, 900f);
            Q2.SetSkillshot(0.35f, 25f, int.MaxValue, false, true, SkillshotType.Line);

            W = new Spell(SpellSlot.W, 1000f);
            W.SetSkillshot(0.30f, 80f, 1600f, true, true, SkillshotType.Line);

            W2 = new Spell(SpellSlot.W, 1000f);
            W2.SetSkillshot(0.30f, 80f, 1600f, false, true, SkillshotType.Line);

            E = new Spell(SpellSlot.E, 425f);

            R = new Spell(SpellSlot.R, 1200f);
            R.SetSkillshot(0.10f, 110f, 2500f, true, true, SkillshotType.Line);

            R2 = new Spell(SpellSlot.R, 1200f);
            R2.SetSkillshot(0.10f, 110f, 2500f, false, true, SkillshotType.Line);


            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboQExtend", "Use Q Extend", false);
            ComboOption.AddW();
            ComboOption.AddBool("ComboWLogic", "Use W |Logic Cast");
            ComboOption.AddBool("ComboEDash", "Use E Dash to target");
            ComboOption.AddBool("ComboEReset", "Use E Reset Auto Attack");
            ComboOption.AddBool("ComboESafe", "Use E| Safe Check");
            ComboOption.AddBool("ComboEWall", "Use E| Dont Dash to Wall");
            ComboOption.AddBool("ComboEShort", "Use E| Enabled the Short E Logic");
            ComboOption.AddR();

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddBool("HarassQExtend", "Use Q Extend");
            HarassOption.AddW(false);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddW();
            LaneClearOption.AddE();
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddW();

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiRKey", "Semi-manual R Key", Keys.T, KeyBindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddQExtend(Q2);
            DrawOption.AddW(W);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(true, true, false, true, true);

            Tick.OnTick += OnUpdate;
            AIBaseClient.OnPlayAnimation += OnPlayAnimation;
            Spellbook.OnCastSpell += OnCastSpell;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalker.OnAction += OnAction;
            //Gapcloser.OnGapcloser += OnGapcloser;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                havePassive = false;
                return;
            }

            if (Variables.GameTimeTickCount - lastCastTime >= 3100)
            {
                havePassive = false;
            }

            if (Me.HasBuff("LucianR"))
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                return;
            }

            if (Me.HasBuff("LucianR") || Me.IsDashing())
            {
                return;
            }

            if (Me.IsWindingUp)
            {
                return;
            }

            if (MiscOption.GetKey("R", "SemiRKey").Active && R.IsReady())
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
                    Farm();
                    break;
            }
        }

        private static void SemiRLogic()
        {
            var target = MyTargetSelector.GetTarget(R.Range);

            if (target != null && !target.HaveShiledBuff() && target.IsValidTarget(R.Range))
            {
                R2.Cast(target);
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.IsReady() && Me.Mana > Q.Mana + E.Mana)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(Q2.Range) && !x.IsUnKillable() &&
                            x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                {
                    if (target.IsValidTarget(Q2.Range) && !target.IsUnKillable())
                    {
                        QLogic(target);
                    }
                }
            }

            if (KillStealOption.UseW && W.IsReady() && Me.Mana > Q.Mana + E.Mana + W.Mana)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(W.Range) && !x.IsUnKillable() &&
                            x.Health < Me.GetSpellDamage(x, SpellSlot.W)))
                {
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
        }

        private static void Combo()
        {
            if (ComboOption.UseR && R.IsReady())
            {
                var target = MyTargetSelector.GetTarget(R.Range);

                if (target.IsValidTarget(R.Range) && !target.IsUnKillable() && !Me.IsUnderEnemyTurret() &&
                    !target.IsValidTarget(Me.GetRealAutoAttackRange(target)))
                {
                    if (GameObjects.EnemyHeroes.Any(x => x.NetworkId != target.NetworkId && x.Distance(target) <= 550))
                    {
                        return;
                    }

                    var rAmmo = new float[] { 20, 25, 30 }[Me.Spellbook.GetSpell(SpellSlot.R).Level - 1];
                    var rDMG = GetRDamage(target) * rAmmo;

                    if (target.Health + target.HPRegenRate * 3 < rDMG)
                    {
                        if (target.DistanceToPlayer() <= 800 && target.Health < rDMG * 0.6)
                        {
                            R.Cast(target);
                            return;
                        }

                        if (target.DistanceToPlayer() <= 1000 && target.Health < rDMG * 0.4)
                        {
                            R.Cast(target);
                        }
                    }
                }
            }

            if (havePassive || Me.Buffs.Any(x => x.Name.ToLower() == "lucianpassivebuff") || Me.IsDashing())
            {
                return;
            }

            if (ComboOption.GetBool("ComboEDash").Enabled && E.IsReady())
            {
                var target = MyTargetSelector.GetTarget(950);

                if (target.IsValidTarget(950) && !target.IsValidTarget(550))
                {
                    DashELogic(target);
                }
            }

            if (Q.IsReady() && !havePassive && Me.Buffs.All(x => x.Name.ToLower() != "lucianpassivebuff") && !Me.IsDashing())
            {
                var target = MyTargetSelector.GetTarget(Q2.Range);

                if (ComboOption.UseQ)
                {
                    if (target.IsValidTarget(Q2.Range))
                    {
                        QLogic(target, ComboOption.GetBool("ComboQExtend").Enabled);
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
                    var target = HarassOption.GetTarget(Q2.Range);

                    if (target != null && target.IsValidTarget(Q2.Range))
                    {
                        QLogic(target, HarassOption.GetBool("HarassQExtend").Enabled);
                    }
                }

                if (HarassOption.UseW && W.IsReady())
                {
                    var target = HarassOption.GetTarget(W.Range);

                    if (target != null && target.IsValidTarget(W.Range))
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.Hitchance >= HitChance.High)
                        {
                            W.Cast(wPred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Farm()
        {
            if (MyManaManager.SpellHarass)
            {
                Harass();
            }

            if (MyManaManager.SpellFarm)
            {
                LaneClear();
            }
        }

        private static void LaneClear()
        {
            if (Variables.GameTimeTickCount - lastCastTime < 600 + Game.Ping ||
                Me.Buffs.Any(x => x.Name.ToLower() == "lucianpassivebuff"))
            {
                return;
            }

            if (LaneClearOption.HasEnouguMana())
            {
                if (LaneClearOption.UseQ && Q.IsReady())
                {
                    var qMinions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToList();

                    if (qMinions.Any())
                    {
                        foreach (var minion in qMinions)
                        {
                            var q2Minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q2.Range) && x.IsMinion()).ToList();

                            if (minion != null && minion.IsValidTarget(Q.Range) &&
                                Q2.GetHitCounts(q2Minions, Me.PreviousPosition.Extend(minion.PreviousPosition, 900)) >= 2)
                            {
                                Q.CastOnUnit(minion);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static void OnPlayAnimation(AIBaseClient sender, AIBaseClientPlayAnimationEventArgs Args)
        {
            if (!sender.IsMe || Orbwalker.ActiveMode == OrbwalkerMode.None)
            {
                return;
            }

            if (Args.Animation == "Spell1" || Args.Animation == "Spell2")
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs Args)
        {
            if (sender?.Owner != null && sender.Owner.IsMe)
            {
                if (Args.Slot == SpellSlot.Q || Args.Slot == SpellSlot.W || Args.Slot == SpellSlot.E)
                {
                    havePassive = true;
                    lastCastTime = Variables.GameTimeTickCount;
                }

                if (Args.Slot == SpellSlot.E && Orbwalker.ActiveMode != OrbwalkerMode.None)
                {
                    Orbwalker.ResetAutoAttackTimer();
                }
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs Args)
        {
            if (sender.IsMe)
            {
                if (Args.Slot == SpellSlot.Q || Args.Slot == SpellSlot.W || Args.Slot == SpellSlot.E)
                {
                    havePassive = true;
                    lastCastTime = Variables.GameTimeTickCount;
                }
            }
        }

        private static void OnAction(object sender, OrbwalkerActionArgs Args)
        {
            if (Args.Type != OrbwalkerType.AfterAttack)
            {
                return;
            }

            havePassive = false;

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
                            var target = Args.Target as AIHeroClient;

                            if (target != null && target.IsValidTarget())
                            {
                                if (ComboOption.GetBool("ComboEReset").Enabled && E.IsReady())
                                {
                                    ResetELogic(target);
                                }
                                else if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                                {
                                    Q.CastOnUnit(target);
                                }
                                else if (ComboOption.UseW && W.IsReady())
                                {
                                    if (ComboOption.GetBool("ComboWLogic").Enabled)
                                    {
                                        W2.Cast(target.PreviousPosition);
                                    }
                                    else
                                    {
                                        var wPred = W.GetPrediction(target);

                                        if (wPred.Hitchance >= HitChance.High)
                                        {
                                            W.Cast(wPred.UnitPosition);
                                        }
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
                            if (mob != null && mob.IsValidTarget() && mob.GetJungleType() != JungleType.Unknown && MyManaManager.SpellFarm && JungleClearOption.HasEnouguMana())
                            {
                                if (JungleClearOption.UseE && E.IsReady())
                                {
                                    E.Cast(Me.PreviousPosition.Extend(Game.CursorPos, 130));
                                }
                                else if (JungleClearOption.UseQ && Q.IsReady())
                                {
                                    Q.CastOnUnit(mob);
                                }
                                else if (JungleClearOption.UseW && W.IsReady())
                                {
                                    W2.Cast(mob.PreviousPosition);
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
                                    E.Cast(Me.PreviousPosition.Extend(Game.CursorPos, 130));
                                }
                                else if (LaneClearOption.UseW && W.IsReady())
                                {
                                    W.Cast(Game.CursorPos);
                                }
                            }
                        }
                    }
                    break;
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

        private static void QLogic(AIHeroClient target, bool useExtendQ = true)
        {
            if (!Q.IsReady() || target == null || target.IsDead || target.IsUnKillable())
            {
                return;
            }

            if (target.IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(target);
            }
            else if (target.IsValidTarget(Q2.Range) && useExtendQ)
            {
                var collisions =
                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && (x.IsMinion() || x.GetJungleType() != JungleType.Unknown))
                        .ToList();

                if (!collisions.Any())
                {
                    return;
                }

                foreach (var minion in collisions)
                {
                    var qPred = Q2.GetPrediction(target);
                    var qPloygon = new Geometry.Rectangle(Me.PreviousPosition, Me.PreviousPosition.Extend(minion.Position, Q2.Range), Q2.Width);

                    if (qPloygon.IsInside(qPred.UnitPosition.ToVector2()) && minion.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(minion);
                        break;
                    }
                }
            }
        }

        private static void DashELogic(AIBaseClient target)
        {
            if (target.DistanceToPlayer() <= Me.GetRealAutoAttackRange(target) ||
                target.DistanceToPlayer() > Me.GetRealAutoAttackRange(target) + E.Range)
            {
                return;
            }

            var dashPos = Me.PreviousPosition.Extend(Game.CursorPos, E.Range);
            if (dashPos.IsWall() && ComboOption.GetBool("ComboEWall").Enabled)
            {
                return;
            }

            if (dashPos.CountEnemyHeroesInRange(500) >= 3 && dashPos.CountAllyHeroesInRange(400) < 3 &&
                ComboOption.GetBool("ComboESafe").Enabled)
            {
                return;
            }

            var realRange = Me.BoundingRadius + target.BoundingRadius + Me.AttackRange;
            if (Me.PreviousPosition.DistanceToCursor() > realRange * 0.60 &&
                !target.InAutoAttackRange() &&
                target.PreviousPosition.Distance(dashPos) < realRange - 45)
            {
                E.Cast(dashPos);
            }
        }

        private static void ResetELogic(AIBaseClient target)
        {
            var dashRange = ComboOption.GetBool("ComboEShort").Enabled
                ? (Me.PreviousPosition.DistanceToCursor() > Me.GetRealAutoAttackRange(target) ? E.Range : 130)
                : E.Range;
            var dashPos = Me.PreviousPosition.Extend(Game.CursorPos, dashRange);

            if (dashPos.IsWall() && ComboOption.GetBool("ComboEWall").Enabled)
            {
                return;
            }

            if (dashPos.CountEnemyHeroesInRange(500) >= 3 && dashPos.CountAllyHeroesInRange(400) < 3 &&
                ComboOption.GetBool("ComboESafe").Enabled)
            {
                return;
            }

            E.Cast(dashPos);
        }

        private static double GetRDamage(AIBaseClient target)
        {
            if (Me.Spellbook.GetSpell(SpellSlot.R).Level == 0 || Me.Spellbook.GetSpell(SpellSlot.R).State != SpellState.Ready)
            {
                return 0f;
            }

            var rDMG = new double[] { 20, 35, 50 }[Me.Spellbook.GetSpell(SpellSlot.R).Level - 1] +
                0.1 * Me.TotalMagicalDamage + 0.2 * Me.FlatPhysicalDamageMod;

            return Me.CalculateDamage(target, DamageType.Magical, rDMG);
        }
    }
}
