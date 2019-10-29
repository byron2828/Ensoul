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

    public class Kalista : MyLogic
    {
        private static int lastWTime, lastETime;

        public Kalista()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Spell(SpellSlot.Q, 1150f);
            Q.SetSkillshot(0.35f, 40f, 2400f, true, false, SkillshotType.Line);

            W = new Spell(SpellSlot.W, 5000f);

            E = new Spell(SpellSlot.E, 950f);

            R = new Spell(SpellSlot.R, 1500f);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddE();
            ComboOption.AddBool("ComboESlow", "Use E| When Enemy Have Buff and Minion Can KillAble");
            ComboOption.AddBool("ComboGapcloser", "Auto Attack Minion To Gapcloser Target");

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddBool("HarassESlow", "Use E| When Enemy Have Buff and Minion Can KillAble");
            HarassOption.AddSliderBool("HarassELeave", "Use E| When Enemy Will Leave E Range And Buff Count >= x", 3, 1, 10);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddSliderBool("LaneClearE", "Use E| Min KillAble Count >= x", 3, 1, 5, true);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddMenu();
            LastHitOption.AddE();
            LastHitOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddE();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddE();
            MiscOption.AddBool("E", "AutoESteal", "Auto E Steal Mob (Only Buff&Dragon&Baron)");
            MiscOption.AddSliderBool("E", "EToler", "Enabled E Toler DMG", 0, -100, 110, true);
            MiscOption.AddR();
            MiscOption.AddSliderBool("R", "AutoRAlly", "Auto R| My Allies HealthPercent <= x%", 30, 1, 99, true);
            MiscOption.AddBool("R", "Balista", "Auto Balista");
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcus", "ForcusAttack", "Forcus Attack Passive Target");

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddDamageIndicatorToHero(false, false, true, false, false);

            Tick.OnTick += OnUpdate;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalker.OnAction += OnAction;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Variables.GameTimeTickCount - LastForcusTime > Me.AttackCastDelay * 1000f)
            {
                if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                {
                    Orbwalker.ForceTarget = null;
                }
            }

            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Me.IsWindingUp)
            {
                return;
            }

            KillStealEvent();
            AutoUltEvent();
            AutoEStealEvent();

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
                case OrbwalkerMode.LastHit:
                    LastHitEvent();
                    break;
            }
        }

        private static void KillStealEvent()
        {
            if (KillStealOption.UseE && E.IsReady())
            {
                if (
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(E.Range) &&
                            x.Health <
                            E.GetKalistaRealDamage(x,
                                MiscOption.GetSliderBool("E", "EToler").Enabled,
                                MiscOption.GetSliderBool("E", "EToler").Value) &&
                            !x.IsUnKillable()).Any(target => target.IsValidTarget(E.Range)))
                {
                    E.Cast();
                }
            }

            if (KillStealOption.UseQ && Q.IsReady() && Variables.GameTimeTickCount - lastETime > 1000)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q) && !x.IsUnKillable()))
                {
                    if (target.IsValidTarget(Q.Range))
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

        private static void AutoUltEvent()
        {
            if (Me.Spellbook.GetSpell(SpellSlot.R).Level > 0 && R.IsReady())
            {
                var ally = GameObjects.AllyHeroes.FirstOrDefault(
                    x => !x.IsMe && !x.IsDead && x.Buffs.Any(a => a.Name.ToLower().Contains("kalistacoopstrikeally")));

                if (ally != null && ally.IsVisible && ally.DistanceToPlayer() <= R.Range)
                {
                    if (MiscOption.GetSliderBool("R", "AutoRAlly").Enabled && Me.CountEnemyHeroesInRange(R.Range) > 0 &&
                        ally.CountEnemyHeroesInRange(R.Range) > 0 &&
                        ally.HealthPercent <= MiscOption.GetSliderBool("R", "AutoRAlly").Value)
                    {
                        R.Cast();
                    }

                    if (MiscOption.GetBool("R", "Balista").Enabled && ally.CharacterName == "Blitzcrank")
                    {
                        if (
                            GameObjects.EnemyHeroes.Any(
                                x =>
                                    !x.IsDead && x.IsValidTarget() &&
                                    x.Buffs.Any(a => a.Name.ToLower().Contains("rocketgrab"))))
                        {
                            R.Cast();
                        }
                    }
                }
            }
        }

        private static void AutoEStealEvent()
        {
            if (MiscOption.GetBool("E", "AutoESteal").Enabled && E.IsReady())
            {
                foreach (
                    var mob in
                    GameObjects.EnemyMinions.Where(
                        x =>
                            x != null && x.IsValidTarget(E.Range) && x.MaxHealth > 5 && x.isBigMob()))
                {
                    if (mob.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")) && mob.IsValidTarget(E.Range))
                    {
                        if (mob.Health < E.GetKalistaRealDamage(mob))
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void ComboEvent()
        {
            if (ComboOption.GetBool("ComboGapcloser").Enabled)
            {
                ForcusAttack();
            }

            var target = MyTargetSelector.GetTarget(Q.Range);

            if (target != null && target.IsValidTarget(Q.Range))
            {
                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range) && !target.InAutoAttackRange())
                {
                    var qPred = Q.GetPrediction(target);

                    if (qPred.Hitchance >= HitChance.High)
                    {
                        Q.Cast(qPred.CastPosition);
                    }
                }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range) &&
                    Variables.GameTimeTickCount - lastETime > 500 + Game.Ping)
                {
                    if (target.Health < E.GetKalistaRealDamage(target,
                            MiscOption.GetSliderBool("E", "EToler").Enabled,
                            MiscOption.GetSliderBool("E", "EToler").Value) &&
                        !target.IsUnKillable())
                    {
                        E.Cast();
                    }

                    if (ComboOption.GetBool("ComboESlow").Enabled &&
                        target.DistanceToPlayer() > Me.AttackRange + Me.BoundingRadius + 100 &&
                        target.IsValidTarget(E.Range))
                    {
                        var EKillMinion = GameObjects.Minions.Where(x => x.IsValidTarget(Me.GetRealAutoAttackRange(x)))
                            .FirstOrDefault(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")) &&
                                                 x.DistanceToPlayer() <= E.Range && x.Health < E.GetKalistaRealDamage(x));

                        if (EKillMinion != null && EKillMinion.IsValidTarget(E.Range) &&
                            target.IsValidTarget(E.Range))
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void ForcusAttack()
        {
            if (GameObjects.EnemyHeroes.All(x => !x.IsValidTarget(Me.AttackRange + Me.BoundingRadius + x.BoundingRadius)) &&
                GameObjects.EnemyHeroes.Any(x => x.IsValidTarget((float)(Me.AttackRange * 1.65) + x.BoundingRadius)))
            {
                var AttackUnit =
                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Me.GetRealAutoAttackRange(x)))
                        .OrderBy(x => x.Distance(Game.CursorPos))
                        .FirstOrDefault();

                if (AttackUnit != null && !AttackUnit.IsDead && AttackUnit.InAutoAttackRange())
                {
                    Orbwalker.ForceTarget = AttackUnit;
                    LastForcusTime = Variables.GameTimeTickCount;
                }
            }
            else
            {
                Orbwalker.ForceTarget = null;
            }
        }

        private static void HarassEvent()
        {
            if (HarassOption.HasEnouguMana())
            {
                var target = HarassOption.GetTarget(Q.Range);

                if (target.IsValidTarget(Q.Range))
                {
                    if (HarassOption.UseQ && Q.IsReady())
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }

                    if (HarassOption.UseE && E.IsReady() && Variables.GameTimeTickCount - lastETime > 500 + Game.Ping)
                    {
                        if (HarassOption.GetBool("HarassESlow").Enabled &&
                            target.IsValidTarget(E.Range) &&
                            target.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
                        {
                            var EKillMinion = GameObjects.Minions.Where(x => x.IsValidTarget(Me.GetRealAutoAttackRange(x)))
                                .FirstOrDefault(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")) &&
                                                     x.DistanceToPlayer() <= E.Range && x.Health < E.GetKalistaRealDamage(x));

                            if (EKillMinion != null && EKillMinion.IsValidTarget(E.Range) &&
                                target.IsValidTarget(E.Range))
                            {
                                E.Cast();
                            }
                        }

                        if (HarassOption.GetSliderBool("HarassELeave").Enabled &&
                            target.DistanceToPlayer() >= 800 &&
                            target.Buffs.Find(a => a.Name.ToLower().Contains("kalistaexpungemarker")).Count >=
                            HarassOption.GetSliderBool("HarassELeave").Value)
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void ClearEvent()
        {
            if (MyManaManager.SpellHarass)
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
                if (LaneClearOption.GetSliderBool("LaneClearE").Enabled && E.IsReady())
                {
                    var KSCount =
                        GameObjects.EnemyMinions.Where(
                                x => x.IsValidTarget(E.Range) && x.IsMinion())
                            .Where(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
                            .Count(x => x.Health < E.GetKalistaRealDamage(x));

                    if (KSCount > 0 && KSCount >= LaneClearOption.GetSliderBool("LaneClearE").Value)
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void JungleClearEvent()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseE && E.IsReady() && Variables.GameTimeTickCount - lastETime > 500 + Game.Ping)
                {
                    var KSCount =
                        GameObjects.Jungle.Where(
                                x => x.IsValidTarget(E.Range) && x.GetJungleType() != JungleType.Unknown)
                            .Where(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
                            .Count(x => x.Health < E.GetKalistaRealDamage(x) * 0.5f);

                    if (KSCount > 0)
                    {
                        E.Cast();
                    }
                }

                if (JungleClearOption.UseQ && Q.IsReady())
                {
                    var qMob =
                        GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() != JungleType.Unknown)
                            .OrderByDescending(x => x.MaxHealth)
                            .FirstOrDefault();

                    if (qMob != null && qMob.IsValidTarget(Q.Range))
                    {
                        Q.Cast(qMob);
                    }
                }
            }
        }

        private static void LastHitEvent()
        {
            if (LastHitOption.HasEnouguMana && LastHitOption.UseE && E.IsReady())
            {
                if (GameObjects.EnemyMinions.Any(
                        x =>
                            x.IsValidTarget(E.Range) && 
                            x.IsMinion() &&
                            x.Buffs.Any(
                                a =>
                                    a.Name.ToLower().Contains("kalistaexpungemarker") &&
                                    x.Health < E.GetKalistaRealDamage(x))))
                {
                    E.Cast();
                }
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs Args)
        {
            if (Me.IsDead || !sender.IsMe)
            {
                return;
            }

            switch (Args.SData.Name.ToLower())
            {
                case "kalistaw":
                    lastWTime = Variables.GameTimeTickCount;
                    break;
                case "kalistaexpunge":
                case "kalistaexpungewrapper":
                case "kalistadummyspell":
                    lastETime = Variables.GameTimeTickCount;
                    break;
            }
        }

        private static void OnAction(object sender, OrbwalkerActionArgs Args)
        {
            if (Args.Type == OrbwalkerType.NonKillableMinion)
            {
                if (Me.IsDead || Me.IsRecalling() || !Me.CanMoveMent())
                {
                    return;
                }

                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    return;
                }

                if (LastHitOption.HasEnouguMana && LastHitOption.UseE && E.IsReady())
                {
                    var minion = Args.Target as AIMinionClient;
                    if (minion != null && minion.IsValidTarget(E.Range) && Me.CountEnemyHeroesInRange(600) == 0 &&
                        minion.Health < E.GetKalistaRealDamage(minion))
                    {
                        E.Cast();
                    }
                }
            }

            if (Args.Type == OrbwalkerType.BeforeAttack)
            {
                if (MiscOption.GetBool("Forcus", "ForcusAttack").Enabled && Me.CanMoveMent() && Args.Target != null && 
                    !Args.Target.IsDead && Args.Target.Health > 0)
                {
                    if (Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass)
                    {
                        foreach (var target in GameObjects.EnemyHeroes.Where(x => !x.IsDead &&
                                                                                  x.InAutoAttackRange() &&
                                                                                  x.Buffs.Any(
                                                                                      a =>
                                                                                          a.Name.ToLower()
                                                                                              .Contains(
                                                                                                  "kalistacoopstrikemarkally"))))
                        {
                            if (!target.IsDead && target.IsValidTarget(Me.GetRealAutoAttackRange(target)))
                            {
                                Orbwalker.ForceTarget = target;
                                LastForcusTime = Variables.GameTimeTickCount;
                            }
                        }
                    }
                    else if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                    {
                        foreach (var target in GameObjects.Minions.Where(x => !x.IsDead && x.IsEnemy &&
                                                      x.InAutoAttackRange() &&
                                                      x.Buffs.Any(
                                                          a =>
                                                              a.Name.ToLower()
                                                                  .Contains(
                                                                      "kalistacoopstrikemarkally"))))
                        {
                            if (!target.IsDead && target.IsValidTarget(Me.GetRealAutoAttackRange(target)))
                            {
                                Orbwalker.ForceTarget = target;
                                LastForcusTime = Variables.GameTimeTickCount;
                            }
                        }
                    }
                }
            }

            if (Args.Type == OrbwalkerType.AfterAttack)
            {
                Orbwalker.ForceTarget = null;
                 
                if (Args.Target == null || Args.Target.IsDead || Args.Target.Health <= 0 || Me.IsDead || !Q.IsReady())
                {
                    return;
                }

                switch (Args.Target.Type)
                {
                    case GameObjectType.AIHeroClient:
                        {
                            var target = (AIHeroClient)Args.Target;

                            if (target != null && !target.IsDead && target.IsValidTarget(Q.Range))
                            {
                                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                                {
                                    if (ComboOption.UseQ)
                                    {
                                        var qPred = Q.GetPrediction(target);

                                        if (qPred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(qPred.CastPosition);
                                        }
                                    }
                                }
                                else if (HarassOption.HasEnouguMana() &&
                                         (Orbwalker.ActiveMode == OrbwalkerMode.Harass ||
                                          Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && MyManaManager.SpellHarass))
                                {
                                    if (HarassOption.UseQ)
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
                        break;
                    case GameObjectType.AIMinionClient:
                        {
                            if (MyManaManager.SpellFarm && Orbwalker.ActiveMode == OrbwalkerMode.LaneClear &&
                                JungleClearOption.HasEnouguMana())
                            {
                                var mob = (AIMinionClient)Args.Target;
                                if (mob != null && mob.IsValidTarget(Q.Range) && mob.GetJungleType() != JungleType.Unknown)
                                {
                                    if (JungleClearOption.UseQ)
                                    {
                                        Q.Cast(mob);
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}
