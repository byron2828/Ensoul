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
    using EnsoulSharp.SDK.Utility;
    using EnsoulSharp.SDK.Events;
    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using Keys = System.Windows.Forms.Keys;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    public class Jhin : MyLogic
    {
        private static AIHeroClient rShotTarget { get; set; }
        private static int lastETime { get; set; }
        private static bool isAttacking { get; set; }

        public Jhin()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 600f);

            W = new Spell(SpellSlot.W, 2500f);
            W.SetSkillshot(0.75f, 40f, 5000f, false, false, SkillshotType.Line);

            E = new Spell(SpellSlot.E, 750f);
            E.SetSkillshot(0.50f, 120f, 1600f, false, true, SkillshotType.Circle);

            R = new Spell(SpellSlot.R, 3500f);
            R.SetSkillshot(0.21f, 80f, 5000f, true, false, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboQMinion", "Use Q| On Minion", false);
            ComboOption.AddW();
            ComboOption.AddBool("ComboWAA", "Use W| After Attack");
            ComboOption.AddBool("ComboWOnly", "Use W| Only Use to MarkTarget");
            ComboOption.AddE();
            ComboOption.AddR();

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddBool("HarassQMinion", "Use Q| On Minion");
            HarassOption.AddW();
            HarassOption.AddBool("HarassWOnly", "Use W| Only Use to MarkTarget");
            HarassOption.AddE();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddW();
            LaneClearOption.AddMana();
            LaneClearOption.AddBool("LaneClearReload", "Use Spell Clear| Only Jhin Reloading");

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddMenu();
            LastHitOption.AddQ();
            LastHitOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddW();
            KillStealOption.AddBool("KillStealWInAttackRange", "Use W| Target In Attack Range");

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddW();
            MiscOption.AddBool("W", "AutoW", "Auto W| CC");
            MiscOption.AddE();
            MiscOption.AddBool("E", "AutoE", "Auto E| CC");
            MiscOption.AddR();
            MiscOption.AddBool("R", "rMenuAuto", "Auto R");
            MiscOption.AddKey("R", "rMenuSemi", "Semi-manual R Key", Keys.T, KeyBindType.Press);
            MiscOption.AddBool("R", "rMenuCheck", "Use R| Check is Safe?");
            MiscOption.AddSlider("R", "rMenuMin", "Use R| Min Range >= x", 1000, 500, 2500);
            MiscOption.AddSlider("R", "rMenuMax", "Use R| Max Range <= x", 3000, 1500, 3500);
            MiscOption.AddSlider("R", "rMenuKill", "Use R| Min Shot Can Kill >= x (0 = off)", 3, 0, 4);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(true, true, false, true, true);

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

            RLogic();

            if (R.Name == "JhinRShot")
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

            KillSteal();
            Auto();

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

        private static void RLogic()
        {
            AIHeroClient target = null;

            if (TargetSelector.SelectedTarget != null &&
                TargetSelector.SelectedTarget.DistanceToPlayer() <= MiscOption.GetSlider("R", "rMenuMax").Value)
            {
                target = TargetSelector.SelectedTarget;
            }
            else
            {
                target = MyTargetSelector.GetTarget(R.Range);
            }

            if (R.IsReady())
            {
                switch (R.Name)
                {
                    case "JhinR":
                        {
                            if (target.IsValidTarget(R.Range))
                            {
                                var rPred = R.GetPrediction(target);

                                if (MiscOption.GetKey("R", "rMenuSemi").Active)
                                {
                                    if (R.Cast(rPred.UnitPosition))
                                    {
                                        rShotTarget = target;
                                        return;
                                    }
                                }

                                if (!MiscOption.GetBool("R", "rMenuAuto").Enabled)
                                {
                                    return;
                                }

                                if (MiscOption.GetBool("R", "rMenuCheck").Enabled && Me.CountEnemyHeroesInRange(800f) > 0)
                                {
                                    return;
                                }

                                if (target.DistanceToPlayer() <= MiscOption.GetSlider("R", "rMenuMin").Value)
                                {
                                    return;
                                }

                                if (target.DistanceToPlayer() > MiscOption.GetSlider("R", "rMenuMax").Value)
                                {
                                    return;
                                }

                                if (MiscOption.GetSlider("R", "rMenuKill").Value == 0 ||
                                    target.Health > Me.GetSpellDamage(target, SpellSlot.R) * MiscOption.GetSlider("R", "rMenuKill").Value)
                                {
                                    return;
                                }

                                if (IsSpellHeroCollision(target, R))
                                {
                                    return;
                                }

                                if (R.Cast(rPred.UnitPosition))
                                {
                                    rShotTarget = target;
                                }
                            }
                        }
                        break;
                    case "JhinRShot":
                        {
                            var selectTarget = TargetSelector.SelectedTarget;

                            if (selectTarget != null && selectTarget.IsValidTarget(R.Range) && InRCone(selectTarget))
                            {
                                var rPred = R.GetPrediction(selectTarget);

                                if (MiscOption.GetKey("R", "rMenuSemi").Active)
                                {
                                    AutoUse(rShotTarget);

                                    if (rPred.Hitchance >= HitChance.High)
                                    {
                                        R.Cast(rPred.CastPosition);
                                    }

                                    return;
                                }

                                if (ComboOption.UseR && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                                {
                                    AutoUse(rShotTarget);

                                    if (rPred.Hitchance >= HitChance.High)
                                    {
                                        R.Cast(rPred.CastPosition);
                                    }

                                    return;
                                }

                                if (!MiscOption.GetBool("R", "rMenuAuto").Enabled)
                                {
                                    return;
                                }

                                AutoUse(rShotTarget);

                                if (rPred.Hitchance >= HitChance.High)
                                {
                                    R.Cast(rPred.CastPosition);
                                }

                                return;
                            }

                            foreach (
                                var t in
                                GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && InRCone(x))
                                    .OrderBy(x => x.Health).ThenByDescending(x => Me.GetSpellDamage(x, SpellSlot.R)))
                            {
                                if (t.IsValidTarget(R.Range) && !target.IsUnKillable())
                                {
                                    var rPred = R.GetPrediction(t);

                                    if (MiscOption.GetKey("R", "rMenuSemi").Active)
                                    {
                                        AutoUse(t);

                                        if (rPred.Hitchance >= HitChance.High)
                                        {
                                            R.Cast(rPred.CastPosition);
                                        }

                                        return;
                                    }

                                    if (ComboOption.UseR && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                                    {
                                        AutoUse(t);

                                        if (rPred.Hitchance >= HitChance.High)
                                        {
                                            R.Cast(rPred.CastPosition);
                                        }

                                        return;
                                    }

                                    if (!MiscOption.GetBool("R", "rMenuAuto").Enabled)
                                    {
                                        return;
                                    }

                                    AutoUse(t);

                                    if (rPred.Hitchance >= HitChance.High)
                                    {
                                        R.Cast(rPred.CastPosition);
                                    }

                                    return;
                                }
                            }
                        }
                        break;
                }
            }
        }

        private static void KillSteal()
        {
            if (R.Name == "JhinRShot")
            {
                return;
            }

            if (KillStealOption.UseQ && Q.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                {
                    if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                    {
                        Q.CastOnUnit(target);
                    }
                }
            }

            if (KillStealOption.UseW && W.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(W.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.W)))
                {
                    if (target.IsValidTarget(W.Range) && !target.IsUnKillable())
                    {
                        var wPred = W.GetPrediction(target);

                        if (target.Health < Me.GetSpellDamage(target, SpellSlot.Q) && Q.IsReady() &&
                            target.IsValidTarget(Q.Range))
                        {
                            return;
                        }

                        if (KillStealOption.GetBool("KillStealWInAttackRange").Enabled && target.InAutoAttackRange())
                        {
                            if (wPred.Hitchance >= HitChance.High)
                            {
                                W.Cast(wPred.UnitPosition);
                            }
                            return;
                        }

                        if (target.InAutoAttackRange() &&
                            target.Health <= Me.GetAutoAttackDamage(target))
                        {
                            return;
                        }

                        if (wPred.Hitchance >= HitChance.High)
                        {
                            W.Cast(wPred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Auto()
        {
            if (R.Name == "JhinRShot")
            {
                return;
            }

            foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(W.Range) && !x.CanMoveMent()))
            {
                if (MiscOption.GetBool("W", "AutoW").Enabled && W.IsReady() && target.IsValidTarget(W.Range))
                {
                    W.Cast(target.PreviousPosition);
                }

                if (MiscOption.GetBool("E", "AutoE").Enabled && E.IsReady() &&
                    target.IsValidTarget(E.Range) && Variables.GameTimeTickCount - lastETime > 2500 && !isAttacking)
                {
                    E.Cast(target.PreviousPosition);
                }
            }
        }

        private static void Combo()
        {
            if (R.Name == "JhinRShot")
            {
                return;
            }

            if (ComboOption.UseW && W.IsReady())
            {
                var target = MyTargetSelector.GetTarget(W.Range);

                if (target != null && target.IsValidTarget(W.Range))
                {
                    if (ComboOption.GetBool("ComboWOnly").Enabled)
                    {
                        if (HasPassive(target))
                        {
                            var wPred = W.GetPrediction(target);

                            if (wPred.Hitchance >= HitChance.High)
                            {
                                W.Cast(wPred.UnitPosition);
                            }
                        }
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

            if (ComboOption.UseQ && Q.IsReady())
            {
                var target = MyTargetSelector.GetTarget(Q.Range + 300);
                var qTarget = MyTargetSelector.GetTarget(Q.Range);

                if (qTarget.IsValidTarget(Q.Range) && !Orbwalker.CanAttack())
                {
                    Q.Cast(qTarget);
                }
                else if (target.IsValidTarget(Q.Range + 300) && ComboOption.GetBool("ComboQMinion").Enabled)
                {
                    if (Me.HasBuff("JhinPassiveReload") || !Me.HasBuff("JhinPassiveReload") &&
                        Me.CountEnemyHeroesInRange(Me.AttackRange + Me.BoundingRadius) == 0)
                    {
                        var qPred =
                            SpellPrediction.GetPrediction(new SpellPrediction.PredictionInput { Unit = target, Delay = 0.25f });
                        var bestQMinion =
                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(300, true, qPred.CastPosition) && x.MaxHealth > 5)
                                .Where(x => x.IsValidTarget(Q.Range))
                                .OrderBy(x => x.Distance(target))
                                .ThenBy(x => x.Health)
                                .FirstOrDefault();

                        if (bestQMinion != null && bestQMinion.IsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(bestQMinion);
                        }
                    }
                }
            }

            if (ComboOption.UseE && E.IsReady() && Variables.GameTimeTickCount - lastETime > 2500 && !isAttacking)
            {
                var target = MyTargetSelector.GetTarget(E.Range);

                if (target != null && target.IsValidTarget(E.Range))
                {
                    if (!target.CanMoveMent())
                    {
                        E.Cast(target.PreviousPosition);
                    }
                    else
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.Hitchance >= HitChance.High)
                        {
                            E.Cast(ePred.CastPosition);
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
                    var target = HarassOption.GetTarget(Q.Range + 300);

                    if (target.IsValidTarget(Q.Range))
                    {
                        Q.Cast(target);
                    }
                    else if (target.IsValidTarget(Q.Range + 300) && HarassOption.GetBool("HarassQMinion").Enabled)
                    {
                        if (Me.HasBuff("JhinPassiveReload") || !Me.HasBuff("JhinPassiveReload") &&
                            Me.CountEnemyHeroesInRange(Me.AttackRange + Me.BoundingRadius) == 0)
                        {
                            var qPred =
                                SpellPrediction.GetPrediction(new SpellPrediction.PredictionInput { Unit = target, Delay = 0.25f });
                            var bestQMinion =
                                GameObjects.EnemyMinions.Where(x => x.IsValidTarget(300, true, qPred.CastPosition) && x.MaxHealth > 5)
                                    .Where(x => x.IsValidTarget(Q.Range))
                                    .OrderBy(x => x.Distance(target))
                                    .ThenBy(x => x.Health)
                                    .FirstOrDefault();

                            if (bestQMinion != null && bestQMinion.IsValidTarget(Q.Range))
                            {
                                Q.CastOnUnit(bestQMinion);
                            }
                        }
                    }
                }

                if (HarassOption.UseE && E.IsReady() && Variables.GameTimeTickCount - lastETime > 2500 && !isAttacking)
                {
                    var target = HarassOption.GetTarget(E.Range);

                    if (target.IsValidTarget(E.Range))
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.Hitchance >= HitChance.High)
                        {
                            E.Cast(ePred.CastPosition);
                        }
                    }
                }

                if (HarassOption.UseW && W.IsReady())
                {
                    var target = HarassOption.GetTarget(1500);

                    if (target.IsValidTarget(W.Range))
                    {
                        if (HarassOption.GetBool("HarassWOnly").Enabled && !HasPassive(target))
                        {
                            return;
                        }

                        var wPred = W.GetPrediction(target);

                        if (wPred.Hitchance >= HitChance.High)
                        {
                            W.Cast(wPred.UnitPosition);
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
            if (LaneClearOption.GetBool("LaneClearReload").Enabled && !Me.HasBuff("JhinPassiveReload"))
            {
                return;
            }

            if (LaneClearOption.HasEnouguMana())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range + 300) && x.IsMinion()).ToList();

                if (minions.Any())
                {
                    var minion = minions.MinOrDefault(x => x.Health);

                    if (LaneClearOption.UseQ && Q.IsReady())
                    {
                        if (minion != null && minion.IsValidTarget(Q.Range) && minions.Count >= 2 &&
                            minion.Health < Me.GetSpellDamage(minion, SpellSlot.Q))
                        {
                            Q.Cast(minion);
                        }
                    }

                    if (LaneClearOption.UseW && W.IsReady())
                    {
                        var wFarm = W.GetLineFarmLocation(minions);

                        if (wFarm.MinionsHit >= 3)
                        {
                            W.Cast(wFarm.Position);
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
                    GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() != JungleType.Unknown)
                        .OrderByDescending(x => x.MaxHealth)
                        .ToList();

                if (mobs.Any())
                {
                    var bigmob = mobs.FirstOrDefault(x => x.GetJungleType() != JungleType.Small);

                    if (bigmob != null && bigmob.IsValidTarget(E.Range) && (!bigmob.InAutoAttackRange() || !Orbwalker.CanAttack()))
                    {
                        if (JungleClearOption.UseE && E.IsReady() && bigmob.IsValidTarget(E.Range))
                        {
                            E.Cast(bigmob.PreviousPosition);
                        }

                        if (JungleClearOption.UseQ && Q.IsReady() && bigmob.IsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(bigmob);
                        }

                        if (JungleClearOption.UseW && W.IsReady() && bigmob.IsValidTarget(W.Range))
                        {
                            W.Cast(bigmob.PreviousPosition);
                        }
                    }
                    else
                    {
                        var farmMobs = mobs.Where(x => x.IsValidTarget(E.Range)).ToList();

                        if (JungleClearOption.UseE && E.IsReady())
                        {
                            var eFarm = E.GetCircularFarmLocation(farmMobs);

                            if (eFarm.MinionsHit >= 2)
                            {
                                E.Cast(eFarm.Position);
                            }
                        }

                        if (JungleClearOption.UseQ && Q.IsReady())
                        {
                            if (farmMobs.Count >= 2)
                            {
                                Q.CastOnUnit(farmMobs.FirstOrDefault());
                            }
                        }

                        if (JungleClearOption.UseW && W.IsReady())
                        {
                            var wFarm = W.GetLineFarmLocation(farmMobs);

                            if (wFarm.MinionsHit >= 2)
                            {
                                W.Cast(wFarm.Position);
                            }
                        }
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
                    var minion =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                            .OrderBy(x => x.Health)
                            .FirstOrDefault(x => x.Health < Me.GetSpellDamage(x, SpellSlot.Q));

                    if (minion != null && minion.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(minion);
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
                                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                                {
                                    Q.CastOnUnit(target);
                                }
                                else if (ComboOption.UseW && ComboOption.GetBool("ComboWAA").Enabled && W.IsReady() &&
                                         target.IsValidTarget(W.Range) && HasPassive(target))
                                {
                                    var wPred = W.GetPrediction(target);

                                    if (wPred.Hitchance >= HitChance.High)
                                    {
                                        W.Cast(wPred.UnitPosition);
                                    }
                                }
                            }
                            else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass ||
                                     Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && MyManaManager.SpellHarass)
                            {
                                if (HarassOption.HasEnouguMana())
                                {
                                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                                    {
                                        Q.CastOnUnit(target);
                                    }
                                    else if (HarassOption.UseW && W.IsReady() && target.IsValidTarget(W.Range) &&
                                             HasPassive(target))
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
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs Args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            var spellslot = Me.GetSpellSlotFromName(Args.SData.Name);

            if (spellslot == SpellSlot.E)
            {
                lastETime = Variables.GameTimeTickCount;
            }

            if (Args.SData.Name.Equals("attack", StringComparison.CurrentCultureIgnoreCase) ||
                Args.SData.Name.Equals("crit", StringComparison.CurrentCultureIgnoreCase))
            {
                isAttacking = true;
                DelayAction.Add(450 + Game.Ping, () => { isAttacking = false; });
            }
        }

        //private static void OnGapcloser(AIHeroClient target, GapcloserArgs Args)
        //{
        //    if (R.Name == "JhinRShot")
        //    {
        //        return;
        //    }

        //    if (target != null && target.IsValidTarget(E.Range) && !Args.HaveShield)
        //    {
        //        switch (Args.Type)
        //        {
        //            case SpellType.SkillShot:
        //                {
        //                    if (target.IsValidTarget(300))
        //                    {
        //                        if (E.IsReady() && Variables.GameTimeTickCount - lastETime > 2500 && !isAttacking)
        //                        {
        //                            var ePred = E.GetPrediction(target);

        //                            E.Cast(ePred.CastPosition);
        //                        }

        //                        if (W.IsReady() && HasPassive(target))
        //                        {
        //                            var wPred = W.GetPrediction(target);

        //                            W.Cast(wPred.UnitPosition);
        //                        }
        //                    }
        //                }
        //                break;
        //            case SpellType.Melee:
        //            case SpellType.Dash:
        //            case SpellType.Targeted:
        //                {
        //                    if (target.IsValidTarget(400))
        //                    {
        //                        if (E.IsReady() && Variables.GameTimeTickCount - lastETime > 2500 && !isAttacking)
        //                        {
        //                            var ePred = E.GetPrediction(target);

        //                            E.Cast(ePred.CastPosition);
        //                        }

        //                        if (W.IsReady() && HasPassive(target))
        //                        {
        //                            var wPred = W.GetPrediction(target);

        //                            W.Cast(wPred.UnitPosition);
        //                        }
        //                    }
        //                }
        //                break;
        //        }
        //    }
        //}

        private static void AutoUse(AIBaseClient target)
        {
            var item = new Items.Item(3363, 5000f);

            if (item.IsOwned() && item.IsReady)
            {
                item.Cast(target.PreviousPosition);
            }
        }

        private static bool HasPassive(AIBaseClient target)
        {
            return target.HasBuff("jhinespotteddebuff");
        }

        private static bool InRCone(GameObject target)
        {
            // Asuvril
            // https://github.com/VivianGit/LeagueSharp/blob/master/Jhin%20As%20The%20Virtuoso/Jhin%20As%20The%20Virtuoso/Extensions.cs#L67-L79
            var range = R.Range;
            const float angle = 70f * (float)Math.PI / 180;
            var end2 = target.Position.ToVector2() - Me.Position.ToVector2();
            var edge1 = end2.Rotated(-angle / 2);
            var edge2 = edge1.Rotated(angle);
            var point = target.Position.ToVector2() - Me.Position.ToVector2();

            return point.DistanceSquared(new Vector2()) < range * range && edge1.CrossProduct(point) > 0 &&
                   point.CrossProduct(edge2) > 0;
        }

        private static bool IsSpellHeroCollision(AIHeroClient t, Spell r, int extraWith = 50)
        {
            foreach (
                var hero in
                GameObjects.EnemyHeroes.Where(
                    hero =>
                        hero.IsValidTarget(r.Range + r.Width, true, r.From) &&
                        t.NetworkId != hero.NetworkId))
            {
                var prediction = r.GetPrediction(hero);
                var powCalc = Math.Pow(r.Width + extraWith + hero.BoundingRadius, 2);

                if (
                    prediction.UnitPosition.ToVector2()
                        .DistanceSquared(Me.PreviousPosition.ToVector2(), r.GetPrediction(t).CastPosition.ToVector2(), true) <=
                    powCalc)
                {
                    return true;
                }

                if (
                    prediction.UnitPosition.ToVector2()
                        .DistanceSquared(Me.PreviousPosition.ToVector2(), t.PreviousPosition.ToVector2(), true) <= powCalc)
                {
                    return true;
                }
            }

            return false;
        }
    }
}