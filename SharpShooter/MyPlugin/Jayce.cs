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

    // EMM TODO
    public class Jayce : MyLogic
    {
        private static float qCd, qCdEnd;
        private static float q1Cd, q1CdEnd;
        private static float wCd, wCdEnd;
        private static float w1Cd, w1CdEnd;
        private static float eCd, eCdEnd;
        private static float e1Cd, e1CdEnd;

        private static bool isMelee => !Me.HasBuff("jaycestancegun");
        private static bool isWActive => Me.Buffs.Any(buffs => buffs.Name.ToLower() == "jaycehypercharge");

        public Jayce()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 1050f);
            Q.SetSkillshot(0.25f, 79f, 1200f, true, true, SkillshotType.Line);

            Q2 = new Spell(SpellSlot.Q, 600f) { Speed = float.MaxValue, Delay = 0.25f };

            QE = new Spell(SpellSlot.Q, 1650f);
            QE.SetSkillshot(0.35f, 98f, 1900f, true, true, SkillshotType.Line);

            W = new Spell(SpellSlot.W);

            W2 = new Spell(SpellSlot.W, 350f);

            E = new Spell(SpellSlot.E, 650f);
            E.SetSkillshot(0.1f, 120f, float.MaxValue, false, false, SkillshotType.Circle);

            E2 = new Spell(SpellSlot.E, 240f) { Speed = float.MaxValue, Delay = 0.25f };

            R = new Spell(SpellSlot.R);

            ComboOption.AddMenu();
            ComboOption.AddBool("UsQECombo", "Use Cannon Q");
            ComboOption.AddBool("UseWCombo", "Use Cannon W");
            ComboOption.AddBool("UseECombo", "Use Cannon E");
            ComboOption.AddBool("UsQEComboHam", "Use Hammer Q");
            ComboOption.AddBool("UseWComboHam", "Use Hammer W");
            ComboOption.AddBool("UseEComboHam", "Use Hammer E");
            ComboOption.AddBool("UseRCombo", "Use R Switch");

            HarassOption.AddMenu();
            HarassOption.AddBool("UsQEHarass", "Use Cannon Q");
            HarassOption.AddBool("UseWHarass", "Use Cannon W");
            HarassOption.AddBool("UseEHarass", "Use Cannon E");
            HarassOption.AddBool("UsQEHarassHam", "Use Hammer Q", false);
            HarassOption.AddBool("UseWHarassHam", "Use Hammer W", false);
            HarassOption.AddBool("UseEHarassHam", "Use Hammer E", false);
            HarassOption.AddBool("UseRHarass", "Use R Switch");
            HarassOption.AddMana(60);
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddBool("UsQEFarm", "Use Cannon Q");
            LaneClearOption.AddBool("UseRFarm", "Use R Switch");
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddBool("UsQEJungle", "Use Cannon Q");
            JungleClearOption.AddBool("UseWJungle", "Use Cannon W");
            JungleClearOption.AddBool("UseEJungle", "Use Cannon E");
            JungleClearOption.AddBool("UsQEJungleHam", "Use Hammer Q");
            JungleClearOption.AddBool("UseWJungleHam", "Use Hammer W");
            JungleClearOption.AddBool("UseEJungleHam", "Use Hammer E");
            JungleClearOption.AddBool("UseRJungle", "Use R Switch");
            JungleClearOption.AddMana();

            FleeOption.AddMenu();
            FleeOption.AddQ();
            FleeOption.AddE();
            FleeOption.AddR();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddE();
            KillStealOption.AddBool("UsQEEKS", "Use QE");
            KillStealOption.AddR();

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddE();
            MiscOption.AddBool("E", "forceGate", "Auto E| After Q", false);
            MiscOption.AddSlider("E", "gatePlace", "Gate Place Distance", 50, 50, 110);
            MiscOption.AddSlider("E", "autoE", "Auto E Save|When Player HealthPercent < x%", 20, 0, 101);
            MiscOption.AddSetting("QE");
            MiscOption.AddKey("QE", "SemiQE", "Semi-manual QE Key", Keys.T, KeyBindType.Press);
            MiscOption.AddList("QE", "SemiQEMode", "Semi-manual QE Mode", new[] { "To Target", "To Mouse" });

            DrawOption.AddMenu();
            DrawOption.AddRange(Q, "Cannon Q");
            DrawOption.AddRange(QE, "Cannon Q Extend");
            DrawOption.AddRange(W, "Cannon W");
            DrawOption.AddRange(E, "Cannon E");
            DrawOption.AddRange(Q, "Hammer Q");
            DrawOption.AddRange(W, "Hammer W");
            DrawOption.AddRange(E, "Hammer E");
            DrawOption.AddDamageIndicatorToHero(true, true, true, false, false);
            DrawOption.AddBool("DrawCoolDown", "Draw Spell CoolDown");

            Tick.OnTick += OnUpdate;
            Orbwalker.OnAction += OnAction;
            GameObject.OnCreate += (sender, args) => OnCreate(sender);
            //Gapcloser.OnGapcloser += OnGapcloser;
            Drawing.OnDraw += args => OnRender();
        }

        private static void OnUpdate(EventArgs args)
        {
            CalculateCooldown();

            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Me.IsWindingUp)
            {
                return;
            }

            if (MiscOption.GetBool("QE", "SemiQE").Enabled)
            {
                SemiQELogic();
            }

            if (FleeOption.isFleeKeyActive)
            {
                Flee();
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

        private static void CalculateCooldown()
        {
            if (!isMelee)
            {
                qCdEnd = Me.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires;
                wCdEnd = Me.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;
                eCdEnd = Me.Spellbook.GetSpell(SpellSlot.E).CooldownExpires;
            }
            else
            {
                q1CdEnd = Me.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires;
                w1CdEnd = Me.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;
                e1CdEnd = Me.Spellbook.GetSpell(SpellSlot.E).CooldownExpires;
            }

            qCd = Me.Spellbook.GetSpell(SpellSlot.Q).Level > 0 ? CheckCD(qCdEnd) : -1;
            wCd = Me.Spellbook.GetSpell(SpellSlot.W).Level > 0 ? CheckCD(wCdEnd) : -1;
            eCd = Me.Spellbook.GetSpell(SpellSlot.E).Level > 0 ? CheckCD(eCdEnd) : -1;
            q1Cd = Me.Spellbook.GetSpell(SpellSlot.Q).Level > 0 ? CheckCD(q1CdEnd) : -1;
            w1Cd = Me.Spellbook.GetSpell(SpellSlot.W).Level > 0 ? CheckCD(w1CdEnd) : -1;
            e1Cd = Me.Spellbook.GetSpell(SpellSlot.E).Level > 0 ? CheckCD(e1CdEnd) : -1;
        }

        private static float CheckCD(float Expires)
        {
            var time = Expires - Game.Time;

            if (time < 0)
            {
                time = 0;

                return time;
            }

            return time;
        }

        private static void SemiQELogic()
        {

        }

        private static void Flee()
        {

        }

        private static void KillSteal()
        {

        }

        private static void Combo()
        {

        }

        private static void Harass()
        {

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

        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                
            }
        }

        private static void OnAction(object sender, OrbwalkerActionArgs Args)
        {
            if (Args.Type != OrbwalkerType.AfterAttack)
            {
                return;
            }

            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0 ||
                wCd != 0 || W.Level == 0 || !W.IsReady() || isWActive)
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.AIHeroClient:
                    {
                        var target = Args.Target as AIHeroClient;

                        if (target != null && target.IsValidTarget())
                        {
                            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                            {
                                if (ComboOption.GetBool("UseWCombo").Enabled)
                                {
                                    if (target.InAutoAttackRange())
                                    {
                                        Orbwalker.ResetAutoAttackTimer();
                                        W.Cast();
                                    }
                                }
                            }
                            else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass ||
                                     Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && MyManaManager.SpellHarass)
                            {
                                if (HarassOption.HasEnouguMana() &&
                                    HarassOption.GetHarassTargetEnabled(target.CharacterName) &&
                                    HarassOption.GetBool("UseWHarass").Enabled)
                                {
                                    if (target.InAutoAttackRange())
                                    {
                                        Orbwalker.ResetAutoAttackTimer();
                                        W.Cast();
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void OnCreate(GameObject sender)
        {
            if (sender == null || sender.Type != GameObjectType.MissileClient ||
                !MiscOption.GetBool("E", "forceGate").Enabled || eCd != 0 || !E.IsReady())
            {
                return;
            }

            var missile = sender as MissileClient;

            if (missile != null && missile.SpellCaster.IsMe &&
                string.Equals(missile.SData.Name, "jayceshockblastmis", StringComparison.CurrentCultureIgnoreCase))
            {
                var vec = missile.PreviousPosition - Vector3.Normalize(Me.PreviousPosition - missile.PreviousPosition) * 100;

                E.Cast(vec);
            }
        }

        //private static void OnGapcloser(AIHeroClient target, GapcloserArgs Args)
        //{
        //    if (E.IsReady() && target != null && target.IsValidTarget(E2.Range) && !Args.HaveShield)
        //    {
        //        switch (Args.Type)
        //        {
        //            case SpellType.Dash:
        //            case SpellType.SkillShot:
        //            case SpellType.Targeted:
        //                {
        //                    if (Args.EndPosition.DistanceToPlayer() <= target.BoundingRadius + Me.BoundingRadius)
        //                    {
        //                        if (!isMelee)
        //                        {
        //                            R.Cast();
        //                        }

        //                        if (isMelee)
        //                        {
        //                            E.CastOnUnit(target);
        //                        }
        //                    }
        //                }
        //                break;
        //        }
        //    }
        //}

        private static void OnRender()
        {
            if (DrawOption.GetBool("DrawCoolDown").Enabled)
            {
                string msg;
                var QCoolDown = (int)qCd == -1 ? 0 : (int)qCd;
                var WCoolDown = (int)wCd == -1 ? 0 : (int)wCd;
                var ECoolDown = (int)eCd == -1 ? 0 : (int)eCd;
                var Q1CoolDown = (int)q1Cd == -1 ? 0 : (int)q1Cd;
                var W1CoolDown = (int)w1Cd == -1 ? 0 : (int)w1Cd;
                var E1CoolDown = (int)e1Cd == -1 ? 0 : (int)e1Cd;

                if (isMelee)
                {
                    msg = "Q: " + QCoolDown + "   W: " + WCoolDown + "   E: " + ECoolDown;
                    Drawing.DrawText(Me.HPBarPosition.X + 30, Me.HPBarPosition.Y - 30, Color.Orange, msg);
                }
                else
                {
                    msg = "Q: " + Q1CoolDown + "   W: " + W1CoolDown + "   E: " + E1CoolDown;
                    Drawing.DrawText(Me.HPBarPosition.X + 30, Me.HPBarPosition.Y - 30, Color.SkyBlue, msg);
                }
            }
        }

        private static void CastQCannon(AIHeroClient target, bool useE)
        {
            var qePred = QE.GetPrediction(target);

            if (qePred.Hitchance >= HitChance.High && qCd == 0 && eCd == 0 && useE)
            {
                var gateVector = Me.Position + 
                    Vector3.Normalize(target.PreviousPosition - Me.Position) * MiscOption.GetSlider("E", "gatePlace").Value;

                if (Me.Distance(qePred.CastPosition) < QE.Range + 100)
                {
                    if (E.IsReady() && QE.IsReady())
                    {
                        E.Cast(gateVector);
                        QE.Cast(qePred.CastPosition);
                        return;
                    }
                }
            }

            var qPred = Q.GetPrediction(target);

            if ((!useE || !E.IsReady()) && qCd == 0 && qPred.Hitchance >= HitChance.High &&
                Me.Distance(target.PreviousPosition) <= Q.Range && Q.IsReady() && eCd != 0)
            {
                Q.Cast(target);
            }
        }

        private static void CastQCannonMouse()
        {
            Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (isMelee && !R.IsReady())
            {
                return;
            }

            if (isMelee && R.IsReady())
            {
                R.Cast();
                return;
            }

            if (eCd == 0 && qCd == 0 && !isMelee)
            {
                if (MiscOption.GetList("QE", "SemiQEMode").Index == 1)
                {
                    var gateDis = MiscOption.GetSlider("E", "gatePlace").Value;
                    var gateVector = Me.PreviousPosition + Vector3.Normalize(Game.CursorPos - Me.PreviousPosition) * gateDis;

                    if (E.IsReady() && QE.IsReady())
                    {
                        E.Cast(gateVector);
                        QE.Cast(Game.CursorPos);
                    }
                }
                else
                {
                    var qTarget = MyTargetSelector.GetTarget(QE.Range);

                    if (qTarget != null && qTarget.IsValidTarget(QE.Range) && qCd == 0)
                    {
                        CastQCannon(qTarget, true);
                    }
                }
            }
        }

        private static bool ECheck(AIHeroClient target, bool usQE, bool useW)
        {
            if (GetEDamage(target) >= target.Health)
            {
                return true;
            }

            if ((qCd == 0 && usQE || wCd == 0 && useW) && q1Cd != 0 && w1Cd != 0)
            {
                return true;
            }

            if (WallStun(target))
            {
                return true;
            }

            if (Me.HealthPercent <= MiscOption.GetSlider("E", "autoE").Value)
            {
                return true;
            }

            return false;
        }

        private static void SwitchFormCheck(AIHeroClient target, bool usQE, bool useW, bool usQE2, bool useW2, bool useE2)
        {
            if (target == null)
                return;

            if (target.Health > 80)
            {
                if (target.Distance(Me) > 650 && R.IsReady() && qCd == 0 && wCd == 0 && eCd == 0 && isMelee)
                {
                    R.Cast();
                    return;
                }

                if ((qCd != 0 || !usQE) && (wCd != 0 && (!isWActive || !useW)) && R.IsReady() && HammerAllReady() &&
                    !isMelee && Me.Distance(target.PreviousPosition) < 650 && (usQE2 || useW2 || useE2))
                {
                    R.Cast();
                    return;
                }
            }

            if (!isMelee && target.Distance(Me) <= Q2.Range + 150 &&
                target.Health <= GetEDamage(target, true) + GetQDamage(target, true) + Me.GetAutoAttackDamage(target) &&
                q1Cd == 0 && e1Cd == 0)
            {
                R.Cast();
                return;
            }

            if ((qCd == 0 && usQE || wCd == 0 && useW && R.IsReady()) && isMelee)
            {
                R.Cast();
                return;
            }

            if (q1Cd != 0 && w1Cd != 0 && e1Cd != 0 && isMelee && R.IsReady())
            {
                R.Cast();
            }
        }

        private static bool HammerAllReady()
        {
            return q1Cd == 0 && w1Cd == 0 && e1Cd == 0;
        }

        private static bool WallStun(AIHeroClient target)
        {
            if (target == null)
                return false;

            var pred = E.GetPrediction(target);
            var pushedPos = pred.UnitPosition + Vector3.Normalize(pred.UnitPosition - Me.PreviousPosition) * 350;

            return IsPassWall(target.PreviousPosition, pushedPos);
        }

        private static bool IsPassWall(Vector3 start, Vector3 end)
        {
            double count = Vector3.Distance(start, end);

            for (uint i = 0; i <= count; i += 25)
            {
                var pos = start.ToVector2().Extend(Me.PreviousPosition.ToVector2(), -i);

                if (pos.IsWall())
                {
                    return true;
                }
            }

            return false;
        }

        public static double GetQDamage(AIBaseClient target, bool getmeleeDMG = false, bool getcannonDMG = false)
        {
            var level = Q.Level - 1;

            var meleeDMG = new double[] { 35, 70, 105, 140, 175, 210 }[level] + 1 * Me.FlatPhysicalDamageMod;
            var cannonDMG = new double[] { 70, 120, 170, 220, 270, 320 }[level] + 1.2 * Me.FlatPhysicalDamageMod;

            if (getmeleeDMG)
            {
                return Me.CalculateDamage(target, DamageType.Physical, meleeDMG);
            }

            if (getcannonDMG)
            {
                return Me.CalculateDamage(target, DamageType.Physical, cannonDMG);
            }

            return Me.CalculateDamage(target, DamageType.Physical, isMelee ? meleeDMG : cannonDMG);
        }

        public static double GetWDamage(AIBaseClient target, bool ignoreCheck = false)
        {
            if (!isMelee || !ignoreCheck)
            {
                return 0;
            }

            var level = W.Level - 1;

            var meleeDMG = new double[] { 100, 160, 220, 280, 340, 400 }[level] + 1 * Me.FlatPhysicalDamageMod;

            return Me.CalculateDamage(target, DamageType.Magical, meleeDMG);
        }

        public static double GetEDamage(AIBaseClient target, bool ignoreCheck = false)
        {
            if (!isMelee || !ignoreCheck)
            {
                return 0;
            }

            var level = E.Level - 1;

            var meleeDMG = new[] { 0.08, 0.104, 0.128, 0.152, 0.176, 0.20 }[level] * target.MaxHealth + 1 * Me.FlatPhysicalDamageMod;
            var mobDMG = new double[] { 200, 300, 400, 500, 600 }[level];

            if (target.Type == GameObjectType.AIHeroClient)
            {
                if (meleeDMG > mobDMG)
                {
                    return mobDMG;
                }
            }

            return Me.CalculateDamage(target, DamageType.Magical, meleeDMG);
        }
    }
}
