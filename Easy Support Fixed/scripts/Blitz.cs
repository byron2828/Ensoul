using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
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
    class Blitz
    {
        private static Menu Ismenu;
        private static Spell _q, _e, _r;
        private static readonly AIHeroClient Me = ObjectManager.Player;
        public static void BlitzOnLoad()
        {
            if (Me.CharacterName != "Blitzcrank")
                return;

            _q = new Spell(SpellSlot.Q, 1150);
            _q.SetSkillshot(0.25f, 70f, 1800f, true, false,SkillshotType.Line);

            _e = new Spell(SpellSlot.E, 150f);
            _r = new Spell(SpellSlot.R, 550f);


            CreateMenu();

            Game.OnUpdate += BlitzOnUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
        }


        public static void CreateMenu()
        {
            var IsMenu = new Menu("Easy_Sup.Blitz", "Easy_Sup.Blitz", true);


            var combo1 = new Menu("Combo", "Combo Config");
            combo1.Add(Menubase.blitz_combat.Qb);
            combo1.Add(Menubase.blitz_combat.Wb);
            combo1.Add(Menubase.blitz_combat.Eb);
            combo1.Add(Menubase.blitz_combat.Rb);
            combo1.Add(Menubase.blitz_combat.Rcount);
            combo1.Add(Menubase.blitz_combat.qhit);

            //var Qconfig = new Menu("grab", "Q Config(Not Work at moment)");
            //foreach (var inimigo in GameObjects.EnemyHeroes)
            // {
            //     MenuBool Grab = new MenuBool("qgrab " + inimigo.CharacterName, "Q on " + inimigo.CharacterName);
            //     Qconfig.Add(Grab);
            // }
            // combo.Add(Qconfig);


            var misc = new Menu("Misc", "Misc Config");
            misc.Add(Menubase.blitz_misc.Qdash);
            misc.Add(Menubase.blitz_misc.Qint);
            misc.Add(Menubase.blitz_misc.Rint);
            misc.Add(Menubase.blitz_misc.Qii);

            var ks = new Menu("ks", "Ks Config");
            ks.Add(Menubase.blitz_ks.Qks);
            ks.Add(Menubase.blitz_ks.Rks);

            var Draw = new Menu("Draw", "Draw Spells");
            Draw.Add(Menubase.blitz_draw.Dq);
            Draw.Add(Menubase.blitz_draw.Dr);

            var pred = new Menu("pred", "SPrediction");
            SPrediction.Prediction.Initialize(pred);
            IsMenu.Add(combo1);
            IsMenu.Add(misc);
            IsMenu.Add(Draw);
            IsMenu.Add(pred);
            IsMenu.Attach();

        }

        private static void BlitzOnUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
            }
            Secure();
            AutoCast();
        }

        private static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (!sender.IsEnemy)
                return;

            if (!Menubase.blitz_misc.Qint.Enabled)
                return;

            if (sender.DistanceToPlayer() < _q.Range)
                _q.SPredictionCast(sender, HitChance.High);
        }

        private static void AutoCast()
        {
            if (_q.IsReady())
            {
                foreach (
                    var ii in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(x => x.IsValidTarget(_q.Range)))
                {
                    if (Menubase.blitz_misc.Qdash.Enabled)
                    {
                        _q.SPredictionCast(ii, HitChance.Dash);
                    }
                    if (Menubase.blitz_misc.Qii.Enabled)
                    {
                        _q.SPredictionCast(ii, HitChance.Immobile);
                    }
                }
            }
        }

        private static void Combo()
        {
            var useq = Menubase.blitz_combat.Qb.Enabled;
            var hitchance = HitChance.High;
            switch (Menubase.blitz_combat.qhit.Value)
            {
                case 1:
                    hitchance = HitChance.Low;
                    break;
                case 2:
                    hitchance = HitChance.Medium;
                    break;
                case 3:
                    hitchance = HitChance.High;
                    break;
                case 4:
                    hitchance = HitChance.VeryHigh;
                    break;
            }
            if (useq && _q.IsReady())
            {
                var qtarget = TargetSelector.GetTarget(_q.Range);
                if (qtarget == null)
                    return;
                if (qtarget.IsValidTarget(_q.Range))
                {
                    _q.SPredictionCast(qtarget as AIHeroClient, hitchance);
                }
            }

            var usee = Menubase.blitz_combat.Eb.Enabled;

            if (usee && _e.IsReady())
            {
                var etarget = TargetSelector.GetTarget(350);
                if (etarget.IsValidTarget())
                {
                    _e.CastOnUnit(Me);
                }
                var qtarget = TargetSelector.GetTarget(_q.Range);
                if (qtarget.IsValidTarget(_q.Range))
                {
                    if (qtarget.HasBuff("rocketgrab2"))
                    {
                        _e.CastOnUnit(Me);
                    }
                }
            }
            var user = Menubase.blitz_combat.Rb.Enabled;
            var rcount = Menubase.blitz_combat.Rcount.Value;
            if (user && _r.IsReady())
            {
                if (ObjectManager.Player.CountEnemyHeroesInRange(_r.Range) >= rcount)
                {
                    _r.Cast();
                }
            }
        }

        private static void Secure()
        {
            var useq = Menubase.blitz_ks.Qks.Enabled;
            var user = Menubase.blitz_ks.Rks.Enabled;

            if (user && _r.IsReady())
            {
                var rtarget = ObjectManager.Get<AIHeroClient>().FirstOrDefault(h => h.IsEnemy);
                if (rtarget.IsValidTarget(_r.Range))
                {
                    if (Me.GetSpellDamage(rtarget, SpellSlot.R) >= rtarget.Health)
                        _r.Cast();
                }
            }
            if (useq && _q.IsReady())
            {
                var qtarget = ObjectManager.Get<AIHeroClient>().FirstOrDefault(h => h.IsEnemy);
                if (qtarget.IsValidTarget(_q.Range))
                {
                    if (Me.GetSpellDamage(qtarget, SpellSlot.Q) >= qtarget.Health)
                    {
                        _q.SPredictionCast(qtarget, HitChance.High);
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
            if (Menubase.blitz_draw.Dq.Enabled && _q.IsReady())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.LightGreen);
            }
            if (Menubase.blitz_draw.Dr.Enabled && _r.IsReady())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.LightGreen);
            }
        }
    }
}
