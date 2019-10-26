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

    public class KogMaw : MyLogic
    {
        private static int GetRCount => Me.HasBuff("kogmawlivingartillerycost") ? Me.GetBuffCount("kogmawlivingartillerycost") : 0;

        private static float wRange => 500f + new[] {0, 130, 150, 170, 190, 210}[Me.Spellbook.GetSpell(SpellSlot.W).Level] + Me.BoundingRadius;
        private static float rRange => new[] { 1200, 1200, 1500, 1800 }[Me.Spellbook.GetSpell(SpellSlot.R).Level];

        public KogMaw()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 950f);
            Q.SetSkillshot(0.25f, 70f, 1650f, true, false, SkillshotType.Line);

            W = new Spell(SpellSlot.W, wRange);

            E = new Spell(SpellSlot.E, 1200f);
            E.SetSkillshot(0.25f, 120f, 1400f, false, true, SkillshotType.Line);

            R = new Spell(SpellSlot.R, rRange);
            R.SetSkillshot(1.20f, 120f, float.MaxValue, false, false, SkillshotType.Circle);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRLimit", "Use R|Max Buff Count < x", 3, 0, 10);
            ComboOption.AddBool("ComboROnlyOutAARange", "Use R|Only Target Out AA Range", false);
            ComboOption.AddSlider("ComboRHP", "Use R|target HealthPercent <= x%", 70, 1, 101);
            ComboOption.AddBool("ComboForcus", "Forcus Spell on Orbwalker Target", false);

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddR();
            HarassOption.AddSlider("HarassRLimit", "Use R|Max Buff Count < x", 5, 0, 10);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E|Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddR();
            LaneClearOption.AddSlider("LaneClearRLimit", "Use R|Max Buff Count < x", 4, 0, 10);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddR();
            JungleClearOption.AddSlider("JungleClearRLimit", "Use R|Max Buff Count < x", 5, 0, 10);
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddE();
            KillStealOption.AddSliderBool("KillStealRCount", "Use R|Max Buff Count < x", 3, 0, 10);
            KillStealOption.AddBool("KillStealOutAARange", "Only Target Out of AA Range");
            KillStealOption.AddTargetList();

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddE();
            MiscOption.AddBool("E", "AutoE", "Auto E| Anti Gapcloser");
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key", Keys.T, KeyBindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(true, true, true, true, true);

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

            if (Me.IsWindingUp)
            {
                return;
            }

            if (W.Level > 0)
            {
                W.Range = wRange;
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
            if (R.IsReady())
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
                        if (KillStealOption.GetBool("KillStealOutAARange").Enabled && target.InAutoAttackRange())
                        {
                            return;
                        }

                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.Medium)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }
            }

            if (KillStealOption.UseE && E.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(E.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.E)))
                {
                    if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                    {
                        if (KillStealOption.GetBool("KillStealOutAARange").Enabled && target.InAutoAttackRange())
                        {
                            return;
                        }

                        var ePred = E.GetPrediction(target);

                        if (ePred.Hitchance >= HitChance.High)
                        {
                            E.Cast(ePred.CastPosition);
                        }
                    }
                }
            }

            if (KillStealOption.GetSliderBool("KillStealRCount").Enabled && R.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(R.Range) && KillStealOption.GetKillStealTarget(x.CharacterName) && 
                        x.Health < Me.GetSpellDamage(x, SpellSlot.R)))
                {
                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable() &&
                        GetRCount < KillStealOption.GetSliderBool("KillStealRCount").Value)
                    {
                        if (KillStealOption.GetBool("KillStealOutAARange").Enabled && target.InAutoAttackRange())
                        {
                            return;
                        }

                        var rPred = R.GetPrediction(target);

                        if (rPred.Hitchance >= HitChance.High)
                        {
                            R.Cast(rPred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(R.Range, ComboOption.GetBool("ComboForcus").Enabled);

            if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
            {
                if (ComboOption.UseR && R.IsReady() && ComboOption.GetSlider("ComboRLimit").Value > GetRCount &&
                    target.IsValidTarget(R.Range) && target.HealthPercent <= ComboOption.GetSlider("ComboRHP").Value &&
                    (!ComboOption.GetBool("ComboROnlyOutAARange").Enabled ||
                     ComboOption.GetBool("ComboROnlyOutAARange").Enabled && !target.InAutoAttackRange()))
                {
                    var rPred = R.GetPrediction(target);

                    if (rPred.Hitchance >= HitChance.High)
                    {
                        R.Cast(rPred.CastPosition);
                    }
                }

                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    var qPred = Q.GetPrediction(target);

                    if (qPred.Hitchance >= HitChance.Medium)
                    {
                        Q.Cast(qPred.CastPosition);
                    }
                }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                {
                    var ePred = E.GetPrediction(target);

                    if (ePred.Hitchance >= HitChance.High)
                    {
                        E.Cast(ePred.CastPosition);
                    }
                }

                if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range) &&
                    !target.InAutoAttackRange() && Orbwalker.CanAttack())
                {
                    W.Cast();
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
                    if (HarassOption.UseR && R.IsReady() && HarassOption.GetSlider("HarassRLimit").Value > GetRCount &&
                        target.IsValidTarget(R.Range))
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.Hitchance >= HitChance.High)
                        {
                            R.Cast(rPred.CastPosition);
                        }
                    }

                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.Medium)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
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
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(R.Range) && x.IsMinion()).ToList();

                if (minions.Any())
                {
                    if (LaneClearOption.UseR && R.IsReady() && LaneClearOption.GetSlider("LaneClearRLimit").Value > GetRCount)
                    {
                        var rMinion =
                            minions.FirstOrDefault(x => x.DistanceToPlayer() > Me.AttackRange + Me.BoundingRadius);

                        if (rMinion != null && rMinion.IsValidTarget(R.Range))
                        {
                            R.Cast(rMinion);
                        }
                    }

                    if (LaneClearOption.UseE && E.IsReady())
                    {
                        var eMinions = minions.Where(x => x.IsValidTarget(E.Range)).ToList();
                        var eFarm = E.GetLineFarmLocation(eMinions);

                        if (eFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearECount").Value)
                        {
                            E.Cast(eFarm.Position);
                        }
                    }

                    if (LaneClearOption.UseQ && Q.IsReady())
                    {
                        var qMinion =
                            minions.Where(x => x.IsValidTarget(Q.Range))
                                .FirstOrDefault(
                                    x =>
                                        x.Health < Me.GetSpellDamage(x, SpellSlot.Q) &&
                                        x.Health > Me.GetAutoAttackDamage(x));

                        if (qMinion != null && qMinion.IsValidTarget(Q.Range))
                        {
                            Q.Cast(qMinion);
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

                    if (JungleClearOption.UseR && R.IsReady() &&
                        JungleClearOption.GetSlider("JungleClearRLimit").Value > GetRCount &&
                        bigmob != null && (!bigmob.InAutoAttackRange() || !Orbwalker.CanAttack()))
                    {
                        R.Cast(bigmob);
                    }

                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        if (bigmob != null && bigmob.IsValidTarget(E.Range) && (!bigmob.InAutoAttackRange() || !Orbwalker.CanAttack()))
                        {
                            E.Cast(bigmob);
                        }
                        else
                        {
                            var eMobs = mobs.Where(x => x.IsValidTarget(E.Range)).ToList();
                            var eFarm = E.GetLineFarmLocation(eMobs);

                            if (eFarm.MinionsHit >= 2)
                            {
                                E.Cast(eFarm.Position);
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

                            if (target != null && !target.IsDead)
                            {
                                if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range))
                                {
                                    W.Cast();
                                }
                                else if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.Hitchance >= HitChance.Medium)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                                else if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
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
                    break;
                case GameObjectType.AIMinionClient:
                    {
                        if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && JungleClearOption.HasEnouguMana())
                        {
                            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(R.Range) && x.GetJungleType() != JungleType.Unknown).ToList();

                            if (mobs.Any())
                            {
                                var mob = mobs.FirstOrDefault();
                                var bigmob = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

                                if (JungleClearOption.UseW && W.IsReady() && bigmob != null && bigmob.IsValidTarget(W.Range))
                                {
                                    W.Cast();
                                }
                                else if (JungleClearOption.UseE && E.IsReady())
                                {
                                    if (bigmob != null && bigmob.IsValidTarget(E.Range))
                                    {
                                        E.Cast(bigmob);
                                    }
                                    else
                                    {
                                        var eMobs = mobs.Where(x => x.IsValidTarget(E.Range)).ToList();
                                        var eFarm = E.GetLineFarmLocation(eMobs);

                                        if (eFarm.MinionsHit >= 2)
                                        {
                                            E.Cast(eFarm.Position);
                                        }
                                    }
                                }
                                else if (JungleClearOption.UseQ && Q.IsReady() && mob != null && mob.IsValidTarget(Q.Range))
                                {
                                    Q.Cast(mob);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        //private static void OnGapcloser(AIHeroClient target, GapcloserArgs Args)
        //{
        //    if (MiscOption.GetBool("E", "AutoE").Enabled && E.IsReady() && target.IsValidTarget(E.Range))
        //    {
        //        if (E.IsReady() && target != null && target.IsValidTarget(E.Range))
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
        //                    {
        //                        var ePred = E.GetPrediction(target);
        //                        E.Cast(ePred.UnitPosition);
        //                    }
        //                    break;
        //            }
        //        }
        //    }
        //}
    }
}