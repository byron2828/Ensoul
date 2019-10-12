using Easy_Sup.Resources;
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
    class Morgana
    {
        #region
        //q
        public static readonly MenuBool Qpred = new MenuBool("qpred", "Draw Q Prediction");
        public static readonly MenuList qhit = new MenuList("qhit", "Q - HitChance :", new[] { "High", "Medium", "Low" });
        public static readonly MenuBool Qcombo = new MenuBool("qcombo", "Use Q on Combo");
        public static readonly MenuBool Qharass = new MenuBool("qharass", "Use Q on Harass");
        public static readonly MenuSlider qmana = new MenuSlider("qmana", "^ Mana >= X%", 60, 0, 100);
        public static readonly MenuBool Qant = new MenuBool("qant", "Auto use Q on gapcloser");
        public static readonly MenuBool Qint = new MenuBool("qint", "Auto use Q to interrupt");
        public static readonly MenuBool Qdash = new MenuBool("qdash", "Auto use Q on Dash");
        public static readonly MenuBool Qim = new MenuBool("qim", "Auto use Q on Immobile target");

        //w
        public static readonly MenuBool Wcombo = new MenuBool("wcombo", "Use W on Combo");
        public static readonly MenuList wmode = new MenuList("whit", "W - Mode :", new[] { "Allways", "Target Immobile"});
        public static readonly MenuSlider Wticks = new MenuSlider("wtick", "W Combo Damage Ticks", 6, 3, 10);
        public static readonly MenuBool Wharass = new MenuBool("wharass", "Use W on Harass");
        public static readonly MenuSlider wmana = new MenuSlider("wmana", "^ Mana >= X%", 60, 0, 100);
        public static readonly MenuBool Wpush = new MenuBool("wpush", "Use W on ClearWave");
        public static readonly MenuSlider wmin = new MenuSlider("wmin", "^ Minions >= X", 3, 1, 6);

        //r
        public static readonly MenuBool Rcombo = new MenuBool("rcombo", "Use R on Combo");
        public static readonly MenuSlider Rmin = new MenuSlider("rmin", "Use R if Enemies in Range >= X (put 0 to disable)", 3, 0, 5);
        public static readonly MenuBool Rkill = new MenuBool("rkill", "Use on Solo target if target is killable");

        #endregion


        // CREDITS : KURISU
        // CREDITS : KURISU
        // CREDITS : KURISU
        // CREDITS : KURISU
        // CREDITS : KURISU
        // CREDITS : KURISU
        // CREDITS : KURISU

        private static Menu _menu;
        private static Spell _q, _w, _e, _r;
        private static readonly AIHeroClient Me = ObjectManager.Player;
        private static EnsoulSharp.SDK.Geometry.Rectangle QRectangle { get; set; }

        public static void OnLoad()
        {
            if (Me.CharacterName != "Morgana")
                return;
            _q = new Spell(SpellSlot.Q, 1175f);
            _q.SetSkillshot(0.25f, 72f, 1200f, true, false, SkillshotType.Line);

            _w = new Spell(SpellSlot.W, 900f);
            _w.SetSkillshot(0.50f, 225f, 2200f, false, false, SkillshotType.Circle);

            _e = new Spell(SpellSlot.E, 750f);
            _r = new Spell(SpellSlot.R, 600f);


            QRectangle = new EnsoulSharp.SDK.Geometry.Rectangle(ObjectManager.Player.Position, Vector3.Zero, _q.Width);

            CreateMenu();
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            try
            {
                AIBaseClient.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            }
            catch { }
        }

        private static void CreateMenu()
        {
            _menu = new Menu("EasySup.Morgana", "EasySup.morgana", true);

            var _qmenu = new Menu("qmenu", "[Q] Settings");
            _qmenu.Add(qhit);
            _qmenu.Add(Qcombo);
            _qmenu.Add(Qharass);
            _qmenu.Add(qmana);
            _qmenu.Add(Qint);
            _qmenu.Add(Qant);
            _qmenu.Add(Qdash);
            _qmenu.Add(Qim);
            _qmenu.Add(Qpred);

            var _wmenu = new Menu("wmenu", "[W] Settings");
            _wmenu.Add(Wcombo);
            _wmenu.Add(wmode);
            _wmenu.Add(Wharass);
            _wmenu.Add(wmana);
            _wmenu.Add(Wpush);
            _wmenu.Add(wmin);


            var _emenu = new Menu("emenu", "[E] Settings (Not fully functional, I'll fix it later.)");

            foreach (var ene in ObjectManager.Get<AIHeroClient>().Where(x => x.Team != Me.Team))
            {
                foreach (var lib in KurisuLib.CCList.Where(x => x.HeroName == ene.CharacterName))
                {
                    _emenu.Add(new MenuBool(lib.SDataName, lib.SpellMenuName + " from " + ene.CharacterName));
                }
            }
            var _rmenu = new Menu("rmenu", "[R] Settings");
            _rmenu.Add(Rcombo);
            _rmenu.Add(Rmin);
            _rmenu.Add(Rkill);

            _menu.Add(_qmenu);
            _menu.Add(_wmenu);
            _menu.Add(_emenu);
            _menu.Add(_rmenu);

            var pred = new Menu("pred", "SPrediction");
            SPrediction.Prediction.Initialize(pred);
            _menu.Add(pred);
            _menu.Attach();
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            try
            {
                var Target = TargetSelector.GetTarget(_q.Range);
                if (Target.IsValidTarget(_q.Range))
                {
                    QRectangle.Start = ObjectManager.Player.Position.ToVector2();
                    QRectangle.End = _q.GetSPrediction(Target).CastPosition;
                    QRectangle.UpdatePolygon();
                }

                AutoCast(Qdash.Enabled,Qim.Enabled);

                switch (Orbwalker.ActiveMode)
                {
                    case OrbwalkerMode.Combo:
                        Combo(Qcombo.Enabled,Wcombo.Enabled,Rcombo.Enabled);
                        break;
                    case OrbwalkerMode.Harass:
                        Harass(Qharass.Enabled,Wharass.Enabled);
                        break;
                    case OrbwalkerMode.LaneClear:
                        LaneClear();
                        break;
                }
            }
            catch { }

        }

        private static void AntiGapcloser_OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (sender.IsValidTarget(250f))
            {
                if (Qant.Enabled)
                    _q.Cast(sender);
            }
        }

        private static bool Immobile(AIHeroClient unit)
        {
            return unit.HasBuffOfType(BuffType.Charm) || unit.HasBuffOfType(BuffType.Knockup) ||
                   unit.HasBuffOfType(BuffType.Snare) ||
                   unit.HasBuffOfType(BuffType.Taunt) || unit.HasBuffOfType(BuffType.Suppression);
        }

        private static void LaneClear()
        {
            List<AIBaseClient> minions = MinionManager.GetMinions(Me.Position, _w.Range);
            var wCastLocation = _w.GetCircularFarmLocation(minions, _w.Width);
            if (_w.IsReady() && wCastLocation.MinionsHit > wmin.Value && Wpush.Enabled)
            {
                _w.Cast(wCastLocation.Position);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (_q.IsReady())
            {
                Render.Circle.DrawCircle(Me.Position, _q.Range,
                    System.Drawing.Color.FromArgb(155, System.Drawing.Color.DeepPink), 4);
            }
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp)
            {
                return;
            }
            if (!Qpred.Enabled)
                return;
            var t = TargetSelector.GetTarget(_q.Range);
            if (_q.IsReady() && t.IsValidTarget(_q.Range))
            {
                if (_q.GetSPrediction(t).HitChance != HitChance.OutOfRange && _q.GetSPrediction(t).HitChance != HitChance.Collision && _q.GetSPrediction(t).HitChance >= getQ())
                    QRectangle.Draw(Color.LightGreen, 3);
                else
                {
                    QRectangle.Draw(Color.Red, 3);
                }
            }
        }

        private static HitChance getQ()
        {
            var hitchance = HitChance.High;
            switch (qhit.Index)
            {
                case 0:
                    hitchance = HitChance.High;
                    break;
                case 1:
                    hitchance = HitChance.Medium;
                    break;
                case 2:
                    hitchance = HitChance.Low;
                    break;
            }
            return hitchance;
        }

        private static HitChance getW()
        {
            var hitchance = HitChance.Medium;
            switch (wmode.Index)
            {
                case 0:
                    hitchance = HitChance.Medium;
                    break;
                case 1:
                    hitchance = HitChance.Immobile;
                    break;
            }
            return hitchance;
        }

        private static void Combo(bool useq, bool usew, bool user)
        {
            var qhit = getQ();
            if (useq && _q.IsReady())
            {
                var qtarget = TargetSelector.GetTarget(_q.Range);
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    _q.SPredictionCast(qtarget, qhit);
                }
            }
            if (usew && _w.IsReady())
            {
                var wtarget = TargetSelector.GetTarget(_w.Range + 10);
                if (wtarget.IsValidTarget())
                {
                    _w.SPredictionCast(wtarget, getW());
                }
            }
            if (user && _r.IsReady())
            {
                var calcW = Wticks.Value;
                var rtarget = TargetSelector.GetTarget(_r.Range);
                if (rtarget.IsValidTarget(_r.Range))
                {
                    if (Rkill.Enabled && (_q.GetDamage(rtarget) > rtarget.Health && _q.IsReady()) || _w.GetDamage(rtarget) > rtarget.Health && _w.IsReady())
                    {
                        if (_e.IsReady()) _e.CastOnUnit(Me);
                        _r.Cast();
                    }
                }
                if (Me.CountEnemyHeroesInRange(_r.Range) >= Rmin.Value)
                {
                    if (_e.IsReady()) _e.CastOnUnit(Me);
                    _r.Cast();
                }
            }
        }

        private static void Harass(bool useq, bool usew)
        {
            if (useq && _q.IsReady())
            {
                var qhit = getQ();
                var qtarget = TargetSelector.GetTarget(_q.Range);
                if (qtarget.IsValidTarget())
                {
                    if (Me.ManaPercent >= qmana.Value)
                    {
                        _q.SPredictionCast(qtarget, qhit);
                    }
                }
            }
            if (usew && _w.IsReady())
            {
                var wtarget = TargetSelector.GetTarget(_w.Range);
                if (wtarget.IsValidTarget())
                {
                    if(Me.ManaPercent >= wmana.Value)
                    {
                        _w.SPredictionCast(wtarget, HitChance.High);
                    }
                }
            }
        }

        private static void AutoCast(bool dashing, bool immobile)
        {
            if (_q.IsReady())
            {
                foreach (var itarget in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(_q.Range))) {
                    if (dashing)
                    {
                        _q.SPredictionCast(itarget, HitChance.Dash);
                    }

                    if (immobile)
                    {
                        _q.SPredictionCast(itarget, HitChance.Immobile);
                    }
                }
            }
        }

        internal static void Obj_AI_Base_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.Type != Me.Type || !_e.IsReady() || !sender.IsEnemy)
                return;

            var attacker = ObjectManager.Get<AIHeroClient>().First(x => x.NetworkId == sender.NetworkId);
            foreach (var ally in GameObjects.AllyHeroes.Where(x => x.IsValidTarget(_e.Range, false)))
            {
                var detectRange = ally.Position + (args.End - ally.Position).Normalized() * ally.Distance(args.End);
                if (detectRange.Distance(ally.Position) > ally.AttackRange - ally.BoundingRadius)
                    continue;

                foreach (var lib in KurisuLib.CCList.Where(x => x.HeroName == attacker.CharacterName && x.Slot == attacker.GetSpellSlot(args.SData.Name)))
                {
                    if (lib.Type == Skilltype.Unit && args.Target.NetworkId != ally.NetworkId)
                        return;
                    try
                    {
                        if (_menu["emenu"].GetValue<MenuBool>(lib.SDataName).Enabled)
                        {
                            Console.WriteLine(_menu["emenu"].GetValue<MenuBool>(lib.SDataName).Enabled.ToString());
                            Console.WriteLine(lib.SDataName);
                            Console.WriteLine(ally.CharacterName);
                            _e.CastOnUnit(ally);
                            _e.Cast(ally);
                            _e.Cast(Me);
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
