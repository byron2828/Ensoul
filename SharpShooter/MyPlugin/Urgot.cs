namespace SharpShooter.MyPlugin
{
    #region

    using System;
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

    public class Urgot : MyLogic
    {
        private static int lastQTime, lastWTime, lastETime, lastPressTime;

        private static bool isWActive => W.Name.ToLower() == "urgotwcancel";
        private static bool isRActive => R.Name.ToLower() == "urgotrrecast";

        private static bool HaveEBuff(AIHeroClient target)
        {
            return target.Buffs.Any(x => x.IsActive && x.Name.ToLower().Contains("urgotr") && x.Name.ToLower().Contains("stun"));
        }

        private static bool HaveRBuff(AIHeroClient target)
        {
            return target.Buffs.Any(x => x.IsActive && x.Name.ToLower() == "urgotr");
        }

        private static bool CanbeRKillAble(AIHeroClient target)
        {
            return target != null && target.IsValidTarget() && isRActive && HaveRBuff(target) && target.HealthPercent < 25 && R.IsReady();
        }

        public Urgot()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 800f);
            Q.SetSkillshot(0.41f, 180f, float.MaxValue, false, true, SkillshotType.Circle);

            W = new Spell(SpellSlot.W, 550f);

            E = new Spell(SpellSlot.E, 550f);
            E.SetSkillshot(0.40f, 65f, 580f, false, true, SkillshotType.Line);

            R = new Spell(SpellSlot.R, 1600f);
            R.SetSkillshot(0.20f, 80f, 2150f, false, false, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboQAfterE", "Use Q| Only After E or E is CoolDown");
            ComboOption.AddW();
            ComboOption.AddBool("ComboWCancel", "Use W| Auto Cancel");
            ComboOption.AddE();
            ComboOption.AddBool("ComboRSolo", "Use R| Solo Mode");

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddSliderBool("LaneClearQCount", "Use Q| Min Hit Count >= x", 3, 1, 5, true);
            LaneClearOption.AddSliderBool("LaneClearWCount", "Use W| Min Hit Count >= x", 4, 1, 10, true);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddR();
            KillStealOption.AddSlider("KillStealRDistance", "Use R| When target Distance Player >= x", 600, 0, 1600);
            KillStealOption.AddTargetList();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            //MiscOption.AddW(); TODO
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key(only work for select target)", Keys.T, KeyBindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(true, true, true, true, true);

            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            Tick.OnTick += OnUpdate;
            Orbwalker.OnAction += OnAction;
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs Args)
        {
            if (sender.IsMe)
            {
                switch (Args.Slot)
                {
                    case SpellSlot.Q:
                        lastQTime = Variables.GameTimeTickCount;
                        break;
                    case SpellSlot.W:
                        lastWTime = Variables.GameTimeTickCount;
                        break;
                    case SpellSlot.E:
                        lastETime = Variables.GameTimeTickCount;
                        break;
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (isWActive)
            {
                Orbwalker.AttackState = false;
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                Orbwalker.AttackState = true;
            }

            if (MiscOption.GetKey("R", "SemiR").Active && R.IsReady())
            {
                SemiRLogic();
            }

            if (Me.IsWindingUp)
            {
                return;
            }

            AutoR2Logic();
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
                    Clear();
                    break;
            }
        }

        private static void SemiRLogic()
        {
            var target = TargetSelector.SelectedTarget;

            if (target != null && target.IsValidTarget())
            {
                if (!isRActive)
                {
                    if (target.IsValidTarget(R.Range))
                    {
                        var rPos = GetRCastPosition(target);

                        if (rPos != Vector3.Zero)
                        {
                            R.Cast(rPos);
                            lastPressTime = Variables.GameTimeTickCount;
                        }
                    }
                }

                if (isRActive && HaveRBuff(target))
                {
                    R.Cast();
                }
            }
        }

        private static void AutoR2Logic()
        {
            if (Variables.GameTimeTickCount - lastPressTime <= 2000)
            {
                if (!isRActive)
                {
                    return;
                }

                if (GameObjects.EnemyHeroes.Where(x => x.IsValidTarget() && HaveRBuff(x) && !x.HaveShiledBuff())
                        .Any(target => target != null && target.IsValidTarget()))
                {
                    R.Cast();
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
                        x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q) && !HaveRBuff(x)))
                {
                    if (target.IsValidTarget(Q.Range) && !HaveRBuff(target) && !target.IsUnKillable())
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                            return;
                        }
                    }
                }
            }

            if (KillStealOption.UseR && R.IsReady() && Variables.GameTimeTickCount - lastQTime > 3000)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(R.Range + 500) &&
                            !x.IsValidTarget(KillStealOption.GetSlider("KillStealRDistance").Value) &&
                            x.Health < GetRDamage(x, true) && KillStealOption.GetKillStealTarget(x.CharacterName)))
                {
                    if (target.IsValidTarget())
                    {
                        if (CanbeRKillAble(target))
                        {
                            R.Cast();
                            return;
                        }

                        if (!isRActive && target.IsValidTarget(R.Range))
                        {
                            var rPos = GetRCastPosition(target);

                            if (rPos != Vector3.Zero)
                            {
                                R.Cast(rPos);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.GetBool("ComboWCancel").Enabled && W.IsReady() && isWActive)
            {
                CancelW();
            }

            var target = MyTargetSelector.GetTarget(Q.Range);

            if (target != null && target.IsValidTarget(Q.Range))
            {
                if (ComboOption.UseE && E.IsReady())
                {
                    if (target.IsValidTarget(E.Range))
                    {
                        if (!target.IsValidTarget(350) || !Q.IsReady())
                        {
                            var ePred = E.GetPrediction(target);

                            if (ePred.Hitchance >= HitChance.High)
                            {
                                E.Cast(ePred.CastPosition);
                            }
                        }
                    }
                }

                if (ComboOption.UseQ && Q.IsReady() && Variables.GameTimeTickCount - lastETime > 800 + Game.Ping)
                {
                    if (target.IsValidTarget(Q.Range))
                    {
                        if (ComboOption.GetBool("ComboQAfterE").Enabled)
                        {
                            if (E.IsReady())
                            {
                                return;
                            }

                            var qPred = Q.GetPrediction(target);

                            if (qPred.Hitchance >= HitChance.High)
                            {
                                Q.Cast(qPred.CastPosition);
                            }
                        }
                        else
                        {
                            if (target.IsValidTarget(E.Range) && E.IsReady())
                            {
                                return;
                            }

                            var qPred = Q.GetPrediction(target);

                            if (qPred.Hitchance >= HitChance.High)
                            {
                                Q.Cast(qPred.CastPosition);
                            }
                        }
                    }
                }

                if (ComboOption.UseW && W.IsReady() && !isWActive)
                {
                    if (GameObjects.EnemyHeroes.Any(x => x.IsValidTarget(W.Range)))
                    {
                        if (target.IsValidTarget(E.Range) && HaveEBuff(target))
                        {
                            W.Cast();
                        }
                        else if (!E.IsReady() && target.IsValidTarget(350) && Variables.GameTimeTickCount - lastETime < 1500 + Game.Ping)
                        {
                            W.Cast();
                        }
                        else if (!Q.IsReady() && target.IsValidTarget(450) && Variables.GameTimeTickCount - lastQTime < 1300 + Game.Ping)
                        {
                            W.Cast();
                        }
                        else if (!Q.IsReady() && !E.IsReady() && target.IsValidTarget(W.Range - 65) && Variables.GameTimeTickCount - lastQTime > 1300 &&
                                 Variables.GameTimeTickCount - lastETime > 1300)
                        {
                            var minions =
                                GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range - 65) && (x.IsMinion() || x.GetJungleType() != JungleType.Unknown))
                                    .ToList();

                            if (!minions.Any() || minions.Count <= 3 && target.IsValidTarget(410))
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (ComboOption.GetBool("ComboRSolo").Enabled && R.IsReady() && !target.IsUnKillable() && Me.CountEnemyHeroesInRange(1200) == 1)
                {
                    if (target.Health > GetRDamage(target, true) &&
                        target.Health <
                        GetRDamage(target, true) + GetWDamage(target, 2) + GetPassiveDamage(target) +
                        Me.GetAutoAttackDamage(target) + (Q.IsReady() ? Me.GetSpellDamage(target, SpellSlot.Q) : 0) +
                        (E.IsReady() ? Me.GetSpellDamage(target, SpellSlot.E) : 0))
                    {
                        if (isRActive)
                        {
                            if (target.IsValidTarget())
                            {
                                R.Cast();
                            }
                        }
                        else
                        {
                            if (target.IsValidTarget(R.Range))
                            {
                                var rPos = GetRCastPosition(target);

                                if (rPos != Vector3.Zero)
                                {
                                    R.Cast(rPos);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                var target = HarassOption.GetTarget(Q.Range);

                if (target != null && target.IsValidTarget(Q.Range))
                {
                    if (HarassOption.UseQ && Q.IsReady())
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

        private static void Clear()
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
                if (LaneClearOption.GetSliderBool("LaneClearQCount").Enabled && Q.IsReady())
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToList();

                    if (minions.Any())
                    {
                        var qFarm = Q.GetCircularFarmLocation(minions);

                        if (qFarm.MinionsHit >= LaneClearOption.GetSliderBool("LaneClearQCount").Value)
                        {
                            Q.Cast(qFarm.Position);
                        }
                    }
                }

                if (LaneClearOption.GetSliderBool("LaneClearWCount").Enabled && W.IsReady() && !isWActive)
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion()).ToList();

                    if (minions.Any())
                    {
                        if (minions.Count >= LaneClearOption.GetSliderBool("LaneClearWCount").Value)
                        {
                            W.Cast();
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(R.Range) && x.GetJungleType() != JungleType.Unknown).ToList();

                if (mobs.Any())
                {
                    var bigmob = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

                    if (JungleClearOption.UseQ && Q.IsReady())
                    {
                        if (bigmob != null && bigmob.IsValidTarget(Q.Range) && (!bigmob.InAutoAttackRange() || !Orbwalker.CanAttack()))
                        {
                            Q.Cast(bigmob.PreviousPosition);
                        }
                        else
                        {
                            var qMobs = mobs.Where(x => x.IsValidTarget(Q.Range)).ToList();
                            var qFarm = Q.GetCircularFarmLocation(qMobs);

                            if (qFarm.MinionsHit >= 2)
                            {
                                Q.Cast(qFarm.Position);
                            }
                        }
                    }

                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        if (bigmob != null && bigmob.IsValidTarget(E.Range) && (!bigmob.InAutoAttackRange() || !Orbwalker.CanAttack()))
                        {
                            E.Cast(bigmob.PreviousPosition);
                        }
                    }

                    if (JungleClearOption.UseW && W.IsReady() && !isWActive)
                    {
                        if (bigmob != null && bigmob.IsValidTarget(W.Range) && (!bigmob.InAutoAttackRange() || !Orbwalker.CanAttack()))
                        {
                            W.Cast();
                        }
                        else
                        {
                            var wMobs = mobs.Where(x => x.IsValidTarget(W.Range)).ToList();

                            if (wMobs.Count >= 2)
                            {
                                W.Cast();
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

            if (Args.Target == null || Args.Target.IsDead || Args.Target.Health <= 0 || Me.IsDead || !Q.IsReady())
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
                                if (ComboOption.UseW && W.IsReady() && !isWActive)
                                {
                                    if (GameObjects.EnemyHeroes.Any(x => x.IsValidTarget(W.Range)))
                                    {
                                        if (target.IsValidTarget(E.Range) && HaveEBuff(target))
                                        {
                                            W.Cast();
                                        }
                                        else if (!E.IsReady() && target.IsValidTarget(350) && Variables.GameTimeTickCount - lastETime < 1500 + Game.Ping)
                                        {
                                            W.Cast();
                                        }
                                    }
                                }
                                else if (ComboOption.UseQ && Q.IsReady())
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.Hitchance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.UnitPosition);
                                    }
                                }
                            }
                            else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass ||
                                     Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && MyManaManager.SpellHarass)
                            {
                                if (HarassOption.HasEnouguMana() && HarassOption.GetHarassTargetEnabled(target.CharacterName))
                                {
                                    if (HarassOption.UseQ && Q.IsReady())
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
                    }
                    break;
                case GameObjectType.AIMinionClient:
                    {
                        var mob = (AIMinionClient)Args.Target;
                        if (mob != null && mob.IsValidTarget() && mob.GetJungleType() != JungleType.Unknown && mob.GetJungleType() != JungleType.Small)
                        {
                            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && MyManaManager.SpellFarm && JungleClearOption.HasEnouguMana())
                            {
                                if (JungleClearOption.UseQ && Q.IsReady() && mob.IsValidTarget(Q.Range))
                                {
                                    Q.Cast(mob.PreviousPosition);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void CancelW(bool ignoreCheck = false)
        {
            if (isWActive)
            {
                if (ignoreCheck || GameObjects.EnemyHeroes.All(x => !x.IsValidTarget(W.Range)))
                {
                    W.Cast();
                }
            }
        }

        private static Vector3 GetRCastPosition(AIHeroClient target)
        {
            if (target == null || !target.IsValidTarget(R.Range) || target.IsUnKillable())
            {
                return Vector3.Zero;
            }

            var rPredInput = new SpellPrediction.PredictionInput
            {
                Unit = target,
                Radius = R.Width,
                Speed = R.Speed,
                Range = R.Range,
                Delay = R.Delay,
                Aoe = false,
                AddHitBox = true,
                From = Me.PreviousPosition,
                RangeCheckFrom = Me.PreviousPosition,
                Type = SkillshotType.Line,
                CollisionObjects = CollisionObjects.Heroes | CollisionObjects.YasuoWall
            };

            var rPredOutput = SpellPrediction.GetPrediction(rPredInput);

            if (rPredOutput.Hitchance < HitChance.High/* ||
                SpellPrediction.GetCollision(new List<Vector3> { target.PreviousPosition }, rPredInput)
                    .Any(x => x.NetworkId != target.NetworkId)*/)
            {
                return Vector3.Zero;
            }

            return rPredOutput.CastPosition;
        }

        public static double GetWDamage(AIBaseClient target, int time = 1)
        {
            if (target == null || !target.IsValidTarget())
            {
                return 0d;
            }

            var wDMG = 12 + new[] { 0.20, 0.24, 0.28, 0.32, 0.36 }[W.Level - 1] * Me.FlatPhysicalDamageMod * 3 * time;

            return Me.CalculateDamage(target, DamageType.Physical, wDMG);
        }

        public static double GetRDamage(AIHeroClient target, bool calculate25 = false)
        {
            if (target == null || !target.IsValidTarget())
            {
                return 0d;
            }

            var rDMG = new double[] { 50, 175, 300 }[R.Level - 1] + 0.5 * Me.FlatPhysicalDamageMod;

            return Me.CalculateDamage(target, DamageType.Physical, rDMG) + (calculate25 ? target.MaxHealth * 0.249 : 0);
        }

        private static double GetBasicDamage
        {
            get
            {
                if (Me.Level >= 16)
                {
                    return 1.00 * Me.FlatPhysicalDamageMod;
                }

                if (Me.Level >= 13)
                {
                    return 0.90 * Me.FlatPhysicalDamageMod;
                }

                if (Me.Level >= 11)
                {
                    return 0.80 * Me.FlatPhysicalDamageMod;
                }

                if (Me.Level >= 9)
                {
                    return 0.65 * Me.FlatPhysicalDamageMod;
                }

                if (Me.Level >= 6)
                {
                    return 0.50 * Me.FlatPhysicalDamageMod;
                }

                return 0.40 * Me.FlatPhysicalDamageMod;
            }
        }

        private static double GetBasicValueForHP
        {
            get
            {
                if (Me.Level >= 15)
                {
                    return 0.08;
                }

                if (Me.Level >= 13)
                {
                    return 0.07;
                }

                if (Me.Level >= 6)
                {
                    return 0.525;
                }

                return 0.045;
            }
        }

        private static double GetPassiveDamage(AIHeroClient target)
        {
            if (target == null || !target.IsValidTarget())
            {
                return 0d;
            }

            var basicDamage = GetBasicDamage + GetBasicValueForHP * target.MaxHealth;

            return Me.CalculateDamage(target, DamageType.Physical, basicDamage);
        }
    }
}
