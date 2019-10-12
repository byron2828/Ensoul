using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using SPrediction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

namespace Easy_Sup.scripts
{
    class Thresh
    {
        private static Spell Q, Q2, W, E, R;
        private static float CheckInterval = 50f;
        private static Menu IsMenu;
        public static Vector2 oWp;
        public static Vector2 nWp;
        private static EnsoulSharp.SDK.Geometry.Rectangle QRectangle { get; set; }
        private static readonly Dictionary<int, List<Vector2>> _waypoints = new Dictionary<int, List<Vector2>>();

        public static void OnLoad()
        {
            Q = new Spell(SpellSlot.Q, 1100);
            Q2 = new Spell(SpellSlot.Q, 1400);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 400);
            R = new Spell(SpellSlot.R, 450);


            Q.SetSkillshot(0.500f, 70, 1900f, true, false, SkillshotType.Line);
            Q2.SetSkillshot(0.500f, 70, 1900f, true, false, SkillshotType.Line);
            QRectangle = new EnsoulSharp.SDK.Geometry.Rectangle(ObjectManager.Player.Position, Vector3.Zero, Q.Width);
            var IsMenu = new Menu("Easy_Sup.Thresh", "Easy_Sup.Thresh", true);
            var combo = new Menu("Combo", "Combo Config");
            combo.Add(Menubase.thresh_combat.Qpred);
            combo.Add(Menubase.thresh_combat.Q);
            combo.Add(Menubase.thresh_combat.Q2);
            combo.Add(Menubase.thresh_combat.qhit);
            combo.Add(Menubase.thresh_combat.E);
            combo.Add(Menubase.thresh_combat.emodo);
            combo.Add(Menubase.thresh_combat.R);
            combo.Add(Menubase.thresh_combat.Rcount);

            var harass = new Menu("Harass", "Harass Config");
            harass.Add(Menubase.thresh_harass.Q);
            harass.Add(Menubase.thresh_harass.E);

            var misc = new Menu("Misc", "Misc Config");
            misc.Add(Menubase.thresh_misc.Egap);
            misc.Add(Menubase.thresh_misc.Ein);

            var pred = new Menu("pred", "Spred Config");

            IsMenu.Add(combo);
            IsMenu.Add(harass);
            IsMenu.Add(misc);
            SPrediction.Prediction.Initialize(pred);
            IsMenu.Add(pred);
            IsMenu.Attach();

            Game.OnUpdate += OnGameUpdate;
            Interrupter.OnInterrupterSpell += OnPossibleToInterrupt;
            Gapcloser.OnGapcloser += OnEnemyGapcloser;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnPossibleToInterrupt(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (!sender.IsEnemy)
                return;

            if (!Menubase.thresh_misc.Ein.Enabled)
                return;

            if (E.IsReady() && E.IsInRange(sender))
            {
                E.Cast(sender.Position);
            }
        }

        private static void OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling())
            {
                return;
            }

            if (MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
            {
                return;
            }
            if (Menubase.thresh_misc.Egap.Enabled && E.IsReady() && E.IsInRange(sender))
            {
                E.Cast(ObjectManager.Player.Position.Extend(sender.Position, 250));
            }
        }

        static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.ToVector2() + distance * Vector3.Normalize(direction - from).ToVector2();
        }

        private static void Pull()
        {
            var target = TargetSelector.GetTarget(E.Range);

            if (E.IsReady() && ObjectManager.Player.Distance(target.Position) < E.Range)
            {
                E.Cast(target.Position.Extend(ObjectManager.Player.Position, Vector3.Distance(target.Position, ObjectManager.Player.Position) + 400));
            }
        }

        private static void Push()
        {
            var target = TargetSelector.GetTarget(E.Range);
            if (E.IsReady() && ObjectManager.Player.Distance(target.Position) < E.Range)
            {
                E.Cast(target.Position);
            }
        }

        private static void CastQ()
        {
            var qhit = HitChance.High;
            switch (Menubase.thresh_combat.qhit.Value)
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
            var target = TargetSelector.GetTarget(Q.Range);
            if (target == null)
                return;

            if (target.HasBuff("threshQ") && !Menubase.thresh_combat.Q2.Enabled)
                return;

            Q.SPredictionCast(target, qhit);
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range);
            if (target == null)
                return;
            if (Q.IsReady() && Menubase.thresh_harass.Q.Enabled)
            {
                CastQ();
            }
            if (E.IsReady() & Menubase.thresh_harass.E.Enabled && ObjectManager.Player.Distance(target.Position) < E.Range)
            {
                E.Cast(V2E(target.Position, ObjectManager.Player.Position, ObjectManager.Player.Distance(target.Position) + 400));
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range);
            if (target == null)
                return;

            if (Q.IsReady() && Menubase.thresh_combat.Q.Enabled)
            {
                CastQ();
            }

            if (E.IsReady() & Menubase.thresh_combat.E.Enabled && ObjectManager.Player.Distance(target.Position) < E.Range)
            {
                if (Menubase.thresh_combat.emodo.Value == 0)
                    E.Cast(V2E(target.Position, ObjectManager.Player.Position, ObjectManager.Player.Distance(target.Position) + 400));

                else
                    E.Cast(target.Position);
            }

            try
            {
                if(W.IsReady() && Menubase.thresh_combat.W.Enabled && ObjectManager.Player.Distance(target.Position) < E.Range - 100)
                {
                    ThrowLantern();
                }
            }
            catch
            {
                //error
            }

            if (R.IsReady() && Menubase.thresh_combat.R.Enabled && ObjectManager.Player.CountEnemyHeroesInRange(R.Range) >=
                Menubase.thresh_combat.Rcount.Value)
            {
                R.Cast();
            }
        }


        private static void OnGameUpdate(EventArgs args)
        {
            try
            {
                var Target = TargetSelector.GetTarget(Q.Range);
                if (Target.IsValidTarget(Q.Range))
                {
                    QRectangle.Start = ObjectManager.Player.Position.ToVector2();
                    QRectangle.End = Q.GetSPrediction(Target).CastPosition;
                    QRectangle.UpdatePolygon();
                }
            }
            catch
            {

            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp)
            {
                return;
            }
            if (!Menubase.thresh_combat.Qpred.Enabled)
                return;
            var t = TargetSelector.GetTarget(Q.Range);
            if (Q.IsReady() && t.IsValidTarget(Q.Range))
            {
                if(Q.GetSPrediction(t).HitChance != HitChance.OutOfRange && Q.GetSPrediction(t).HitChance != HitChance.Collision)
                QRectangle.Draw(Color.LightGreen, 3);
                else
                {
                    QRectangle.Draw(Color.Red, 3);
                }
            }
        }

        private static void ThrowLantern()
        {
            try
            {
                if (W.IsReady())
                {
                    var NearAllies = GameObjects.AllyHeroes.Where(x => !x.IsMe && !x.IsDead && x.DistanceToPlayer() <= W.Range + 100).FirstOrDefault();

                    if (NearAllies == null) return;

                    W.Cast(NearAllies.Position);
                }
            }
            catch {
                // error
            }
        }
    }
}
