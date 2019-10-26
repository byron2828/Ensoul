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
    using EnsoulSharp.SDK.Utility;
    using EnsoulSharp.SDK.Events;
    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using Color = System.Drawing.Color;
    using Keys = System.Windows.Forms.Keys;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    public class Draven : MyLogic
    {
        private static Dictionary<GameObject, int> AxeList { get; } = new Dictionary<GameObject, int>();

        private static Vector3 OrbwalkerPoint { get; set; } = Game.CursorPos;

        private static int AxeCount => (Me.HasBuff("dravenspinning") ? 1 : 0) + (Me.HasBuff("dravenspinningleft") ? 1 : 0) + AxeList.Count;

        private static int lastCatchTime { get; set; }

        public Draven()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E, 950f);
            E.SetSkillshot(0.25f, 100f, 1400f, false, false, SkillshotType.Line);

            R = new Spell(SpellSlot.R, 3000f);
            R.SetSkillshot(0.4f, 160f, 2000f, false, false, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("RSolo", "Use R | Solo Ks Mode");
            ComboOption.AddBool("RTeam", "Use R| Team Fight");

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSliderBool("LaneClearECount", "Use E| Min Hit Count >= x", 4, 1, 7, true);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddE();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            AxeOption.AddMenu();
            AxeOption.AddList("CatchMode", "Catch Axe Mode: ", new[] { "All", "Only Combo", "Off" });
            AxeOption.AddSlider("CatchRange", "Catch Axe Range(Cursor center)", 2000, 180, 3000);
            AxeOption.AddSlider("CatchCount", "Max Axe Count <= x", 2, 1, 3);
            AxeOption.AddBool("CatchWSpeed", "Use W| When Axe Too Far");
            AxeOption.AddBool("NotCatchKS", "Dont Catch| If Target Can KillAble(1-3 AA)");
            AxeOption.AddBool("NotCatchTurret", "Dont Catch| If Axe Under Enemy Turret");
            AxeOption.AddSliderBool("NotCatchMoreEnemy", "Dont Catch| If Enemy Count >= x", 3, 1, 5, true);
            AxeOption.AddBool("CancelCatch", "Enabled Cancel Catch Axe Key");
            AxeOption.AddKey("CancelKey1", "Cancel Catch Key 1", Keys.G, KeyBindType.Press);
            AxeOption.AddBool("CancelKey2", "Cancel Catch Key 2(is right click)");
            AxeOption.AddBool("CancelKey3", "Cancel Catch Key 3(is mouse scroll)", false);
            AxeOption.AddSeperator("Set Orbwalker->Misc->Hold Radius to 0 (will better)");

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddW();
            MiscOption.AddBool("W", "WSlow", "Auto W| When Player Have Debuff(Slow)");
            MiscOption.AddR();
            MiscOption.AddSlider("R", "GlobalRMin", "Global -> Cast R Min Range", 1000, 500, 2500);
            MiscOption.AddSlider("R", "GlobalRMax", "Global -> Cast R Max Range", 3000, 1500, 3500);
            MiscOption.AddKey("R", "SemiRKey", "Semi-manual R Key", Keys.T, KeyBindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddBool("AxeRange", "Draw Catch Axe Range");
            DrawOption.AddBool("AxePosition", "Draw Axe Position");
            DrawOption.AddDamageIndicatorToHero(true, false, true, true, true);

            AxeOption.GetKey("CancelKey1").ValueChanged += OnCancelValueChange;

            Tick.OnTick += OnUpdate;
            Game.OnWndProc += OnWndProc;
            GameObject.OnCreate += (sender, args) => OnCreate(sender);
            GameObject.OnDelete += (sender, args) => OnDestroy(sender);
            //Gapcloser.OnGapcloser += OnGapcloser;
            Orbwalker.OnAction += OnAction;
            Drawing.OnDraw += OnRender;
        }

        private static void OnCancelValueChange(object sender, EventArgs e)
        {
            var key = sender as MenuKeyBind;
            if (key != null && key.Active)
            {
                if (AxeOption.GetBool("CancelCatch").Enabled)
                {
                    if (Variables.GameTimeTickCount - lastCatchTime > 1800)
                    {
                        lastCatchTime = Variables.GameTimeTickCount;
                    }
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            foreach (var sender in AxeList.Where(x => x.Key.IsDead || !x.Key.IsValid).Select(x => x.Key))
            {
                AxeList.Remove(sender);
            }

            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Me.IsWindingUp)
            {
                return;
            }

            R.Range = MiscOption.GetSlider("R", "GlobalRMax").Value;

            CatchAxeEvent();
            KillStealEvent();
            AutoUseEvent();

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    ComboEvent();
                    break;
                case OrbwalkerMode.Harass:
                    HarassEvent();
                    break;
                case OrbwalkerMode.LaneClear:
                    ClearEvent();
                    break;
            }
        }

        private static void CatchAxeEvent()
        {
            if (AxeList.Count == 0)
            {
                Orbwalker.SetOrbwalkerPosition(Vector3.Zero);
                return;
            }

            if (AxeOption.GetList("CatchMode").Index == 2 ||
                AxeOption.GetList("CatchMode").Index == 1 && Orbwalker.ActiveMode != OrbwalkerMode.Combo)
            {
                Orbwalker.SetOrbwalkerPosition(Vector3.Zero);
                return;
            }

            var catchRange = AxeOption.GetSlider("CatchRange").Value;

            var bestAxe =
                AxeList.Where(x => !x.Key.IsDead && x.Key.IsValid && x.Key.Position.DistanceToCursor() <= catchRange)
                    .OrderBy(x => x.Value)
                    .ThenBy(x => x.Key.Position.DistanceToPlayer())
                    .ThenBy(x => x.Key.Position.DistanceToCursor())
                    .FirstOrDefault();

            if (bestAxe.Key != null)
            {
                if (AxeOption.GetBool("NotCatchTurret").Enabled &&
                    (Me.IsUnderEnemyTurret() && bestAxe.Key.Position.IsUnderEnemyTurret() ||
                     bestAxe.Key.Position.IsUnderEnemyTurret() && !Me.IsUnderEnemyTurret()))
                {
                    return;
                }

                if (AxeOption.GetSliderBool("NotCatchMoreEnemy").Enabled &&
                    (bestAxe.Key.Position.CountEnemyHeroesInRange(350) >=
                     AxeOption.GetSliderBool("NotCatchMoreEnemy").Value ||
                     GameObjects.EnemyHeroes.Count(x => x.Distance(bestAxe.Key.Position) < 350 && x.IsMelee) >=
                     AxeOption.GetSliderBool("NotCatchMoreEnemy").Value - 1))
                {
                    return;
                }

                if (AxeOption.GetBool("NotCatchKS").Enabled && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    var target = MyTargetSelector.GetTarget(800);

                    if (target != null && target.IsValidTarget(800) &&
                        target.DistanceToPlayer() > target.BoundingRadius + Me.BoundingRadius + 200 &&
                        target.Health < Me.GetAutoAttackDamage(target) * 2.5 - 80)
                    {
                        Orbwalker.SetOrbwalkerPosition(Vector3.Zero);
                        return;
                    }
                }

                if (AxeOption.GetBool("CatchWSpeed").Enabled && W.IsReady() &&
                    bestAxe.Key.Position.DistanceToPlayer() / Me.MoveSpeed * 1000 >= bestAxe.Value - Variables.GameTimeTickCount)
                {
                    W.Cast();
                }

                if (bestAxe.Key.Position.DistanceToPlayer() > 100)
                {
                    if (Variables.GameTimeTickCount - lastCatchTime > 1800)
                    {
                        if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                        {
                            Orbwalker.SetOrbwalkerPosition(bestAxe.Key.Position);
                        }
                        else
                        {
                            Me.IssueOrder(GameObjectOrder.MoveTo, bestAxe.Key.Position);
                        }
                    }
                    else
                    {
                        if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                        {
                            Orbwalker.SetOrbwalkerPosition(Vector3.Zero);
                        }
                    }
                }
                else
                {
                    if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                    {
                        Orbwalker.SetOrbwalkerPosition(Vector3.Zero);
                    }
                }
            }
            else
            {
                if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                {
                    Orbwalker.SetOrbwalkerPosition(Vector3.Zero);
                }
            }
        }

        private static void KillStealEvent()
        {
            if (KillStealOption.UseE && E.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(E.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.E) &&
                            !x.IsUnKillable()))
                {
                    if (target.IsValidTarget(E.Range))
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.Hitchance >= HitChance.High)
                        {
                            E.Cast(ePred.CastPosition);
                        }
                    }
                }
            }

            if (KillStealOption.UseR && R.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(R.Range) &&
                            KillStealOption.GetKillStealTarget(x.CharacterName) &&
                            x.Health <
                            Me.GetSpellDamage(x, SpellSlot.R) +
                            Me.GetSpellDamage(x, SpellSlot.R, DamageStage.SecondCast) && !x.IsUnKillable()))
                {
                    if (target.IsValidTarget(R.Range) && !target.IsValidTarget(MiscOption.GetSlider("R", "GlobalRMin").Value))
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

        private static void AutoUseEvent()
        {
            if (MiscOption.GetKey("R", "SemiRKey").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

                if (Me.Spellbook.GetSpell(SpellSlot.R).Level > 0 && R.IsReady())
                {
                    var target = MyTargetSelector.GetTarget(R.Range);
                    if (target.IsValidTarget(R.Range) && !target.IsValidTarget(MiscOption.GetSlider("R", "GlobalRMin").Value))
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.Hitchance >= HitChance.High)
                        {
                            R.Cast(rPred.CastPosition);
                        }
                    }
                }
            }

            if (MiscOption.GetBool("W", "WSlow").Enabled && W.IsReady() && Me.HasBuffOfType(BuffType.Slow))
            {
                W.Cast();
            }
        }

        private static void ComboEvent()
        {
            var target = MyTargetSelector.GetTarget(E.Range);

            if (target != null && target.IsValidTarget(E.Range))
            {
                if (ComboOption.UseW && W.IsReady() && !Me.HasBuff("dravenfurybuff"))
                {
                    if (target.DistanceToPlayer() >= 600)
                    {
                        W.Cast();
                    }
                    else
                    {
                        if (target.Health <
                            (AxeCount > 0
                                ? Me.GetSpellDamage(target, SpellSlot.Q) * 5
                                : Me.GetAutoAttackDamage(target) * 5))
                        {
                            W.Cast();
                        }
                    }
                }

                if (ComboOption.UseE && E.IsReady())
                {
                    if (!target.InAutoAttackRange() ||
                        target.Health <
                        (AxeCount > 0
                            ? Me.GetSpellDamage(target, SpellSlot.Q) * 3
                            : Me.GetAutoAttackDamage(target) * 3) || Me.HealthPercent < 40)
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.Hitchance >= HitChance.High)
                        {
                            E.Cast(ePred.CastPosition);
                        }
                    }
                }

                if (ComboOption.UseR && R.IsReady() && !target.IsValidTarget(MiscOption.GetSlider("R", "GlobalRMin").Value))
                {
                    if (ComboOption.GetBool("RSolo").Enabled)
                    {
                        if (target.Health <
                            Me.GetSpellDamage(target, SpellSlot.R) +
                            Me.GetSpellDamage(target, SpellSlot.R, DamageStage.SecondCast) +
                            (AxeCount > 0
                                ? Me.GetSpellDamage(target, SpellSlot.Q) * 2
                                : Me.GetAutoAttackDamage(target) * 2) +
                            (E.IsReady() ? Me.GetSpellDamage(target, SpellSlot.E) : 0) &&
                            target.Health >
                            (AxeCount > 0
                                ? Me.GetSpellDamage(target, SpellSlot.Q) * 3
                                : Me.GetAutoAttackDamage(target) * 3) &&
                            (Me.CountEnemyHeroesInRange(1000) == 1 ||
                             Me.CountEnemyHeroesInRange(1000) == 2 && Me.HealthPercent >= 60))
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.Hitchance >= HitChance.High)
                            {
                                R.Cast(rPred.CastPosition);
                            }
                        }
                    }

                    if (ComboOption.GetBool("RTeam").Enabled)
                    {
                        if (Me.CountAllyHeroesInRange(1000) <= 3 && Me.CountEnemyHeroesInRange(1000) <= 3)
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.Hitchance >= HitChance.Medium)
                            {
                                if (rPred.AoeTargetsHitCount >= 3)
                                {
                                    R.Cast(rPred.CastPosition);
                                }
                                else if (rPred.AoeTargetsHitCount >= 2)
                                {
                                    R.Cast(rPred.CastPosition);
                                }
                            }
                        }
                        else if (Me.CountAllyHeroesInRange(1000) <= 2 && Me.CountEnemyHeroesInRange(1000) <= 4)
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.Hitchance >= HitChance.Medium)
                            {
                                if (rPred.AoeTargetsHitCount >= 3)
                                {
                                    R.Cast(rPred.CastPosition);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void HarassEvent()
        {
            if (HarassOption.HasEnouguMana() && HarassOption.UseE && E.IsReady())
            {
                var target = HarassOption.GetTarget(E.Range);

                if (target != null && target.IsValidTarget(E.Range))
                {
                    var ePred = E.GetPrediction(target);

                    if (ePred.Hitchance >= HitChance.High ||
                        ePred.Hitchance >= HitChance.Medium && ePred.AoeTargetsHitCount > 1)
                    {
                        E.Cast(ePred.CastPosition);
                    }
                }
            }
        }

        private static void ClearEvent()
        {
            if (MyManaManager.SpellHarass && Me.CountEnemyHeroesInRange(E.Range) > 0)
            {
                HarassEvent();
            }

            if (MyManaManager.SpellFarm)
            {
                LaneClearEvent();
                JungleClearEvent();
            }
        }

        private static void LaneClearEvent()
        {
            if (LaneClearOption.HasEnouguMana())
            {
                if (LaneClearOption.UseQ && Q.IsReady() && AxeCount < 2 && Orbwalker.CanAttack())
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(600) && x.IsMinion()).ToList();

                    if (minions.Any() && minions.Count >= 2)
                    {
                        Q.Cast();
                    }
                }

                if (LaneClearOption.GetSliderBool("LaneClearECount").Enabled && E.IsReady())
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion()).ToList();

                    if (minions.Any() && minions.Count >= LaneClearOption.GetSliderBool("LaneClearECount").Value)
                    {
                        var eFarm = E.GetLineFarmLocation(minions);

                        if (eFarm.MinionsHit >= LaneClearOption.GetSliderBool("LaneClearECount").Value)
                        {
                            E.Cast(eFarm.Position);
                        }
                    }
                }
            }
        }

        private static void JungleClearEvent()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range) && x.GetJungleType() != JungleType.Unknown)
                                             .OrderByDescending(x => x.MaxHealth)
                                             .ToList();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseE && E.IsReady() && mob != null && mob.IsValidTarget(E.Range))
                    {
                        E.CastIfHitchanceEquals(mob, HitChance.Medium);
                    }

                    if (JungleClearOption.UseW && W.IsReady() && !Me.HasBuff("dravenfurybuff") && AxeCount > 0)
                    {
                        foreach (
                            var m in
                            mobs.Where(
                                x =>
                                    x.DistanceToPlayer() <= 600 && !x.Name.ToLower().Contains("mini") &&
                                    !x.Name.ToLower().Contains("crab") && x.MaxHealth > 1500 &&
                                    x.Health > Me.GetAutoAttackDamage(x) * 2))
                        {
                            if (m.IsValidTarget(600))
                            {
                                W.Cast();
                            }
                        }
                    }

                    if (JungleClearOption.UseQ && Q.IsReady() && AxeCount < 2 && Orbwalker.CanAttack())
                    {
                        if (mobs.Count >= 2)
                        {
                            Q.Cast();
                        }

                        if (mobs.Count == 1 && mob != null && mob.InAutoAttackRange() && mob.Health > Me.GetAutoAttackDamage(mob) * 5)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        private static void OnWndProc(GameWndProcEventArgs Args)
        {
            if (AxeOption.GetBool("CancelCatch").Enabled)
            {
                if (AxeOption.GetBool("CancelKey2").Enabled && (Args.Msg == 516 || Args.Msg == 517))
                {
                    if (Variables.GameTimeTickCount - lastCatchTime > 1800)
                    {
                        lastCatchTime = Variables.GameTimeTickCount;
                    }
                }

                if (AxeOption.GetBool("CancelKey3").Enabled && Args.Msg == 519)
                {
                    if (Variables.GameTimeTickCount - lastCatchTime > 1800)
                    {
                        lastCatchTime = Variables.GameTimeTickCount;
                    }
                }
            }
        }

        private static void OnCreate(GameObject sender)
        {
            if (sender != null && sender.Name.Contains("Draven_") && sender.Name.Contains("_Q_reticle_self"))
            {
                AxeList.Add(sender, Variables.GameTimeTickCount + 1800);
            }
        }

        private static void OnDestroy(GameObject sender)
        {
            if (sender != null && sender.Name.Contains("Draven_") && sender.Name.Contains("_Q_reticle_self"))
            {
                if (AxeList.Any(o => o.Key.NetworkId == sender.NetworkId))
                {
                    AxeList.Remove(sender);
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

        private static void OnAction(object sender, OrbwalkerActionArgs Args)
        {
            if (Args.Type == OrbwalkerType.BeforeAttack)
            {
                if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0 || !Q.IsReady())
                {
                    return;
                }

                switch (Args.Target.Type)
                {
                    case GameObjectType.AIHeroClient:
                    {
                        var target = (AIHeroClient)Args.Target;
                        if (target != null && target.InAutoAttackRange())
                        {
                            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                            {
                                if (ComboOption.UseQ && AxeOption.GetSlider("CatchCount").Value >= AxeCount)
                                {
                                    Q.Cast();
                                }
                            }
                            else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass || Orbwalker.ActiveMode == OrbwalkerMode.LaneClear &&
                                     MyManaManager.SpellHarass)
                            {
                                if (HarassOption.HasEnouguMana() && HarassOption.GetHarassTargetEnabled(target.CharacterName))
                                {
                                    if (HarassOption.UseQ)
                                    {
                                        if (AxeCount < 2)
                                        {
                                            Q.Cast();
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

        private static void OnRender(EventArgs args)
        {
            if (Me.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
            {
                return;
            }

            if (DrawOption.GetBool("AxeRange").Enabled)
            {
                Render.Circle.DrawCircle(Game.CursorPos, AxeOption.GetSlider("CatchRange").Value, Color.FromArgb(0, 255, 161), 1);
            }

            if (DrawOption.GetBool("AxePosition").Enabled)
            {
                foreach (var axe in AxeList.Where(x => !x.Key.IsDead && x.Key.IsValid).Select(x => x.Key))
                {
                    if (axe != null && axe.IsValid)
                    {
                        Render.Circle.DrawCircle(axe.Position, 130, Color.FromArgb(86, 0, 255), 1);
                    }
                }
            }
        }
    }
}