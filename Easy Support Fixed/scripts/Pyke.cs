using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SPrediction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy_Sup.scripts
{
    class Pyke
    {
        private static Spell Q, W, E, R, Q2;
        private static Menu geral;

        public static void On_Load()
        {
            Q = new Spell(SpellSlot.Q, 400f);
            Q.SetSkillshot(0.25f, 70f, 2000, true, false, SkillshotType.Line);
            Q.SetCharged("PykeQ", "PykeQ", 400, 1030, 1.0f);
            E = new Spell(SpellSlot.E, 550f);
            E.SetSkillshot(0.275f, 70f, 500f, false, false, SkillshotType.Line);
            R = new Spell(SpellSlot.R, 750f);
            R.SetSkillshot(0.25f, 100f, float.MaxValue, false, false, SkillshotType.Circle);

            CreateMenu();
            Game.OnTick += OnTick;
            Drawing.OnDraw += OnDraw;
        }

        private static void CreateMenu()
        {
            var geral = new Menu("menu.base", "011110001.Pyke", true);

            var Combat = new Menu("Pyke_Combat", "Combo Settings");
            Combat.Add(Menubase.Pyke_Combat.Q);
            Combat.Add(Menubase.Pyke_Combat.Qhit);
            Combat.Add(Menubase.Pyke_Combat.E);
            Combat.Add(Menubase.Pyke_Combat.R);
            Combat.Add(Menubase.Pyke_Combat.Rkill);

            var harass = new Menu("harass", "Harass Settings");
            harass.Add(Menubase.Pyke_Harass.Q);
            harass.Add(Menubase.Pyke_Harass.E);

            var Clear = new Menu("Clear", "Clear Settings");
            Clear.Add(Menubase.Pyke_Clear.Ec);
            Clear.Add(Menubase.Pyke_Clear.ehit);

            var ks = new Menu("killsteal", "KillSteal Settings");
            ks.Add(Menubase.Pyke_KS.R);

            var misc = new Menu("misc", "Misc Settings");
            misc.Add(Menubase.Pyke_misc.draw);

            var pred = new Menu("spred", "Spred");

            geral.Add(Combat);
            geral.Add(harass);
            geral.Add(Clear);
            geral.Add(ks);
            geral.Add(misc);
            geral.Add(pred);
            Prediction.Initialize(pred);
            geral.Attach();

        }

        private static void OnTick(EventArgs args)
        {
            if (Menubase.Pyke_KS.R.Enabled)
                KS();
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


        private static void KS()
        {
            var al = GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsEnemy && !x.IsInvulnerable && x.Health < R.GetDamage(x, DamageStage.Empowered) && x.DistanceToPlayer() < R.Range);
            var t = al.FirstOrDefault(x => x.IsValidTarget(R.Range));
            if (t != null && !ObjectManager.Player.IsRecalling())
            {
                if (Orbwalker.ActiveMode != OrbwalkerMode.Combo && !t.IsDead && !t.IsZombie && t.IsVisible && t.IsHPBarRendered)
                {
                    R.SPredictionCast(t, HitChance.Medium);
                }
            }
        }

        private static void Harass()
        {
            if (!Q.IsCharging && E.IsReady() && Menubase.Pyke_Combat.E.Enabled)
            {
                var target = TargetSelector.GetTarget(E.Range);
                if (target != null && target.IsValidTarget(E.Range))
                {
                    var pred = E.GetSPrediction(target);
                    if (pred.HitChance >= HitChance.High)
                    {
                        E.SPredictionCast(target, HitChance.High);
                    }
                }
            }
            var qvalue = Menubase.Pyke_Combat.Qhit.Value;
            var qhit = HitChance.High;
            switch (qvalue)
            {
                case 1:
                    qhit = HitChance.Low;
                    break;
                case 2:
                    qhit = HitChance.Medium;
                    break;
                case 3:
                    qhit = HitChance.High;
                    break;
                case 4:
                    qhit = HitChance.VeryHigh;
                    break;
            }
            if (Q.IsReady() && Menubase.Pyke_Combat.Q.Enabled)
            {
                var target = TargetSelector.GetTarget(Q.ChargedMaxRange);
                if (target != null && target.IsValidTarget(Q.ChargedMaxRange))
                {
                    var pred = Q.GetPrediction(target);
                    if (pred.Hitchance >= qhit)
                    {
                        Q.StartCharging();
                    }
                }
            }
            if (Q.IsReady() && Q.IsCharging)
            {
                var target = TargetSelector.GetTarget(Q.Range);
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    var pred = Q.GetSPrediction(target);
                    if (pred.HitChance >= HitChance.High)
                    {
                        Q.ShootChargedSpell(pred.CastPosition);
                    }
                }
            }
        }

        private static void Combo()
        {
            if (!Q.IsCharging && E.IsReady() && Menubase.Pyke_Combat.E.Enabled)
            {
                var target = TargetSelector.GetTarget(E.Range);
                if (target != null && target.IsValidTarget(E.Range))
                {
                    var pred = E.GetSPrediction(target);
                    if (pred.HitChance >= HitChance.High)
                    {
                        E.SPredictionCast(target, HitChance.High);
                    }
                }
            }
            var qvalue = Menubase.Pyke_Combat.Qhit.Value;
            var qhit = HitChance.High;
            switch (qvalue)
            {
                case 1:
                    qhit = HitChance.Low;
                    break;
                case 2:
                    qhit = HitChance.Medium;
                    break;
                case 3:
                    qhit = HitChance.High;
                    break;
                case 4:
                    qhit = HitChance.VeryHigh;
                    break;
            }
            if (Q.IsReady() && Menubase.Pyke_Combat.Q.Enabled)
            {
                var target = TargetSelector.GetTarget(Q.ChargedMaxRange);
                if (target != null && target.IsValidTarget(Q.ChargedMaxRange))
                {
                    var pred = Q.GetSPrediction(target);
                    if (pred.HitChance >= qhit)
                    {
                        Q.StartCharging();
                    }
                }
            }
            if (Q.IsReady() && Q.IsCharging)
            {
                var target = TargetSelector.GetTarget(Q.Range);
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    var pred = Q.GetSPrediction(target);
                    if (pred.HitChance >= qhit)
                    {
                        Q.ShootChargedSpell(pred.CastPosition);
                    }
                }
            }
            if (R.IsReady() && Menubase.Pyke_Combat.R.Enabled)
            {
                var rt = TargetSelector.GetTarget(R.Range);
                if (rt != null && rt.IsValidTarget(R.Range))
                {
                    if (Menubase.Pyke_Combat.Rkill.Enabled && rt.Health > R.GetDamage(rt, DamageStage.Empowered))
                    {
                        return;
                    }
                    if(!rt.IsDead && !rt.IsZombie && rt.IsVisible && rt.IsHPBarRendered)
                    {
                        R.SPredictionCast(rt, HitChance.High);
                    }
                }
            }
        }
        private static void Clear()
        {

            if (Menubase.Pyke_Clear.Ec && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                            .Cast<AIBaseClient>().ToList();

                if (minions.Any())
                {
                    var eFarmLocation = E.GetLineFarmLocation(minions);
                    if (eFarmLocation.Position.IsValid() && eFarmLocation.MinionsHit >= Menubase.Pyke_Clear.ehit.Value)
                    {
                        E.Cast(eFarmLocation.Position);
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp)
            {
                return;
            }
            if (!Menubase.Pyke_misc.draw.Enabled)
                return;
            if (Q.IsReady())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.LightGreen);
            }
        }
    }
}
