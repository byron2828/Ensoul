namespace SharpShooter.MyPlugin
{
    #region

    using System;
    using System.Linq;

    using SharpDX;

    using EnsoulSharp;
    using EnsoulSharp.SDK;
    using EnsoulSharp.SDK.Events;
    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    public class Vayne : MyLogic
    {
        public Vayne()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 300f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 650f) { Delay = 0.25f };
            R = new Spell(SpellSlot.R);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboAQA", "Use Q| After Attack");
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRCount", "Use R| Enemies Count >= x", 2, 1, 5);
            ComboOption.AddSlider("ComboRHp", "Use R| And Player HealthPercent <= x%", 40, 0, 100);

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddBool("HarassQ2Passive", "Use Q| Only target have 2 Stack");
            HarassOption.AddE();
            HarassOption.AddBool("HarassE2Passive", "Use E| Only target have 2 Stack");
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddE();

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddSubMenu("Stealth", "Stealth Settings");
            MiscOption.AddList("Stealth", "HideSelect", "Enabled Mode: ", new[] { "Always Max Stealth Time", "Config", "Off" }, 1);
            MiscOption.AddBool("Stealth", "Hideinsolo", "Enabled Solo Stealth Config");
            MiscOption.AddSlider("Stealth", "Hideinsolomyhp", "When Player HealthPercent <= x%", 30);
            MiscOption.AddSlider("Stealth", "Hideinsolotargethp", "And Enemy HealthPercent => x%", 60);
            MiscOption.AddBool("Stealth", "Hideinmulti", "Enabled Team Fight Stealth Config");
            MiscOption.AddSlider("Stealth", "Hideinmultimyhp", "When Player HealthPercent <= x%", 70);
            MiscOption.AddSlider("Stealth", "HideinmultiallyCount", "And Allies Count <= x", 2, 0, 4);
            MiscOption.AddSlider("Stealth", "HideinmultienemyCount", "And Enemies Count >= x", 3, 2, 5);
            MiscOption.AddBasic();
            MiscOption.AddQ();
            MiscOption.AddBool("Q", "QCheck", "Auto Q| Safe Check");
            MiscOption.AddList("Q", "QTurret", "Auto Q| Disable Dash to Enemy Turret",
                new[] { "Only Dash Q", "Only After Attack Q", "Both", "Off" });
            MiscOption.AddBool("Q", "QMelee", "Auto Q| Anti Melee");
            MiscOption.AddE();
            MiscOption.AddBool("E", "AntiGapcloserE", "Auto E| Anti Gapcloser");
            MiscOption.AddR();
            MiscOption.AddBool("R", "AutoR", "Auto R");
            MiscOption.AddSlider("R", "AutoRCount", "Auto R| Enemies Count >= x", 3, 1, 5);
            MiscOption.AddSlider("R", "AutoRRange", "Auto R| Search Enemies Range", 600, 500, 1200);
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcus", "ForcusAttack", "Forcus Attack 2 Passive Target");

            DrawOption.AddMenu();
            DrawOption.AddE(E);
            DrawOption.AddDamageIndicatorToHero(false, true, true, false, true);

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

            if (R.Level > 0 && R.IsReady())
            {
                RLogic();
            }

            if (Me.IsWindingUp)
            {
                return;
            }

            KillSteal();

            HideSettings(MiscOption.GetList("Stealth", "HideSelect").Index);

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

        private static void HideSettings(int settings)
        {
            if (Me.HasBuff("VayneInquisition") && Me.HasBuff("vaynetumblefade"))
            {
                switch (settings)
                {
                    case 0:
                        Orbwalker.AttackState = false;
                        break;
                    case 1:
                        if (MiscOption.GetBool("Stealth", "Hideinsolo").Enabled && Me.CountEnemyHeroesInRange(900) == 1)
                        {
                            SoloHideMode();
                        }

                        if (MiscOption.GetBool("Stealth", "Hideinmulti").Enabled && Me.CountEnemyHeroesInRange(900) > 1)
                        {
                            MultiHideMode();
                        }
                        break;
                    default:
                        Orbwalker.AttackState = true;
                        break;
                }
            }
            else
            {
                Orbwalker.AttackState = true;
            }
        }

        private static void MultiHideMode()
        {
            if (Me.HealthPercent <= MiscOption.GetSlider("Stealth", "Hideinmultimyhp").Value &&
                Me.CountAllyHeroesInRange(900) <= MiscOption.GetSlider("Stealth", "HideinmultiallyCount").Value &&
                Me.CountEnemyHeroesInRange(900) >= MiscOption.GetSlider("Stealth", "HideinmultienemyCount").Value)
            {
                Orbwalker.AttackState = false;
            }
            else
            {
                Orbwalker.AttackState = true;
            }
        }

        private static void SoloHideMode()
        {
            var target = GameObjects.EnemyHeroes.First(x => x.IsValidTarget(900));

            if (target != null && target.IsValidTarget(900) &&
                Me.HealthPercent <= MiscOption.GetSlider("Stealth", "Hideinsolomyhp").Value &&
                target.HealthPercent >= MiscOption.GetSlider("Stealth", "Hideinsolotargethp").Value)
            {
                Orbwalker.AttackState = false;
            }
            else
            {
                Orbwalker.AttackState = true;
            }
        }

        private static void RLogic()
        {
            if (!R.IsReady() || Me.Mana < R.Mana || R.Level == 0)
            {
                return;
            }

            if (MiscOption.GetBool("R", "AutoR").Enabled && R.IsReady() &&
                GameObjects.EnemyHeroes.Count(x => x.IsValidTarget(MiscOption.GetSlider("R", "AutoRRange").Value)) >=
                MiscOption.GetSlider("R", "AutoRCount").Value)
            {
                R.Cast();
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseE && E.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(E.Range) &&
                            x.Health < GetEDamage(x) + GetWDamage(x)))
                {
                    if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                    {
                        E.CastOnUnit(target);
                        return;
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.UseR && R.IsReady() &&
                GameObjects.EnemyHeroes.Count(x => x.IsValidTarget(650)) >= ComboOption.GetSlider("ComboRCount").Value &&
                Me.HealthPercent <= ComboOption.GetSlider("ComboRHp").Value)
            {
                R.Cast();
            }

            if (ComboOption.UseE && E.IsReady())
            {
                ELogic();
            }

            if (ComboOption.UseQ && Q.IsReady())
            {
                if (Me.HasBuff("VayneInquisition") && Me.CountEnemyHeroesInRange(1200) > 0 &&
                    Me.CountEnemyHeroesInRange(700) >= 2)
                {
                    var dashPos = GetDashQPos();

                    if (dashPos != Vector3.Zero)
                    {
                        if (Me.CanMoveMent())
                        {
                            Q.Cast(dashPos);
                        }
                    }
                }

                if (Me.CountEnemyHeroesInRange(Me.AttackRange) == 0 && Me.CountEnemyHeroesInRange(900) > 0)
                {
                    var target = MyTargetSelector.GetTarget(900);

                    if (target.IsValidTarget())
                    {
                        if (!target.InAutoAttackRange() &&
                            target.Position.DistanceToCursor() < target.Position.DistanceToPlayer())
                        {
                            var dashPos = GetDashQPos();

                            if (dashPos != Vector3.Zero)
                            {
                                if (Me.CanMoveMent())
                                {
                                    Q.Cast(dashPos);
                                }
                            }
                        }

                        if (ComboOption.UseE && E.IsReady())
                        {
                            var dashPos = GetDashQPos();

                            if (dashPos != Vector3.Zero && CondemnCheck(dashPos, target))
                            {
                                if (Me.CanMoveMent())
                                {
                                    Q.Cast(dashPos);
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
                if (HarassOption.UseE && E.IsReady())
                {
                    var target = HarassOption.GetTarget(E.Range);

                    if (target.IsValidTarget(E.Range))
                    {
                        if (HarassOption.GetBool("HarassE2Passive").Enabled)
                        {
                            if (target.IsValidTarget(E.Range) && Has2WStacks(target))
                            {
                                E.CastOnUnit(target);
                            }
                        }
                        else
                        {
                            if (CondemnCheck(Me.PreviousPosition, target))
                            {
                                E.CastOnUnit(target);
                            }
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
                      GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Me.AttackRange + Me.BoundingRadius) && x.IsMinion())
                            .Where(m => m.Health <= Me.GetAutoAttackDamage(m) + Me.GetSpellDamage(m, SpellSlot.Q))
                            .ToList();

                    if (minions.Any() && minions.Count > 1)
                    {
                        var minion = minions.OrderBy(m => m.Health).FirstOrDefault();
                        var afterQPosition = Me.PreviousPosition.Extend(Game.CursorPos, Q.Range);

                        if (minion != null && afterQPosition.Distance(minion.PreviousPosition) <= Me.AttackRange + Me.BoundingRadius)
                        {
                            Q.Cast(Game.CursorPos);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana() && JungleClearOption.UseE && E.IsReady())
            {
                var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range) && x.GetJungleType() != JungleType.Unknown).ToList();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault(
                        x =>
                            !x.Name.ToLower().Contains("mini") && !x.Name.ToLower().Contains("baron") &&
                            !x.Name.ToLower().Contains("dragon") && !x.Name.ToLower().Contains("crab") &&
                            !x.Name.ToLower().Contains("herald"));

                    if (mob != null && mob.IsValidTarget(E.Range))
                    {
                        if (CondemnCheck(Me.PreviousPosition, mob))
                        {
                            E.CastOnUnit(mob);
                        }
                    }
                }
            }
        }

        private static void OnAction(object sender, OrbwalkerActionArgs Args)
        {
            if (Args.Type == OrbwalkerType.BeforeAttack)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass)
                {
                    var ForcusTarget =
                        GameObjects.EnemyHeroes.FirstOrDefault(
                            x => x.IsValidTarget(Me.AttackRange + Me.BoundingRadius + x.BoundingRadius + 50) && Has2WStacks(x));

                    if (MiscOption.GetBool("Forcus", "ForcusAttack").Enabled && ForcusTarget != null &&
                        ForcusTarget.IsValidTarget(Me.AttackRange + Me.BoundingRadius - ForcusTarget.BoundingRadius + 15))
                    {
                        Orbwalker.ForceTarget = ForcusTarget;
                        LastForcusTime = Variables.GameTimeTickCount;
                    }
                    else
                    {
                        Orbwalker.ForceTarget = null;
                    }
                }
            }

            if (Args.Type == OrbwalkerType.AfterAttack)
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
                                if (ComboOption.GetBool("ComboAQA").Enabled)
                                {
                                    var target = (AIHeroClient)Args.Target;
                                    if (target != null && !target.IsDead && Q.IsReady())
                                    {
                                        AfterQLogic(target);
                                    }
                                }
                            }
                            else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass || Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && MyManaManager.SpellHarass)
                            {
                                if (HarassOption.HasEnouguMana() && HarassOption.UseQ)
                                {
                                    var target = (AIHeroClient)Args.Target;
                                    if (target != null && !target.IsDead && Q.IsReady() &&
                                        HarassOption.GetHarassTargetEnabled(target.CharacterName))
                                    {
                                        if (HarassOption.GetBool("HarassQ2Passive").Enabled && !Has2WStacks(target))
                                        {
                                            return;
                                        }

                                        AfterQLogic(target);
                                    }
                                }
                            }
                        }
                        break;
                    case GameObjectType.AIMinionClient:
                        {
                            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                            {
                                var m = (AIMinionClient) Args.Target;
                                if (m != null && m.IsValidTarget())
                                {
                                    if (m.IsMinion())
                                    {
                                        if (LaneClearOption.HasEnouguMana() && LaneClearOption.UseQ)
                                        {
                                            var minions =
                                                GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Me.AttackRange + Me.BoundingRadius) && x.IsMinion())
                                                    .Where(x => x.Health <= Me.GetAutoAttackDamage(x) + Me.GetSpellDamage(x, SpellSlot.Q))
                                                    .ToList();

                                            if (minions.Any() && minions.Count >= 1)
                                            {
                                                var minion = minions.OrderBy(x => x.Health).FirstOrDefault();
                                                var afterQPosition = Me.PreviousPosition.Extend(Game.CursorPos, Q.Range);

                                                if (minion != null &&
                                                    afterQPosition.Distance(minion.PreviousPosition) <= Me.AttackRange + Me.BoundingRadius)
                                                {
                                                    Q.Cast(Game.CursorPos);
                                                }
                                            }
                                        }
                                    }
                                    else if (m.GetJungleType() != JungleType.Unknown)
                                    {
                                        if (JungleClearOption.HasEnouguMana() && JungleClearOption.UseQ)
                                        {
                                            Q.Cast(Game.CursorPos);
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
                                        if (Me.CanMoveMent())
                                        {
                                            Q.Cast(Game.CursorPos);
                                        }
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
        //    if (target != null && target.IsValidTarget(E.Range))
        //    {
        //        switch (Args.Type)
        //        {
        //            case SpellType.Melee:
        //                {
        //                    if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100) &&
        //                        MiscOption.GetBool("Q", "QMelee").Enabled && Q.IsReady())
        //                    {
        //                        Q.Cast(Me.PreviousPosition.Extend(target.PreviousPosition, -Q.Range));
        //                    }
        //                }
        //                break;
        //            case SpellType.SkillShot:
        //                {
        //                    if (MiscOption.GetBool("E", "AntiGapcloserE").Enabled && E.IsReady() && target.IsValidTarget(250) && !Args.HaveShield)
        //                    {
        //                        E.CastOnUnit(target);
        //                    }
        //                }
        //                break;
        //            case SpellType.Dash:
        //            case SpellType.Targeted:
        //                {
        //                    if (MiscOption.GetBool("E", "AntiGapcloserE").Enabled && E.IsReady() && target.IsValidTarget(E.Range) && !Args.HaveShield)
        //                    {
        //                        E.CastOnUnit(target);
        //                    }
        //                }
        //                break;
        //        }
        //    }
        //}

        private static Vector3 GetDashQPos()
        {
            var firstQPos = Me.PreviousPosition.Extend(Game.CursorPos, Q.Range);
            var allPoint = MyExtraManager.GetCirclePoints(Q.Range).ToList();

            foreach (var point in allPoint)
            {
                var mousecount = firstQPos.CountEnemyHeroesInRange(300);
                var count = point.CountEnemyHeroesInRange(300);

                if (!HaveEnemiesInRange(point))
                {
                    continue;
                }

                if (mousecount == count)
                {
                    if (point.DistanceToCursor() < firstQPos.DistanceToCursor())
                    {
                        firstQPos = point;
                    }
                }

                if (count < mousecount)
                {
                    firstQPos = point;
                }
            }

            if (MiscOption.GetList("Q", "QTurret").Index == 0 || MiscOption.GetList("Q", "QTurret").Index == 2)
            {
                if (firstQPos.IsUnderEnemyTurret())
                {
                    return Vector3.Zero;
                }
            }

            if (MiscOption.GetBool("Q", "QCheck").Enabled)
            {
                if (Me.CountEnemyHeroesInRange(Q.Range + Me.BoundingRadius - 30) <
                    firstQPos.CountEnemyHeroesInRange(Q.Range * 2 - Me.BoundingRadius))
                {
                    return Vector3.Zero;
                }

                if (firstQPos.CountEnemyHeroesInRange(Q.Range * 2 - Me.BoundingRadius) > 3)
                {
                    return Vector3.Zero;
                }
            }

            return HaveEnemiesInRange(firstQPos) ? firstQPos : Vector3.Zero;
        }

        private static void AfterQLogic(AIBaseClient target)
        {
            if (!Q.IsReady() || target == null || !target.IsValidTarget())
            {
                return;
            }

            var qPosition = Me.Position.Extend(Game.CursorPos, Q.Range);
            var targetDisQ = target.Position.Distance(qPosition);

            if (MiscOption.GetList("Q", "QTurret").Index == 1 || MiscOption.GetList("Q", "QTurret").Index == 2)
            {
                if (qPosition.IsUnderEnemyTurret())
                {
                    return;
                }
            }

            if (MiscOption.GetBool("Q", "QCheck").Enabled)
            {
                if (GameObjects.EnemyHeroes.Count(x => x.IsValidTarget(300f, true, qPosition)) >= 3)
                {
                    return;
                }

                //Catilyn W
                if (ObjectManager
                        .Get<GameObject>()
                        .FirstOrDefault(
                            x =>
                                x != null && x.IsValid &&
                                x.Name.ToLower().Contains("yordletrap_idle_red.troy") &&
                                x.Position.Distance(qPosition) <= 100) != null)
                {
                    return;
                }

                //Jinx E
                if (ObjectManager.Get<AIMinionClient>()
                        .FirstOrDefault(x => x.IsValid && x.IsEnemy && x.Name == "k" &&
                                             x.Position.Distance(qPosition) <= 100) != null)
                {
                    return;
                }

                //Teemo R
                if (ObjectManager.Get<AIMinionClient>()
                        .FirstOrDefault(x => x.IsValid && x.IsEnemy && x.Name == "Noxious Trap" &&
                                             x.Position.Distance(qPosition) <= 100) != null)
                {
                    return;
                }
            }

            if (targetDisQ <= Me.AttackRange + Me.BoundingRadius)
            {
                if (Me.CanMoveMent())
                {
                    Q.Cast(Game.CursorPos);
                }
            }
        }

        private static void ELogic()
        {
            if (!E.IsReady())
            {
                return;
            }

            foreach (var target in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget(E.Range)))
            {
                if (target.IsValidTarget(E.Range) && !target.HaveShiledBuff())
                {
                    if (CondemnCheck(Me.PreviousPosition, target))
                    {
                        E.CastOnUnit(target);
                        return;
                    }
                }
            }
        }

        private static bool CondemnCheck(Vector3 startPosition, AIBaseClient target)
        {
            var targetPosition = target.PreviousPosition;
            var predPosition = E.GetPrediction(target).UnitPosition;
            var pushDistance = startPosition == Me.PreviousPosition ? 420 : 410;

            for (var i = 0; i <= pushDistance; i += 20)
            {
                var targetPoint = targetPosition.Extend(startPosition, -i);
                var predPoint = predPosition.Extend(startPosition, -i);

                if (predPoint.IsWall() && targetPoint.IsWall())
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HaveEnemiesInRange(Vector3 position)
        {
            return position.CountEnemyHeroesInRange(Me.AttackRange + Me.BoundingRadius) > 0;
        }

        private static bool Has2WStacks(AIBaseClient target)
        {
            return W.Level > 0 && target.Buffs.Any(x => x.Name.ToLower() == "vaynesilvereddebuff" && x.Count == 2);
        }

        public static double GetWDamage(AIBaseClient target)
        {
            if (target == null || target.IsDead || !target.IsValidTarget() || 
                !target.Buffs.Any(x => x.Name.ToLower() == "vaynesilvereddebuff" && x.Count == 2))
            {
                return 0;
            }

            var DMG = target.Type == GameObjectType.AIMinionClient
                ? Math.Min(200, new[] { 6, 7.5, 9, 10.5, 12 }[Me.Spellbook.GetSpell(SpellSlot.W).Level - 1] / 100 * target.MaxHealth)
                : new[] { 6, 7.5, 9, 10.5, 12 }[Me.Spellbook.GetSpell(SpellSlot.W).Level - 1] / 100 * target.MaxHealth;

            return Me.CalculateDamage(target, DamageType.True, DMG);
        }

        private static double GetEDamage(AIBaseClient target)
        {
            if (target == null || target.IsDead || !target.IsValidTarget())
            {
                return 0;
            }

            var DMG = new double[] { 45, 80, 115, 150, 185 }[Me.Spellbook.GetSpell(SpellSlot.E).Level - 1] + 0.5 * Me.FlatPhysicalDamageMod;

            return Me.CalculateDamage(target, DamageType.Magical, DMG);
        }
    }
}