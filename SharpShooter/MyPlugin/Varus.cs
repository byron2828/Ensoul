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

    public class Varus : MyLogic
    {
        private static int lastQTime, lastETime;

        public Varus()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 925f);
            Q.SetSkillshot(0.25f, 70f, 1650f, false, false, SkillshotType.Line);
            Q.SetCharged("VarusQ", "VarusQ", 925, 1600, 1.5f);

            W = new Spell(SpellSlot.W, 0f);

            E = new Spell(SpellSlot.E, 975f);
            E.SetSkillshot(0.35f, 120f, 1500f, false, true, SkillshotType.Circle);

            R = new Spell(SpellSlot.R, 1050f);
            R.SetSkillshot(0.25f, 120f, 1950f, false, false, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddSlider("ComboQPassive", "Use Q |Target Stack Count >= x", 3, 0, 3);
            ComboOption.AddBool("ComboQFast", "Use Q |Fast Cast");
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddSlider("ComboEPassive", "Use E |Target Stack Count >= x", 3, 0, 3);
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSolo", "Use R |Solo Mode");
            ComboOption.AddSlider("ComboRCount", "Use R |Min Hit Count >= x", 3, 1, 5);

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddE(false);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q |Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E |Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
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
            MiscOption.AddBool("R", "AutoR", "Auto R |Anti Gapcloser");

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(true, true, true, true, true);

            Tick.OnTick += OnUpdate;
            //Gapcloser.OnGapcloser += OnGapcloser;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            Spellbook.OnCastSpell += OnCastSpell;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Me.HasBuff("VarusQLaunch") || Me.HasBuff("VarusQ"))
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }

            if (Me.IsWindingUp)
            {
                return;
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

            if (R.IsReady())
            {
                if (Q.IsCharging)
                {
                    return;
                }

                var target = MyTargetSelector.GetTarget(R.Range);

                if (target.IsValidTarget(R.Range))
                {
                    var rPred = R.GetPrediction(target);

                    if (rPred.Hitchance >= HitChance.High)
                    {
                        R.Cast(rPred.UnitPosition);
                    }
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.IsReady() && Variables.GameTimeTickCount - lastETime > 1000)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(1600f) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q) + GetWDamage(x)))
                {
                    if (target.IsUnKillable())
                    {
                        return;
                    }

                    if (Q.IsCharging)
                    {
                        if (target.IsValidTarget(Q.ChargedMaxRange))
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.Hitchance >= HitChance.High)
                            {
                                W.Cast();
                                Q.ShootChargedSpell(qPred.CastPosition);
                            }
                        }
                        else
                        {
                            foreach (
                                var t in
                                GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget(Q.ChargedMaxRange))
                                    .OrderBy(x => x.Health))
                            {
                                if (t.IsValidTarget(Q.ChargedMaxRange))
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.Hitchance >= HitChance.High)
                                    {
                                        W.Cast();
                                        Q.ShootChargedSpell(qPred.CastPosition);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (target.IsValidTarget(Q.ChargedMinRange))
                        {
                            W.Cast();
                            Q.ShootChargedSpell(target.Position);
                        }
                        else
                        {

                            Q.StartCharging();
                        }
                    }
                    return;
                }
            }

            if (Q.IsCharging)
            {
                return;
            }

            if (KillStealOption.UseE && E.IsReady() && Variables.GameTimeTickCount - lastQTime > 1000)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(E.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.E) + GetWDamage(x)))
                {
                    if (target.IsUnKillable())
                    {
                        return;
                    }

                    var ePred = E.GetPrediction(target);

                    if (ePred.Hitchance >= HitChance.High)
                    {
                        E.Cast(ePred.UnitPosition);
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.UseE && E.IsReady() && !Q.IsCharging && Variables.GameTimeTickCount - lastQTime > 750 + Game.Ping)
            {
                var target = MyTargetSelector.GetTarget(E.Range);

                if (target != null && target.IsValidTarget(E.Range) && (GetBuffCount(target) >= ComboOption.GetSlider("ComboEPassive").Value ||
                    W.Level == 0 || target.Health < Me.GetSpellDamage(target, SpellSlot.E) + GetWDamage(target) ||
                    !target.InAutoAttackRange() && !Q.IsReady()))
                {
                    var ePred = E.GetPrediction(target);

                    if (ePred.Hitchance >= HitChance.High)
                    {
                        E.Cast(ePred.UnitPosition);
                    }
                }
            }

            if (ComboOption.UseQ && Q.IsReady() && Variables.GameTimeTickCount - lastETime > 750 + Game.Ping)
            {
                var target = MyTargetSelector.GetTarget(1600f);

                if (target != null && target.IsValidTarget(1600f))
                {
                    if (Q.IsCharging)
                    {
                        if (ComboOption.GetBool("ComboQFast").Enabled && target.IsValidTarget(800))
                        {
                            W.Cast();
                            Q.ShootChargedSpell(target.Position);
                        }
                        else if (target.IsValidTarget(Q.ChargedMaxRange))
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.Hitchance >= HitChance.High)
                            {
                                W.Cast();
                                Q.ShootChargedSpell(qPred.CastPosition);
                            }
                        }
                    }
                    else
                    {
                        if (GetBuffCount(target) >= ComboOption.GetSlider("ComboQPassive").Value || W.Level == 0 ||
                            target.Health < Me.GetSpellDamage(target, SpellSlot.Q) + GetWDamage(target))
                        {
                            Q.StartCharging();
                        }
                    }
                }
                else
                {
                    foreach (var t in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget(1600)))
                    {
                        if (t.IsValidTarget(1600))
                        {
                            if (Q.IsCharging)
                            {
                                if (ComboOption.GetBool("ComboQFast").Enabled && t.IsValidTarget(800))
                                {
                                    W.Cast();
                                    Q.ShootChargedSpell(t.Position);
                                }
                                else if (t.IsValidTarget(Q.ChargedMaxRange))
                                {
                                    var qPred = Q.GetPrediction(t);

                                    if (qPred.Hitchance >= HitChance.High)
                                    {
                                        W.Cast();
                                        Q.ShootChargedSpell(qPred.CastPosition);
                                    }
                                }
                            }
                            else
                            {
                                if (GetBuffCount(t) >= ComboOption.GetSlider("ComboQPassive").Value || W.Level == 0 ||
                                    t.Health < Me.GetSpellDamage(t, SpellSlot.Q) + GetWDamage(t))
                                {
                                    Q.StartCharging();
                                }
                            }
                        }
                    }
                }
            }

            if (ComboOption.UseR && R.IsReady())
            {
                var target = MyTargetSelector.GetTarget(R.Range);

                if (target.IsValidTarget(R.Range) && ComboOption.GetBool("ComboRSolo").Enabled &&
                    Me.CountEnemyHeroesInRange(1000) <= 2)
                {
                    if (target.Health + target.HPRegenRate * 2 <
                         Me.GetSpellDamage(target, SpellSlot.R) + GetWDamage(target) +
                        (E.IsReady() ? Me.GetSpellDamage(target, SpellSlot.E) : 0) +
                        (Q.IsReady() ? Me.GetSpellDamage(target, SpellSlot.Q) : 0) + Me.GetAutoAttackDamage(target) * 3)
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.Hitchance >= HitChance.High)
                        {
                            R.Cast(rPred.UnitPosition);
                        }
                    }
                }

                foreach (var rTarget in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && !x.HaveShiledBuff()))
                {
                    var rPred = R.GetPrediction(rTarget);

                    if (rPred.AoeTargetsHitCount >= ComboOption.GetSlider("ComboRCount").Value &&
                        Me.CountEnemyHeroesInRange(R.Range) >= ComboOption.GetSlider("ComboRCount").Value)
                    {
                        R.Cast(rPred.CastPosition);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                var target = HarassOption.GetTarget(1600f);

                if (target.IsValidTarget(1600f))
                {
                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(1600))
                    {
                        if (Q.IsCharging)
                        {
                            if (target != null && target.IsValidTarget(Q.ChargedMaxRange))
                            {
                                if (target.IsValidTarget(800))
                                {
                                    W.Cast();
                                    Q.ShootChargedSpell(target.Position);
                                }
                                else
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.Hitchance >= HitChance.High)
                                    {
                                        W.Cast();
                                        Q.ShootChargedSpell(qPred.CastPosition);
                                    }
                                }
                            }
                            else
                            {
                                foreach (
                                    var t in
                                    GameObjects.EnemyHeroes.Where(
                                            x => !x.IsDead && x.IsValidTarget(Q.ChargedMaxRange))
                                        .OrderBy(x => x.Health))
                                {
                                    if (t.IsValidTarget(800))
                                    {
                                        W.Cast();
                                        Q.ShootChargedSpell(t.Position);
                                    }
                                    else if (t.IsValidTarget(Q.ChargedMinRange))
                                    {
                                        var qPred = Q.GetPrediction(t);

                                        if (qPred.Hitchance >= HitChance.High)
                                        {
                                            W.Cast();
                                            Q.ShootChargedSpell(qPred.CastPosition);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Q.StartCharging();
                        }
                    }

                    if (Q.IsCharging)
                    {
                        return;
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
                if (LaneClearOption.UseQ && Q.IsReady())
                {
                    var qMinions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(1600f) && x.IsMinion()).ToList();

                    if (qMinions.Any())
                    {
                        var qFarm = Q.GetLineFarmLocation(qMinions);

                        if (qFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearQCount").Value)
                        {
                            if (Q.IsCharging)
                            {
                                if (qFarm.Position.DistanceToPlayer() <= Q.ChargedMaxRange)
                                {
                                    Q.ShootChargedSpell(qFarm.Position);
                                }
                            }
                            else
                            {
                                Q.StartCharging();
                            }
                        }
                    }
                }

                if (Q.IsCharging)
                {
                    return;
                }

                if (LaneClearOption.UseE && E.IsReady())
                {
                    var eMinions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion()).ToList();

                    if (eMinions.Any())
                    {
                        var eFarm = E.GetCircularFarmLocation(eMinions);

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
                var mobs =
                    GameObjects.Jungle.Where(x => x.IsValidTarget(1200) && x.GetJungleType() != JungleType.Unknown)
                        .Where(x => x.GetJungleType() != JungleType.Small)
                        .ToList();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (mob != null)
                    {
                        if (JungleClearOption.UseQ && Q.IsReady() && mob.IsValidTarget(1600f))
                        {
                            if (Q.IsCharging)
                            {
                                if (mob.IsValidTarget(Q.Range))
                                {
                                    Q.ShootChargedSpell(mob.PreviousPosition);
                                }
                            }
                            else
                            {
                                Q.StartCharging();
                            }
                        }

                        if (Q.IsCharging)
                        {
                            return;
                        }

                        if (JungleClearOption.UseE && E.IsReady() && mob.IsValidTarget(E.Range))
                        {
                            E.Cast(mob.PreviousPosition);
                        }
                    }
                }
            }
        }

        //private static void OnGapcloser(AIHeroClient target, GapcloserArgs Args)
        //{
        //    if (MiscOption.GetBool("R", "AutoR").Enabled && R.IsReady() && target != null && target.IsValidTarget(R.Range))
        //    {
        //        switch (Args.Type)
        //        {
        //            case SpellType.Melee:
        //                if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
        //                {
        //                    var rPred = R.GetPrediction(target);
        //                    R.Cast(rPred.UnitPosition);
        //                }
        //                break;
        //            case SpellType.Dash:
        //                if (Args.EndPosition.DistanceToPlayer() <= 350)
        //                {
        //                    var rPred = R.GetPrediction(target);
        //                    R.Cast(rPred.UnitPosition);
        //                }
        //                break;
        //            case SpellType.SkillShot:
        //            case SpellType.Targeted:
        //                {
        //                    var rPred = R.GetPrediction(target);
        //                    R.Cast(rPred.UnitPosition);
        //                }
        //                break;
        //        }
        //    }
        //}

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs Args)
        {
            if (sender.IsMe)
            {
                if (Args.Slot == SpellSlot.Q)
                {
                    lastQTime = Variables.GameTimeTickCount;

                    if (ComboOption.UseW && W.IsReady() && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                    {
                        W.Cast();
                    }
                }

                if (Args.Slot == SpellSlot.E)
                {
                    lastETime = Variables.GameTimeTickCount;
                }
            }
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs Args)
        {
            if (sender?.Owner != null && sender.Owner.IsMe)
            {
                if (Args.Slot == SpellSlot.Q)
                {
                    if (ComboOption.UseW && W.IsReady() && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                    {
                        W.Cast();
                    }

                    Args.Process = Variables.GameTimeTickCount - lastETime >= 750 + Game.Ping;
                }

                if (Args.Slot == SpellSlot.E)
                {
                    Args.Process = Variables.GameTimeTickCount - lastQTime >= 750 + Game.Ping;
                }
            }
        }

        private static int GetBuffCount(AIBaseClient target)
        {
            return target.HasBuff("VarusWDebuff") ? target.GetBuffCount("VarusWDebuff") : 0;
        }

        private static double GetWDamage(AIBaseClient target)
        {
            var dmg = GetBuffCount(target) * Me.GetSpellDamage(target, SpellSlot.W, DamageStage.Detonation);

            if (target.Type == GameObjectType.AIMinionClient)
            {
                return dmg >= 360 ? 360 : dmg;
            }

            return dmg;
        }
    }
}
