namespace SharpShooter.MyPlugin
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SharpDX;

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

    public class Caitlyn : MyLogic
    {
        private static int lastQTime, lastWTime;

        private static float rRange => 500f * Me.Spellbook.GetSpell(SpellSlot.R).Level + 1500f;

        public Caitlyn()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 1250f);
            Q.SetSkillshot(0.70f, 50f, 2000f, false, false, SkillshotType.Line);

            W = new Spell(SpellSlot.W, 800f);
            W.SetSkillshot(0.80f, 80f, 2000f, false, false, SkillshotType.Circle);

            E = new Spell(SpellSlot.E, 750f);
            E.SetSkillshot(0.25f, 60f, 1600f, true, false, SkillshotType.Line);

            R = new Spell(SpellSlot.R, rRange) {Delay = 1.5f};

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddSlider("ComboQCount", "Use Q |Min Hit Count >= x(0 = Off)", 3, 0, 5);
            ComboOption.AddSlider("ComboQRange", "UseQ |Min Cast Range >= x", 800, 500, 1100);
            ComboOption.AddW();
            ComboOption.AddSlider("ComboWCount", "Use W|Min Stack >= x", 1, 1, 3);
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSafe", "Use R|Safe Check");
            ComboOption.AddSlider("ComboRRange", "Use R|Min Cast Range >= x", 900, 500, 1500);

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q|Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddQ();
            MiscOption.AddBool("Q", "AutoQ", "Use Q| CC");
            MiscOption.AddW();
            MiscOption.AddBool("W", "AutoWCC", "Use W| CC");
            MiscOption.AddBool("W", "AutoWTP", "Use W| TP");
            MiscOption.AddE();
            MiscOption.AddBool("E", "AutoE", "Use E| Anti Gapcloser");
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key", Keys.T, KeyBindType.Press);
            //MiscOption.AddSetting("EQ");
            //MiscOption.AddKey("EQKey", "Semi-manual EQ Key", KeyCode.G, KeyBindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(true, false, true, true, true);

            Tick.OnTick += OnUpdate;
            //Gapcloser.OnGapcloser += OnGapcloser;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
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

            R.Range = rRange;

            //if (MiscOption.GetKey("EQKey").Enabled)
            //{
            //    OneKeyEQ();
            //}

            if (MiscOption.GetKey("R", "SemiR").Active && R.IsReady())
            {
                OneKeyCastR();
            }

            Auto();
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


        private static void OneKeyCastR()
        {
            var target = MyTargetSelector.GetTarget(R.Range);

            if (target != null && target.IsValidTarget(R.Range))
            {
                R.CastOnUnit(target);
            }
        }

        private static void Auto()
        {
            if (MiscOption.GetBool("Q", "AutoQ").Enabled && Q.IsReady() &&
                Orbwalker.ActiveMode != OrbwalkerMode.Combo && Orbwalker.ActiveMode != OrbwalkerMode.Harass)
            {
                var target = MyTargetSelector.GetTarget(Q.Range - 50);

                if (target.IsValidTarget(Q.Range) && !target.CanMoveMent())
                {
                    Q.Cast(target.PreviousPosition);
                }
            }

            if (W.IsReady())
            {
                if (MiscOption.GetBool("W", "AutoWCC").Enabled)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x => x.IsValidTarget(W.Range) && !x.CanMoveMent() && !x.HasBuff("caitlynyordletrappublic")))
                    {
                        if (Variables.GameTimeTickCount - lastWTime > 1500)
                        {
                            W.Cast(target.PreviousPosition);
                        }
                    }
                }

                if (MiscOption.GetBool("W", "AutoWTP").Enabled)
                {
                    var obj =
                        ObjectManager
                            .Get<AIBaseClient>()
                            .FirstOrDefault(x => !x.IsAlly && !x.IsMe && x.DistanceToPlayer() <= W.Range &&
                                                 x.Buffs.Any(
                                                     a =>
                                                         a.Name.ToLower().Contains("teleport") ||
                                                         a.Name.ToLower().Contains("gate")) &&
                                                 !ObjectManager.Get<AIBaseClient>()
                                                     .Any(b => b.Name.ToLower().Contains("trap") && b.Distance(x) <= 150));

                    if (obj != null)
                    {
                        if (Variables.GameTimeTickCount - lastWTime > 1500)
                        {
                            W.Cast(obj.PreviousPosition);
                        }
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
                    if (target.InAutoAttackRange() && target.Health <= Me.GetAutoAttackDamage(target) * 2)
                    {
                        continue;
                    }

                    if (!target.IsUnKillable())
                    {
                        Q.Cast(target.PreviousPosition);
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(R.Range);

            if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
            {
                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(700))
                {
                    var ePred = E.GetPrediction(target);

                    if (!ePred.CollisionObjects.Any() || ePred.Hitchance >= HitChance.High)
                    {
                        if (ComboOption.UseQ && Q.IsReady())
                        {
                            if (E.Cast(ePred.CastPosition))
                            {

                            }
                        }
                        else
                        {
                            E.Cast(ePred.CastPosition);
                        }
                    }
                    else
                    {
                        if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range) && !Me.IsDashing())
                        {
                            if (Me.CountEnemyHeroesInRange(ComboOption.GetSlider("ComboQRange").Value) < 0)
                            {
                                var qPred = Q.GetPrediction(target);

                                if (qPred.Hitchance >= HitChance.High)
                                {
                                    Q.Cast(qPred.CastPosition);
                                }

                                if (ComboOption.GetSlider("ComboQCount").Value != 0 &&
                                    Me.CountEnemyHeroesInRange(Q.Range) >= ComboOption.GetSlider("ComboQCount").Value)
                                {
                                    Q.CastIfWillHit(target, ComboOption.GetSlider("ComboQCount").Value);
                                }
                            }
                        }
                    }
                }

                if (ComboOption.UseQ && Q.IsReady() && !E.IsReady() && target.IsValidTarget(Q.Range) && !Me.IsDashing())
                {
                    if (Me.CountEnemyHeroesInRange(ComboOption.GetSlider("ComboQRange").Value) < 0)
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }

                        if (ComboOption.GetSlider("ComboQCount").Value != 0 &&
                            Me.CountEnemyHeroesInRange(Q.Range) >= ComboOption.GetSlider("ComboQCount").Value)
                        {
                            Q.CastIfWillHit(target, ComboOption.GetSlider("ComboQCount").Value);
                        }
                    }
                }

                if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range) &&
                    W.Ammo >= ComboOption.GetSlider("ComboWCount").Value)
                {
                    if (Variables.GameTimeTickCount - lastWTime > 1800 + Game.Ping * 2)
                    {
                        if (target.CanMoveMent())
                        {
                            if (target.IsFacing(Me))
                            {
                                if (target.IsMelee && target.DistanceToPlayer() < target.AttackRange + 100)
                                {
                                    CastW(Me.PreviousPosition);
                                }
                                else
                                {
                                    var wPred = W.GetPrediction(target);

                                    if (wPred.Hitchance >= HitChance.High && target.IsValidTarget(W.Range))
                                    {
                                        CastW(wPred.CastPosition);
                                    }
                                }
                            }
                            else
                            {
                                var wPred = W.GetPrediction(target);

                                if (wPred.Hitchance >= HitChance.High && target.IsValidTarget(W.Range))
                                {
                                    CastW(wPred.CastPosition +
                                          Vector3.Normalize(target.PreviousPosition - Me.PreviousPosition) * 100);
                                }
                            }
                        }
                        else
                        {
                            if (target.IsValidTarget(W.Range))
                            {
                                CastW(target.PreviousPosition);
                            }
                        }
                    }
                }

                if (ComboOption.UseR && R.IsReady() && Variables.GameTimeTickCount - lastQTime > 2500)
                {
                    if (ComboOption.GetBool("ComboRSafe").Enabled &&
                        (Me.IsUnderEnemyTurret() || Me.CountEnemyHeroesInRange(1000) > 2))
                    {
                        return;
                    }

                    if (!target.IsValidTarget(R.Range))
                    {
                        return;
                    }

                    if (target.DistanceToPlayer() < ComboOption.GetSlider("ComboRRange").Value)
                    {
                        return;
                    }

                    if (target.Health + target.HPRegenRate * 3 > Me.GetSpellDamage(target, SpellSlot.R))
                    {
                        return;
                    }

                    var RCollision =
                        SpellPrediction.GetCollsionsObjects(new List<Vector3> {target.PreviousPosition},
                                new SpellPrediction.PredictionInput
                                {
                                    Delay = R.Delay,
                                    Radius = 500,
                                    Speed = 1500,
                                    From = ObjectManager.Player.PreviousPosition,
                                    Unit = target,
                                    CollisionObjects = CollisionObjects.YasuoWall | CollisionObjects.Heroes
                                })
                            .Any(x => x.NetworkId != target.NetworkId);

                    if (RCollision)
                    {
                        return;
                    }

                    R.CastOnUnit(target);
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

                    if (target.IsValidTarget(Q.Range))
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
                        var qFarm = Q.GetLineFarmLocation(minions);

                        if (qFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearQCount").Value)
                        {
                            Q.Cast(qFarm.Position);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseQ && Q.IsReady())
                {
                    var mobs =
                        GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() != JungleType.Unknown)
                            .OrderByDescending(x => x.MaxHealth)
                            .ToList();

                    if (mobs.Any())
                    {
                        Q.CastIfHitchanceEquals(mobs[0], HitChance.Medium);
                    }
                }
            }
        }

        private static void OneKeyEQ()
        {
            Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (E.IsReady() && Q.IsReady())
            {
                var target = MyTargetSelector.GetTarget(E.Range);

                if (target.IsValidTarget(E.Range))
                {
                    var ePred = E.GetPrediction(target);
                    if (ePred.CollisionObjects.Count == 0)
                    {
                        E.Cast(target);
                        Q.Cast(target);
                    }
                }
            }
        }

        //private static void OnGapcloser(AIHeroClient target, GapcloserArgs Args)
        //{
        //    if (MiscOption.GetBool("E", "AutoE").Enabled && target != null && target.IsValidTarget() && E.IsReady())
        //    {
        //        if (E.IsReady() && target.IsValidTarget(E.Range))
        //        {
        //            switch (Args.Type)
        //            {
        //                case SpellType.Melee:
        //                    if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
        //                    {
        //                        var ePred = E.GetPrediction(target);
        //                        E.Cast(ePred.UnitPosition);
        //                    }
        //                    break;
        //                case SpellType.Dash:
        //                case SpellType.SkillShot:
        //                case SpellType.Targeted:
        //                {
        //                    var ePred = E.GetPrediction(target);
        //                    E.Cast(ePred.UnitPosition);
        //                }
        //                    break;
        //            }
        //        }
        //    }
        //}

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs Args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (Args.SData.Name == Q.Name)
            {
                lastQTime = Variables.GameTimeTickCount;
            }

            if (Args.SData.Name == W.Name)
            {
                lastWTime = Variables.GameTimeTickCount;
            }
        }

        private static void CastW(Vector3 position)
        {
            if (
                ObjectManager.Get<GameObject>()
                    .Any(
                        x =>
                            x.IsValid && x.PreviousPosition.Distance(position) <= 120 &&
                            x.Name.Equals("cupcake trap",
                                System.StringComparison.CurrentCultureIgnoreCase)))
            {
                return;
            }

            W.Cast(position);
        }
    }
}