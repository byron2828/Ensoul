using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using SPrediction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy_Sup.scripts
{
    class Lux
    {
        public static Menu Config;
        public static Spell Q, W, E, R;
        public static Vector3 castpos;
        public static GameObject EGameObject;
        public static AIHeroClient player;

        public static Menu menu;

        public static void Load()
        {
            Q = new Spell(SpellSlot.Q, 1175);
            W = new Spell(SpellSlot.W, 1075);
            E = new Spell(SpellSlot.E, 1100);
            R = new Spell(SpellSlot.R, 3340);

            Q.SetSkillshot(0.5f, 80, 1200, false, false, SkillshotType.Line);
            W.SetSkillshot(0.5f, 150, 1200, false, false, SkillshotType.Line);
            E.SetSkillshot(0.5f, 275, 1300, false, false, SkillshotType.Circle);
            R.SetSkillshot(1.75f, 190, 3000, false, false, SkillshotType.Line);


            CreateMenu();

            Game.OnUpdate += Game_OnGameUpdate;
            Gapcloser.OnGapcloser += AntiGapcloserOnOnEnemyGapcloser;

            GameObject.OnCreate += delegate (GameObject sender, EventArgs args)
            {
                if (sender.Name.Contains("LuxLightstrike_tar_green")
                    || sender.Name.Contains("LuxLightstrike_tar_red"))
                {
                    EGameObject = sender;
                }
            };

            GameObject.OnDelete += delegate (GameObject sender, EventArgs args)
            {

                if (sender.Name.Contains("LuxLightstrike_tar_"))
                {
                    EGameObject = null;
                }
            };
        }


        public static void CreateMenu()
        {
            menu = new Menu("Easy_Sup.Lux", "Easy_Sup.Lux", true);
            var qconfig = new Menu("q", "[Q] - Light Binding");
            qconfig.Add(Menubase.lux_q.Q);
            qconfig.Add(Menubase.lux_q.autoQ);
            qconfig.Add(Menubase.lux_q.gap);
            var wconfig = new Menu("w", "[W] - Super Magical Barrier");
            wconfig.Add(Menubase.lux_w.W);
            wconfig.Add(Menubase.lux_w.Wper);
            //wconfig.Add(Menubase.lux_w.autoW);

            var econfig = new Menu("e", "[E] - Lucent Singularity");
            econfig.Add(Menubase.lux_e.E);
            var rconfig = new Menu("r", "[R] - Finales Funkeln");
            rconfig.Add(Menubase.lux_r.R);
            rconfig.Add(Menubase.lux_r.Rkill);
            rconfig.Add(Menubase.lux_r.rprecision);
            var ks = new Menu("ks", "Killsteal Config");
            ks.Add(Menubase.lux_ks.ksQ);
            ks.Add(Menubase.lux_ks.ksE);
            ks.Add(Menubase.lux_ks.ksR);
            var clear = new Menu("clear", "Clear Wave config");
            clear.Add(Menubase.lux_clear.clearQ);
            clear.Add(Menubase.lux_clear.Qcount);
            clear.Add(Menubase.lux_clear.clearE);
            clear.Add(Menubase.lux_clear.Ecount);
            clear.Add(Menubase.lux_clear.mana);
            var jgsteal = new Menu("jgsteal", "Steal Buffs Config");
            jgsteal.Add(Menubase.lux_steal.steal);
            jgsteal.Add(Menubase.lux_steal.red);
            jgsteal.Add(Menubase.lux_steal.blue);
            jgsteal.Add(Menubase.lux_steal.dragon);
            jgsteal.Add(Menubase.lux_steal.baron);
            var hitchance = new Menu("hitchance", "Hitchance Config");
            hitchance.Add(Menubase.lux_hit.qhit);
            hitchance.Add(Menubase.lux_hit.ehit);
            hitchance.Add(Menubase.lux_hit.rhit);

            menu.Add(qconfig);
            menu.Add(wconfig);
            menu.Add(econfig);
            menu.Add(rconfig);
            menu.Add(ks);
            menu.Add(clear);
            menu.Add(hitchance);
            var spred = new Menu("Spred", "SPrediction Config");
            Prediction.Initialize(spred);
            //menu.Add(jgsteal);
            menu.Attach();
        }

        public static bool EActivated
        {
            get
            {
                return ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState == 1 || EGameObject != null;
            }
        }

        public static bool HasPassive(AIHeroClient hero)
        {
            return hero.HasBuff("luxilluminatingfraulein");
        }

        public static float GetComboDamage(AIHeroClient target)
        {
            double dmg = 0;

            if (Q.IsReady())
            {
                dmg += Q.GetDamage(target);
            }

            if (E.IsReady())
            {
                dmg += E.GetDamage(target);
            }

            if (R.IsReady())
            {
                dmg += R.GetDamage(target);
            }

            return (float)dmg;
        }

        private static void AntiGapcloserOnOnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (!Menubase.lux_q.gap.Enabled)
            {
                return;
            }

            Q.SPredictionCast(sender, HitChance.Medium);
        }

        private static void AutoW()
        {
            if (!W.IsReady() || ObjectManager.Player.IsRecalling())
            {
                return;
            }

            foreach (
                var ally in from ally in ObjectManager.Get<AIHeroClient>().Where(x => x.IsAlly).Where(x => !x.IsDead)
                            let allyPercent = ally.Health / ally.MaxHealth * 100
                            let healthPercent = Menubase.lux_w.Wper.Value
                            where healthPercent >= allyPercent
                            select ally)
            {
                W.SPredictionCast(ally,HitChance.Medium);
                return;
            }
        }

        private static HitChance getQ()
        {
            var hitchance = HitChance.High;
            switch (Menubase.lux_hit.qhit.Index)
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

        private static HitChance getE()
        {
            var hitchance = HitChance.High;
            switch (Menubase.lux_hit.ehit.Index)
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

        private static void CastE(AIHeroClient target)
        {
            var ehit = getE();

            if (EActivated)
            {
                if (
                    !ObjectManager.Get<AIHeroClient>()
                         .Where(x => x.IsEnemy)
                         .Where(x => !x.IsDead)
                         .Where(x => x.IsValidTarget())
                         .Any(enemy => enemy.Distance(EGameObject.Position) < E.Width))
                {
                    return;
                }

                var isInAaRange = ObjectManager.Player.Distance(target) <= ObjectManager.Player.GetRealAutoAttackRange();

                if (isInAaRange && !HasPassive(target))
                {
                    E.Cast();
                }

                // Pop E if the target is out of AA range
                if (!isInAaRange)
                {
                    E.Cast();
                }
            }
            else
            {
                E.SPredictionCast(target, ehit);
            }
        }

        private static void CastQ(AIHeroClient target)
        {
            var qhit = getQ();
            var input = Q.GetSPrediction(target);
            var col = Q.GetCollisions(target.Position.ToVector2());
            var unit = col.Units;
            var minions = unit.Where(x => !(x is AIHeroClient)).Count(x => x.IsMinion);
            if (minions <= 1)
            {
                Q.SPredictionCast(target, qhit);
            }
        }

        private static HitChance getR()
        {
            var hitchance = HitChance.High;
            switch (Menubase.lux_hit.rhit.Index)
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

        private static void DoCombo()
        {
            var target = TargetSelector.GetTarget(Q.Range);

            if (target == null)
            {
                return;
            }

            var useQ = Menubase.lux_q.Q.Enabled;
            var useW = Menubase.lux_w.W.Enabled;
            var useE = Menubase.lux_e.E.Enabled;
            var useR = Menubase.lux_r.R.Enabled;
            var onlyR = Menubase.lux_r.Rkill.Enabled;

            var Rhit = getR();

            if (useQ && !HasPassive(target) && Q.IsReady())
            {
                CastQ(target);
            }

            if (useW && W.IsReady() && ObjectManager.Player.HealthPercent <= Menubase.lux_w.Wper)
            {
                W.Cast(Game.CursorPosRaw);
            }

            if (useE && E.IsReady() && !EActivated)
            {
                CastE(target);
            }
            if (useR && R.IsReady() && !onlyR && !Menubase.lux_r.rprecision.Enabled)
            {
                R.SPredictionCast(target, Rhit);
            }
            if (useR && R.IsReady() && !onlyR && Menubase.lux_r.rprecision.Enabled)
            {
                if(target.HasBuffOfType(BuffType.Slow) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Taunt))
                {
                    R.SPredictionCast(target, Rhit);
                }
            }
            if (!onlyR)
            {
                return;
            }
            var killable = ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) > target.Health;
            var rpred = R.GetSPrediction(target);
            if (killable && R.IsReady() && !Menubase.lux_r.rprecision.Enabled && useR)
            {
                R.SPredictionCast(target, Rhit);
            }
            if(killable && R.IsReady() && Menubase.lux_r.rprecision.Enabled && useR)
            {
                if (target.HasBuffOfType(BuffType.Slow) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Taunt))
                {
                    R.SPredictionCast(target, Rhit);
                }
            }
        }

        private static void DoHarass()
        {
            var target = TargetSelector.GetTarget(Q.Range);

            if (target == null)
            {
                return;
            }

            var useQ = Menubase.lux_q.Q.Enabled;
            var useE = Menubase.lux_e.E.Enabled;

            if (useQ && !HasPassive(target) && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                CastQ(target);
            }

            if (!useE || !E.IsReady())
            {
                return;
            }
            CastE(target);
        }

        private static void DoLaneClear()
        {
            var useQ = Menubase.lux_clear.clearQ.Enabled;
            var useE = Menubase.lux_clear.clearE.Enabled;

            var minionsQ = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).Cast<AIBaseClient>().ToList();
            var minionsE = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion()).Cast<AIBaseClient>().ToList();

            var efarmpos = E.GetCircularFarmLocation(new List<AIBaseClient>(minionsE), E.Width);
            var qfarmpos = Q.GetLineFarmLocation(new List<AIBaseClient>(minionsQ), Q.Width);

            if (useQ && Q.IsReady())
            {
                if (qfarmpos.MinionsHit >= Menubase.lux_clear.Qcount.Value)
                {
                    Q.Cast(qfarmpos.Position);
                }
            }

            if (useE && E.IsReady() && !EActivated)
            {
                if (efarmpos.MinionsHit >= Menubase.lux_clear.Ecount.Value)
                {
                    E.Cast(efarmpos.Position);
                }
            }
            else if (EActivated)
            {
                E.Cast();
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Harass:
                    DoHarass();
                    break;
                case OrbwalkerMode.Combo:
                    DoCombo();
                    break;

                case OrbwalkerMode.LaneClear:
                    DoLaneClear();
                    break;
            }
            Junglesteal();
            if (Menubase.lux_w.autoW.Enabled)
            {
                //AutoW();
            }
            if (Menubase.lux_w.autoW.Enabled)
            {
                KS();
            }
        }

        private static void KS()
        {
            if (!R.IsReady() || !Menubase.lux_ks.ksR.Enabled)
            {
                return;
            }
            var Rhit = getR();
            foreach (var enemy in
                ObjectManager.Get<AIHeroClient>()
                    .Where(x => x.IsValidTarget())
                    .Where(x => !x.IsZombie)
                    .Where(x => !x.IsDead)
                    .Where(enemy => R.GetDamage(enemy) > enemy.Health))
            {
                R.SPredictionCast(enemy, Rhit);
            }
        }

        private static void Junglesteal() //Criar menu
        {
            if (!R.IsReady() || !Menubase.lux_steal.steal.Enabled)
                return;
            if (Menubase.lux_steal.blue.Enabled)
            {
                var blueBuff =
                        ObjectManager.Get<AIMinionClient>()
                            .Where(x => x.Name == "SRU_Blue")
                            .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                            .FirstOrDefault(x => (x.IsAlly) || (x.IsEnemy));
                if (blueBuff != null)
                    R.CastOnUnit(blueBuff);

            }

            if (Menubase.lux_steal.red.Enabled)
            {
                var redBuff =
                    ObjectManager.Get<AIMinionClient>()
                        .Where(x => x.Name == "SRU_Red")
                        .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                        .FirstOrDefault(x => (x.IsAlly) || (x.IsEnemy));

                if (redBuff != null)
                    R.CastOnUnit(redBuff);
            }
            if (Menubase.lux_steal.baron.Enabled)
            {
                var Baron =
                    ObjectManager.Get<AIMinionClient>()
                        .Where(x => x.Name == "SRU_Baron")
                        .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                        .FirstOrDefault(x => (x.IsAlly) || (x.IsEnemy));

                if (Baron != null)
                    R.CastOnUnit(Baron);
            }
            if (Menubase.lux_steal.dragon.Enabled)
            {
                var Dragon =
                    ObjectManager.Get<AIMinionClient>()
                        .Where(x => x.Name == "SRU_Dragon")
                        .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                        .FirstOrDefault(x => (x.IsAlly) || (x.IsEnemy));

                if (Dragon != null)
                    R.CastOnUnit(Dragon);
            }
        }
    }
}
