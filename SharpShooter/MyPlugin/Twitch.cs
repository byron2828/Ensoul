namespace SharpShooter.MyPlugin
{
    #region

    using System;
    using System.Linq;

    using EnsoulSharp;
    using EnsoulSharp.SDK;
    using EnsoulSharp.SDK.Prediction;
    using EnsoulSharp.SDK.Events;
    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    public class Twitch : MyLogic
    {
        public Twitch()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q);

            W = new Spell(SpellSlot.W, 950f);
            W.SetSkillshot(0.25f, 100f, 1400f, false, false, SkillshotType.Circle);

            E = new Spell(SpellSlot.E, 1200f);

            R = new Spell(SpellSlot.R, 975f);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddSlider("ComboQCount", "Use Q| Enemies Count >= x", 3, 1, 5);
            ComboOption.AddSlider("ComboQRange", "Use Q| Search Enemies Range", 600, 0, 1800);
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddBool("ComboEKill", "Use E| When Target Can KillAble");
            ComboOption.AddBool("ComboEFull", "Use E| When Target have Full Stack", false);
            ComboOption.AddR();
            ComboOption.AddBool("ComboRKillSteal", "Use R| When Target Can KillAble");
            ComboOption.AddSlider("ComboRCount", "Use R| Enemies Count >= x", 3, 1, 5);

            HarassOption.AddMenu();
            HarassOption.AddW();
            HarassOption.AddE();
            HarassOption.AddBool("HarassEStack", "Use E| When Target will Leave E Range");
            HarassOption.AddSlider("HarassEStackCount", "Use E(Leave)| Min Stack Count >= x", 3, 1, 6);
            HarassOption.AddBool("HarassEFull", "Use E| When Target have Full Stack");
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min KillAble Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddE();

            MiscOption.AddMenu();
            MiscOption.AddBasic();

            DrawOption.AddMenu();
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(false, false, true, false, false);

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

        private static void KillSteal()
        {
            if (KillStealOption.UseE && E.IsReady())
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(E.Range) && !x.IsUnKillable()))
                {
                    if (target.IsValidTarget(E.Range) && target.Health < GetRealEDamage(target) - target.HPRegenRate)
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(E.Range);

            if (target.IsValidTarget(E.Range))
            {
                if (ComboOption.UseR && R.IsReady())
                {
                    if (ComboOption.GetBool("ComboRKillSteal").Enabled &&
                        GameObjects.EnemyHeroes.Count(x => x.DistanceToPlayer() <= R.Range) <= 2 &&
                        target.Health <= Me.GetAutoAttackDamage(target) * 4 + GetRealEDamage(target) * 2)
                    {
                        R.Cast();
                    }

                    if (GameObjects.EnemyHeroes.Count(x => x.DistanceToPlayer() <= R.Range) >= ComboOption.GetSlider("ComboRCount").Value)
                    {
                        R.Cast();
                    }
                }

                if (ComboOption.UseQ && Q.IsReady() &&
                    GameObjects.EnemyHeroes.Count(x => x.DistanceToPlayer() <= ComboOption.GetSlider("ComboQRange").Value) >=
                    ComboOption.GetSlider("ComboQCount").Value)
                {
                    Q.Cast();
                }

                if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range) &&
                    target.Health > GetRealEDamage(target) && GetEStackCount(target) < 6 &&
                    Me.Mana > Q.Mana + W.Mana + E.Mana + R.Mana)
                {
                    var wPred = W.GetPrediction(target);

                    if (wPred.Hitchance >= HitChance.High)
                    {
                        W.Cast(wPred.CastPosition);
                    }
                }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range) &&
                    target.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom"))
                {
                    if (ComboOption.GetBool("ComboEFull").Enabled && GetEStackCount(target) >= 6)
                    {
                        E.Cast();
                    }

                    if (ComboOption.GetBool("ComboEKill").Enabled && target.Health <= GetRealEDamage(target) &&
                        target.IsValidTarget(E.Range))
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseW && W.IsReady())
                {
                    var target = HarassOption.GetTarget(W.Range);

                    if (target.IsValidTarget(W.Range))
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.Hitchance >= HitChance.High)
                        {
                            W.Cast(wPred.CastPosition);
                        }
                    }
                }

                if (HarassOption.UseE && E.IsReady())
                {
                    var target = HarassOption.GetTarget(E.Range);

                    if (target.IsValidTarget(E.Range))
                    {
                        if (HarassOption.GetBool("HarassEStack").Enabled)
                        {
                            if (target.DistanceToPlayer() > E.Range * 0.8 && target.IsValidTarget(E.Range) &&
                                GetEStackCount(target) >= HarassOption.GetSlider("HarassEStackCount").Value)
                            {
                                E.Cast();
                            }
                        }

                        if (HarassOption.GetBool("HarassEFull").Enabled && GetEStackCount(target) >= 6)
                        {
                            E.Cast();
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
                if (LaneClearOption.UseE && E.IsReady())
                {
                    var eKillMinionsCount =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                            .Count(
                                x =>
                                    x.DistanceToPlayer() <= E.Range && x.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom") &&
                                    x.Health < GetRealEDamage(x));

                    if (eKillMinionsCount >= LaneClearOption.GetSlider("LaneClearECount").Value)
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseE && E.IsReady())
                {
                    var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range) && x.GetJungleType() != JungleType.Unknown).ToList();

                    foreach (
                        var mob in
                        mobs.Where(
                            x =>
                                !x.Name.ToLower().Contains("mini") && x.DistanceToPlayer() <= E.Range &&
                               x.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom")))
                    {
                        if (mob.Health < GetRealEDamage(mob) && mob.IsValidTarget(E.Range))
                        {
                            E.Cast();
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
                                var wPred = W.GetPrediction(target);
                                if (wPred.Hitchance >= HitChance.High)
                                {
                                    W.Cast(wPred.UnitPosition);
                                }
                            }
                        }
                    }
                }
                    break;
            }
        }

        private static double GetRealEDamage(AIBaseClient target)
        {
            if (target != null && !target.IsDead && target.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom"))
            {
                if (target.HasBuff("KindredRNoDeathBuff"))
                {
                    return 0;
                }

                if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("JudicatorIntervention"))
                {
                    return 0;
                }

                if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("FioraW"))
                {
                    return 0;
                }

                if (target.HasBuff("ShroudofDarkness"))
                {
                    return 0;
                }

                if (target.HasBuff("SivirShield"))
                {
                    return 0;
                }

                var damage = 0d;

                damage += E.IsReady() ? GetEDMGTwitch(target) : 0d;

                if (target.CharacterName == "Morderkaiser")
                {
                    damage -= target.Mana;
                }

                if (Me.HasBuff("SummonerExhaust"))
                {
                    damage = damage * 0.6f;
                }

                if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
                {
                    damage -= target.Mana / 2f;
                }

                if (target.HasBuff("GarenW"))
                {
                    damage = damage * 0.7f;
                }

                if (target.HasBuff("ferocioushowl"))
                {
                    damage = damage * 0.7f;
                }

                return damage;
            }

            return 0d;
        }

        public static double GetEDMGTwitch(AIBaseClient target)
        {
            if (target == null || !target.IsValidTarget())
            {
                return 0;
            }

            if (!target.HasBuff("twitchdeadlyvenom"))
            {
                return 0;
            }

            var eLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level;
            if (eLevel <= 0)
            {
                return 0;
            }

            var buffCount = GetEStackCount(target);

            var baseDamage = new[] { 0, 20, 30, 40, 50, 60 }[eLevel];
            var extraDamage = new[] { 0, 15, 20, 25, 30, 35 }[eLevel] + 0.2f * ObjectManager.Player.TotalMagicalDamage +
                              0.35f * (ObjectManager.Player.TotalAttackDamage - ObjectManager.Player.BaseAttackDamage);
            var resultDamage =
                ObjectManager.Player.CalculateDamage(target, DamageType.Physical, baseDamage + extraDamage * buffCount);
            if (ObjectManager.Player.HasBuff("SummonerExhaust"))
            {
                resultDamage *= 0.6f;
            }

            return resultDamage;
        }

        public static int GetEStackCount(AIBaseClient target)
        {
            if (target == null || target.IsDead ||
                !target.IsValidTarget() ||
                target.Type != GameObjectType.AIMinionClient && target.Type != GameObjectType.AIHeroClient)
            {
                return 0;
            }

            return target.GetBuffCount("twitchdeadlyvenom");
        }
    }
}