namespace SharpShooter.MyPlugin
{
    #region

    using System;
    using System.Linq;

    using EnsoulSharp;
    using EnsoulSharp.SDK;
    using EnsoulSharp.SDK.MenuUI;
    using EnsoulSharp.SDK.Prediction;
    using EnsoulSharp.SDK.Events;
    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    public class Sivir : MyLogic
    {
        public Sivir()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.25f, 90f, 1350f, false, false, SkillshotType.Line);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRCount", "Use R| Enemies Count >= x", 3, 1, 5);

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q|Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddW();
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddQ();
            MiscOption.AddBool("Q", "AutoQ", "Auto Q| CC");
            MiscOption.AddE();
            MiscOption.AddSubMenu("Block", "Block Spell Settings");
            MyEvadeManager.Attach(MiscMenu["SharpShooter.MiscSettings.Block"] as Menu);
            MiscOption.AddR();
            MiscOption.AddBool("R", "AutoR", "Auto R", false);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddDamageIndicatorToHero(true, false, false, false, true);

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

        private static void Auto()
        {
            if (Me.IsUnderEnemyTurret())
            {
                return;
            }

            if (MiscOption.GetBool("Q", "AutoQ").Enabled && Q.IsReady())
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range) && !x.CanMoveMent()))
                {
                    if (target.IsValidTarget(Q.Range))
                    {
                        Q.Cast(target.PreviousPosition);
                    }
                }
            }

            if (MiscOption.GetBool("R", "AutoR").Enabled && R.IsReady() && Me.CountEnemyHeroesInRange(850) >= 3)
            {
                R.Cast();
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
            var target = MyTargetSelector.GetTarget(1500f);

            if (target.IsValidTarget(1500f))
            {
                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range) && !Me.IsDashing())
                {
                    var qPred = Q.GetPrediction(target);

                    if (qPred.Hitchance >= HitChance.High)
                    {
                        Q.Cast(qPred.UnitPosition);
                    }
                }

                if (ComboOption.UseR && Me.CountEnemyHeroesInRange(850) >= ComboOption.GetSlider("ComboRCount").Value &&
                    (target.Health <= Me.GetAutoAttackDamage(target) * 3 && !Q.IsReady() ||
                     target.Health <= Me.GetAutoAttackDamage(target) * 3 + Me.GetSpellDamage(target, SpellSlot.Q)))
                {
                    R.Cast();
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana() && HarassOption.UseQ && Q.IsReady())
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
                    var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() != JungleType.Unknown).ToList();

                    if (mobs.Any())
                    {
                        var qFarm = Q.GetLineFarmLocation(mobs, 30);
                        if (qFarm.MinionsHit >= 2 || mobs.Any(x => x.GetJungleType() != JungleType.Small) && qFarm.MinionsHit >= 1)
                        {
                            Q.Cast(qFarm.Position);
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
                            if (ComboOption.UseW && W.IsReady())
                            {
                                var target = (AIHeroClient)Args.Target;
                                if (target != null && target.InAutoAttackRange())
                                {
                                    W.Cast();
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.AIMinionClient:
                    {
                        if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                        {
                            var minion = (AIMinionClient) Args.Target;
                            if (minion != null && minion.IsValidTarget())
                            {
                                if (minion.IsMinion())
                                {
                                    if (LaneClearOption.HasEnouguMana() && LaneClearOption.UseW && W.IsReady())
                                    {
                                        var minions =
                                            GameObjects.EnemyMinions.Count(
                                                x =>
                                                    x.IsValidTarget(Me.AttackRange + Me.BoundingRadius + 200) &&
                                                    x.IsMinion());

                                        if (minions >= 3)
                                        {
                                            W.Cast();
                                        }
                                    }
                                }
                                else if (minion.GetJungleType() != JungleType.Unknown)
                                {
                                    if (JungleClearOption.HasEnouguMana() && JungleClearOption.UseW && W.IsReady())
                                    {
                                        if (!minion.InAutoAttackRange() ||
                                            !(minion.Health > Me.GetAutoAttackDamage(minion) * 2) ||
                                            minion.GetJungleType() == JungleType.Small)
                                        {
                                            return;
                                        }

                                        W.Cast();
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
                            if (LaneClearOption.HasEnouguMana(true) && LaneClearOption.UseW && W.IsReady())
                            {
                                if (Me.CountEnemyHeroesInRange(850) == 0)
                                {
                                    W.Cast();
                                }
                            }
                        }
                    }
                    break;
            }
        }
    }
}