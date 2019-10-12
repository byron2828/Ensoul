using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy_Sup.scripts
{
    class Alistar
    {
        private static Spell Q, W, E, R;
        private static Menu _menu;



        #region
        public static readonly MenuBool useqw = new MenuBool("use", "Use W+Q Combo");
        public static readonly MenuBool usee = new MenuBool("usee", "Use E after W+Q");
        public static readonly MenuBool wgap = new MenuBool("wint", "Use W to anti Gap Closer");
        public static readonly MenuSlider wrange = new MenuSlider("wrante", "^ Range Anti Gap Closer", 650, 365, 650);
        public static readonly MenuBool autoR = new MenuBool("autoR", "Auto use R to clear CC");
        public static readonly MenuSlider minhp = new MenuSlider("minhp", "^ Min HP% to use", 60, 0, 100);
        #endregion


        public static void OnLoad()
        {
            Q = new Spell(SpellSlot.Q, 365);
            W = new Spell(SpellSlot.W, 650);
            E = new Spell(SpellSlot.E, 575);
            R = new Spell(SpellSlot.R, 0);

            W.SetTargetted(0.5f, float.MaxValue);

            CreateMenu();
            Game.OnUpdate += OnUpdate;
            Gapcloser.OnGapcloser += OnGapcloser;
        }


        public static void CreateMenu()
        {
            var _menu = new Menu("easysupalistar", "Easy_Sup.Alistar", true);
            var _geral = new Menu("geral", "General Settings");
            _geral.Add(useqw);
            _geral.Add(usee);
            _geral.Add(wgap);
            _geral.Add(wrange);
            _geral.Add(autoR);
            _geral.Add(minhp);

            _menu.Add(_geral);
            _menu.Attach();
        }

        private static void OnUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
            }
            AutoR();
        }

        private static void Combo()
        {
            var wtarget = TargetSelector.GetTarget(W.Range);

            if (wtarget == null)
                return;

            if (Q.IsReady() && W.IsReady() && wtarget.IsValidTarget(W.Range) && useqw.Enabled)
            {
                W.CastOnUnit(wtarget, true);
                var jumpTime = Math.Max(0, ObjectManager.Player.Distance(wtarget) - 500) * 10 / 25 + 25;
                EnsoulSharp.SDK.Utility.DelayAction.Add((int)jumpTime, () => Q.Cast());
            }
            if (usee.Enabled && E.IsReady() && !Q.IsReady() && !W.IsReady() && wtarget.IsValidTarget(E.Range))
            {
                E.Cast();
            }
        }

        private static void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling())
            {
                return;
            }

            if (MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
            {
                return;
            }

            if (W.IsReady() && wgap.Enabled && args.EndPosition.DistanceToPlayer() < wrange)
            {
                W.Cast(sender);
            }
        }

        private static void AutoR()
        {
            if (ObjectManager.Player.HasBuffOfType(BuffType.Stun) || ObjectManager.Player.HasBuffOfType(BuffType.Snare) &&
                autoR.Enabled && ObjectManager.Player.HealthPercent < minhp.Value)
            {
                R.Cast();
            }
        }
    }
}
