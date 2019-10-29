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

    public class TwistedFate : MyLogic
    {
        public TwistedFate()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 1450f);
            Q.SetSkillshot(0.25f, 40f, 1000f, false, false, SkillshotType.Line);

            W = new Spell(SpellSlot.W, 850f);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R, 5500f);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboSaveMana", "Use Q| Save Mana To Cast W");
            ComboOption.AddW();
            ComboOption.AddList("ComboWSmartKS", "Use W| Smart Card KillAble", new[] { "First Card", "Blue Card", "Off" });
            ComboOption.AddBool("ComboDisableAA", "Auto Disable Attack| When Selecting");

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q|Min Hit Count >= x", 4, 1, 10);
            LaneClearOption.AddW();
            LaneClearOption.AddBool("LaneClearWBlue", "Use W| Blue Card");
            LaneClearOption.AddBool("LaneClearWRed", "Use W| Red Card");
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();

            //GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddSubMenu("CardSelect", "Card Select Settings");
            MiscOption.AddKey("CardSelect", "CardSelectYellow", "Gold Card", Keys.W, KeyBindType.Press);
            MiscOption.AddKey("CardSelect", "CardSelectBlue", "Blue Card", Keys.E, KeyBindType.Press);
            MiscOption.AddKey("CardSelect", "CardSelectRed", "Red Card", Keys.T, KeyBindType.Press);
            MiscOption.AddBool("CardSelect", "HumanizerSelect", "Humanizer Select Card", false);
            MiscOption.AddSlider("CardSelect", "HumanizerSelectMin", "Humanizer Select Card Min Delay", 0, 0, 2000);
            MiscOption.AddSlider("CardSelect", "HumanizerSelectMax", "Humanizer Select Card Max Delay", 2000, 0, 3500);
            MiscOption.AddQ();
            MiscOption.AddBool("Q", "AutoQImmobile", "Auto Q|Enemy Cant Movement");
            MiscOption.AddKey("Q", "SemiQ", "Semi-manual Q Key", Keys.Q, KeyBindType.Press);
            MiscOption.AddR();
            MiscOption.AddBool("R", "UltYellow", "Auto Gold Card| In Ult");

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(true, true, true, false, true);

            Tick.OnTick += OnUpdate;
            //Gapcloser.OnGapcloser += OnGapcloser;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalker.OnAction += OnAction;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Variables.GameTimeTickCount - HumanizerCardSelect.LastWSent > 3000)
            {
                HumanizerCardSelect.Select = HumanizerCards.None;
            }

            if (Variables.GameTimeTickCount - LastForcusTime > Me.AttackCastDelay * 1000f)
            {
                if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                {
                    Orbwalker.ForceTarget = null;
                }
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
                    Farm();
                    break;
            }
        }

        private static void Auto()
        {
            if (Q.IsReady())
            {
                if (MiscOption.GetKey("Q", "SemiQ").Active)
                {
                    var target = MyTargetSelector.GetTarget(Q.Range);

                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }

                if (MiscOption.GetBool("Q", "AutoQImmobile").Enabled)
                {
                    var target = GameObjects.EnemyHeroes.FirstOrDefault(x => x.IsValidTarget(Q.Range) && !x.CanMoveMent());

                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        Q.Cast(target.PreviousPosition);
                    }
                }
            }

            if (W.IsReady())
            {
                if (MiscOption.GetKey("CardSelect", "CardSelectYellow").Active)
                {
                    HumanizerCardSelect.Select = HumanizerCards.Yellow;
                    HumanizerCardSelect.StartSelecting(HumanizerCards.Yellow);
                }

                if (MiscOption.GetKey("CardSelect", "CardSelectBlue").Active)
                {
                    HumanizerCardSelect.Select = HumanizerCards.Blue;
                    HumanizerCardSelect.StartSelecting(HumanizerCards.Blue);
                }

                if (MiscOption.GetKey("CardSelect", "CardSelectRed").Active)
                {
                    HumanizerCardSelect.Select = HumanizerCards.Red;
                    HumanizerCardSelect.StartSelecting(HumanizerCards.Red);
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range)))
                {
                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        if (Me.GetSpellDamage(target, SpellSlot.Q) >= target.Health + 40 && !target.IsUnKillable())
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
        }

        private static void Combo()
        {
            if (ComboOption.UseQ && Q.IsReady())
            {
                var target = MyTargetSelector.GetTarget(Q.Range);

                if (target != null && target.IsValidTarget(Q.Range))
                {
                    if (ComboOption.GetBool("ComboSaveMana").Enabled)
                    {
                        if (Me.Mana >= W.Mana + Q.Mana)
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.Hitchance >= HitChance.High)
                            {
                                Q.Cast(qPred.CastPosition);
                            }
                        }
                    }
                    else
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }
            }

            if (ComboOption.UseW && W.IsReady())
            {
                var target = MyTargetSelector.GetTarget(W.Range);

                if (target != null && target.IsValidTarget())
                {
                    if (ComboOption.GetList("ComboWSmartKS").Index != 2 &&
                        target.Health <= Me.GetSpellDamage(target, SpellSlot.W) + Me.GetAutoAttackDamage(target) &&
                        target.IsValidTarget(Me.GetRealAutoAttackRange(target) + 80))
                    {
                        if (ComboOption.GetList("ComboWSmartKS").Index == 0)
                        {
                            W.Cast();
                            W.Cast();
                            W.Cast();

                            if (HumanizerCardSelect.IsSelect && target.InAutoAttackRange() && Orbwalker.CanAttack())
                            {
                                Me.IssueOrder(GameObjectOrder.AttackUnit, target);
                            }
                        }
                        else
                        {
                            HumanizerCardSelect.StartSelecting(HumanizerCards.Blue);

                            if (HumanizerCardSelect.IsSelect && target.InAutoAttackRange() && Orbwalker.CanAttack())
                            {
                                Me.IssueOrder(GameObjectOrder.AttackUnit, target);
                            }
                        }
                    }
                    else
                    {
                        HumanizerCardSelect.StartSelecting(Me.Mana + W.Mana >=
                                                           Q.Mana + W.Mana
                            ? HumanizerCards.Yellow
                            : HumanizerCards.Blue);
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
                    var target = HarassOption.GetTarget(Q.Range);

                    if (target != null && target.IsValidTarget(Q.Range))
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
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToList();

                    if (minions.Any())
                    {
                        var qFarm = Q.GetLineFarmLocation(minions, 60);

                        if (qFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearQCount").Value)
                        {
                            Q.Cast(qFarm.Position);
                        }
                    }
                }

                if (LaneClearOption.UseW && W.IsReady())
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(
                            x => x.IsValidTarget(Me.AttackRange + Me.BoundingRadius + 80) && x.IsMinion()).ToList();

                    if (minions.Any())
                    {
                        var wFarm = FarmPrediction.GetBestCircularFarmLocation(minions.Select(x => x.Position.ToVector2()).ToList(),
                            Me.AttackRange + Me.BoundingRadius + 80, 280);

                        if (LaneClearOption.GetBool("LaneClearWRed").Enabled && wFarm.MinionsHit >= 3)
                        {
                            var min = minions.FirstOrDefault(x => x.Distance(wFarm.Position) <= 80);

                            if (min != null)
                            {
                                HumanizerCardSelect.StartSelecting(HumanizerCards.Red);

                                Orbwalker.ForceTarget = min;
                                LastForcusTime = Variables.GameTimeTickCount;
                            }
                        }
                        else if (LaneClearOption.GetBool("LaneClearWBlue").Enabled)
                        {
                            var min = minions.FirstOrDefault(x => x.Health < Me.GetSpellDamage(x, SpellSlot.W) + Me.GetAutoAttackDamage(x));

                            if (min != null && min.InAutoAttackRange())
                            {
                                HumanizerCardSelect.StartSelecting(HumanizerCards.Blue);

                                Orbwalker.ForceTarget = min;
                                LastForcusTime = Variables.GameTimeTickCount;
                            }
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
                        var qFarm = Q.GetLineFarmLocation(mobs, 60);

                        if (qFarm.MinionsHit >= 2 || mobs.Any(x => x.GetJungleType() != JungleType.Small) && qFarm.MinionsHit >= 1)
                        {
                            Q.Cast(qFarm.Position);
                        }
                    }
                }

                if (JungleClearOption.UseW && W.IsReady())
                {
                    var Mob = GameObjects.Jungle.Where(x => x.IsValidTarget(Me.AttackRange + Me.BoundingRadius + 80) && x.GetJungleType() != JungleType.Unknown).ToList();

                    if (Mob.Count > 0)
                    {
                        if (Me.Mana >= W.Mana * 2 + Q.Mana * 2)
                        {
                            HumanizerCardSelect.StartSelecting(Mob.Count >= 2
                                ? HumanizerCards.Red
                                : HumanizerCards.Blue);
                        }
                    }
                }
            }
        }

        private static void OnAction(object sender, OrbwalkerActionArgs Args)
        {
            if (Args.Type != OrbwalkerType.BeforeAttack)
            {
                return;
            }

            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0)
            {
                return;
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo &&
                ComboOption.GetBool("ComboDisableAA").Enabled &&
                Args.Target.Type == GameObjectType.AIHeroClient)
            {
                if (HumanizerCardSelect.Status == HumanizerSelectStatus.Selecting &&
                    Variables.GameTimeTickCount - HumanizerCardSelect.LastWSent > 300)
                {
                    Args.Process = false;
                }
            }
        }

        //private static void OnGapcloser(AIHeroClient target, GapcloserArgs Args)
        //{
        //    if (target != null && target.IsValidTarget() && W.IsReady())
        //    {
        //        if (W.IsReady() && target.IsValidTarget(W.Range))
        //        {
        //            switch (Args.Type)
        //            {
        //                case SpellType.Melee:
        //                    if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100) && !Args.HaveShield)
        //                    {
        //                        HumanizerCardSelect.StartSelecting(HumanizerCards.Yellow);

        //                        if (Me.HasBuff("goldcardpreattack") && target.InAutoAttackRange())
        //                        {
        //                            Me.IssueOrder(GameObjectOrder.AttackUnit, target);
        //                        }
        //                    }
        //                    break;
        //                case SpellType.Dash:
        //                case SpellType.SkillShot:
        //                case SpellType.Targeted:
        //                    {
        //                        if (target.InAutoAttackRange() && !Args.HaveShield)
        //                        {
        //                            HumanizerCardSelect.StartSelecting(HumanizerCards.Yellow);

        //                            if (Me.HasBuff("goldcardpreattack") && target.InAutoAttackRange())
        //                            {
        //                                Me.IssueOrder(GameObjectOrder.AttackUnit, target);
        //                            }
        //                        }
        //                    }
        //                    break;
        //            }
        //        }
        //    }
        //}

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Slot == SpellSlot.R && args.SData.Name.Equals("Gate", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (MiscOption.GetBool("R", "UltYellow").Enabled)
                    {
                        if (W.IsReady())
                        {
                            HumanizerCardSelect.StartSelecting(HumanizerCards.Yellow);
                        }
                    }
                }
            }
        }
    }

    public enum HumanizerSelectStatus
    {
        Ready = 0,
        Selecting = 1,
        Selected = 2,
        Cooldown = 3
    }

    public enum HumanizerCards
    {
        Red = 0,
        Yellow = 1,
        Blue = 2,
        None = 3
    }

    public static class HumanizerCardSelect
    {
        public static HumanizerCards Select;
        public static int LastWSent;
        public static Random random = new Random(Variables.GameTimeTickCount);

        public static HumanizerSelectStatus Status { get; set; }

        static HumanizerCardSelect()
        {
            Tick.OnTick += OnUpdate;
        }

        public static void StartSelecting(HumanizerCards card)
        {
            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "PickACard" && Status == HumanizerSelectStatus.Ready)
            {
                Select = card;

                if (Variables.GameTimeTickCount - LastWSent > 170 + Game.Ping / 2)
                {
                    MyLogic.W.Cast();
                    LastWSent = Variables.GameTimeTickCount;
                }
            }
        }

        public static bool IsSelect => ObjectManager.Player.HasBuff("GoldCardPreAttack") ||
            ObjectManager.Player.HasBuff("BlueCardPreAttack") ||
            ObjectManager.Player.HasBuff("RedCardPreAttack");

        private static void OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling())
            {
                return;
            }

            var wName = MyLogic.W.Name.ToLower();
            var wState = MyLogic.W.State;

            if (wName != "pickacard" && ObjectManager.Player.HasBuff("pickacard_tracker") && Variables.GameTimeTickCount - LastWSent > 0)
            {
                if (MiscOption.GetBool("CardSelect", "HumanizerSelect").Enabled &&
                    Variables.GameTimeTickCount - LastWSent <=
                    random.Next(MiscOption.GetSlider("CardSelect", "HumanizerSelectMin").Value,
                        MiscOption.GetSlider("CardSelect", "HumanizerSelectMax").Value))
                {
                    return;
                }

                if (Select == HumanizerCards.Blue &&
                    wName.Equals("BlueCardLock", StringComparison.InvariantCultureIgnoreCase))
                {
                    MyLogic.W.Cast();
                }
                else if (Select == HumanizerCards.Yellow &&
                    wName.Equals("GoldCardLock", StringComparison.InvariantCultureIgnoreCase))
                {
                    MyLogic.W.Cast();
                }
                else if (Select == HumanizerCards.Red && wName.
                    Equals("RedCardLock", StringComparison.InvariantCultureIgnoreCase))
                {
                    MyLogic.W.Cast();
                }
            }
            else
            {
                if (wState == SpellState.Ready)
                {
                    Status = HumanizerSelectStatus.Ready;
                }
                else if ((wState == SpellState.Cooldown || wState == SpellState.Disabled ||
                          wState == SpellState.NoMana || wState == SpellState.NotLearned ||
                          wState == SpellState.CooldownOrSealed || wState == SpellState.Unknown)
                         && !IsSelect)
                {
                    Status = HumanizerSelectStatus.Cooldown;
                }
                else if (IsSelect)
                {
                    Status = HumanizerSelectStatus.Selected;
                }
                else
                {
                    Status = HumanizerSelectStatus.Selecting;
                }
            }
        }
    }
}