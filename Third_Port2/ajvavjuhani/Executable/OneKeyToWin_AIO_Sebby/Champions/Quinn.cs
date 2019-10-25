using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.Events;

using SebbyLib;

using SharpDX;

using Menu = EnsoulSharp.SDK.MenuUI.Menu;
namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Quinn : Program
    {
        public Quinn()
        {
            Q = new Spell(SpellSlot.Q, 1000);
            E = new Spell(SpellSlot.E, 700);
            W = new Spell(SpellSlot.W, 2100);
            R = new Spell(SpellSlot.R, 550);

            Q.SetSkillshot(0.25f, 90f, 1550, true, false, SkillshotType.Line);
            E.SetTargetted(0.25f, 2000f);

            var wrapper = new Menu(Player.CharacterName, Player.CharacterName);

            var draw = new Menu("draw", "Draw");
            draw.Add(new MenuBool("noti", "Show notification & line", true, Player.CharacterName));
            draw.Add(new MenuBool("qRange", "Q range", true, Player.CharacterName));
            draw.Add(new MenuBool("wRange", "W range", true, Player.CharacterName));
            draw.Add(new MenuBool("eRange", "E range", true, Player.CharacterName));
            draw.Add(new MenuBool("onlyRdy", "Draw only ready spells", true, Player.CharacterName));
            wrapper.Add(draw);

            var q = new Menu("QConfig", "Q Config");
            q.Add(new MenuBool("autoQ", "Auto Q", true, Player.CharacterName));
            q.Add(new MenuBool("harassQ", "Harass Q", true, Player.CharacterName));
            wrapper.Add(q);

            var e = new Menu("EConfig", "E Config");
            e.Add(new MenuBool("autoE", "Auto E", true, Player.CharacterName));
            e.Add(new MenuBool("harassE", "Harass E", true, Player.CharacterName));
            e.Add(new MenuBool("AGC", "AntiGapcloser E", true, Player.CharacterName));
            e.Add(new MenuBool("Int", "Interrupter E", true, Player.CharacterName));
            var egap = new Menu("egap", "E Gap Closer");
            foreach (var enemy in GameObjects.EnemyHeroes)
                egap.Add(new MenuBool("gap" + enemy.CharacterName, enemy.CharacterName, true, Player.CharacterName));
            e.Add(egap);
            wrapper.Add(e);

            var w = new Menu("WConfig", "W Config");
            w.Add(new MenuBool("autoW", "Auto W", true, Player.CharacterName));
            wrapper.Add(w);

            var r = new Menu("RConfig", "R Config");
            r.Add(new MenuBool("autoR", "Auto R in shop", true, Player.CharacterName));
            wrapper.Add(r);

            var misc= new Menu("MiscConfig", "Misc");
            misc.Add(new MenuBool("focusP", "Focus marked enemy", true, Player.CharacterName));
            wrapper.Add(misc);

            var farm = new Menu("farm", "Farm");
            farm.Add(new MenuBool("farmP", "Attack marked minion first", true, Player.CharacterName));
            farm.Add(new MenuBool("farmQ", "Farm Q", true, Player.CharacterName));
            farm.Add(new MenuBool("jungleE", "Jungle clear E", true, Player.CharacterName));
            farm.Add(new MenuBool("jungleQ", "Jungle clear Q", true, Player.CharacterName));
            wrapper.Add(farm);

            Config.Add(wrapper);


            Tick.OnTick += OnUpdate;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser; ;
            Drawing.OnDraw += Drawing_OnDraw;
            //Orbwalking.AfterAttack += afterAttack;
            Interrupter.OnInterrupterSpell += OnInterrupterSpell;
            //AIBaseClient.OnDoCast += AIBaseClient_OnDoCast;
            Orbwalker.OnAction += Orbwalker_OnAction;
            //Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
        }

        private void Orbwalker_OnAction(object sender, OrbwalkerActionArgs args)
        {
            if(args.Type == OrbwalkerType.BeforeAttack && Config[Player.CharacterName]["MiscConfig"].GetValue<MenuBool>("focusP").Enabled && args.Target.HealthPercent > 40)
            {
                var orbTarget = args.Target as AIHeroClient;
                
                if (!orbTarget.HasBuff("quinnw"))
                {
                    var best = GameObjects.EnemyHeroes.FirstOrDefault(enemy => enemy.IsValidTarget() && enemy.InAutoAttackRange() && enemy.HasBuff("quinnw"));
                    if (best != null)
                        Orbwalker.ForceTarget = best;
                }
            }
            
            
            
                var t = TargetSelector.GetTarget(E.Range);
                if (E.IsReady() && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("autoE").Enabled && t.IsValidTarget(E.Range) && t.CountEnemyHeroesInRange(800) < 3)
                {
                    if (Combo && Player.Mana > RMANA + EMANA)
                        E.Cast(t);
                    else if (Harass && Player.Mana > RMANA + EMANA + QMANA + WMANA && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("harassE").Enabled && OktwCommon.CanHarass())
                    {
                        E.Cast(t);
                    }
                    else if (OktwCommon.GetKsDamage(t, E) > t.Health)
                        E.Cast(t);
                }
                if (Q.IsReady() && t.IsValidTarget(Q.Range))
                {
                    if (Combo && Player.Mana > RMANA + QMANA)
                        CastSpell(Q, t);
                    else if (Harass && Player.Mana > RMANA + EMANA + QMANA + WMANA && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("harassQ").Enabled && OktwCommon.CanHarass())
                    {
                        CastSpell(Q, t);
                    }
                    else if (OktwCommon.GetKsDamage(t, Q) > t.Health)
                        CastSpell(Q, t);

                    if (!None && Player.Mana > RMANA + QMANA + EMANA)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                            Q.Cast(enemy);
                    }
                }
            
            Jungle();
        }
        

        
        private void OnUpdate(EventArgs args)
        {
            if (LagFree(1))
                SetMana();
            if (LagFree(2) && Q.IsReady() && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("autoQ").Enabled)
                LogicQ();

            if (LagFree(4) && R.IsReady() && Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("autoR").Enabled)
                LogicR();
        }
        private static void OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (E.IsReady() && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("Int").Enabled && sender.IsValidTarget(E.Range))
                E.CastOnUnit(sender);
        }
        private void Jungle()
        {
            if (LaneClear && Player.Mana > RMANA + WMANA + RMANA + WMANA)
            {
                var mobs = Cache.GetMinions(Player.PreviousPosition, 700, SebbyLib.MinionTeam.Neutral);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (mob.HasBuff("QuinnW"))
                        return;

                    if (Q.IsReady() && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("jungleQ").Enabled)
                    {
                        Q.Cast(mob.PreviousPosition);
                        return;
                    }

                    if (E.IsReady() && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("jungleE").Enabled)
                    {
                        E.CastOnUnit(mob);
                        return;
                    }
                }
            }
        }
        private void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (E.IsReady() && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("AGC").Enabled && Config[Player.CharacterName]["EConfig"]["egap"].GetValue<MenuBool>("gap" + sender.CharacterName).Enabled)
            {
                
                if (sender.IsValidTarget(E.Range))
                {
                    E.Cast(args.EndPosition);
                }
            }
        }

        private void LogicR()
        {
            if (Player.InFountain() && R.Instance.Name == "QuinnR")
            {
                R.Cast();
            }
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range);
            if (t.IsValidTarget())
            {
                if (t.InAutoAttackRange() && t.HasBuff("quinnw"))
                    return;
                if (Combo && Player.Mana > RMANA + QMANA)
                    CastSpell(Q, t);
                else if (Harass && Player.Mana > RMANA + EMANA + QMANA + WMANA && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("harassQ").Enabled  && OktwCommon.CanHarass())
                {
                    CastSpell(Q, t);
                }
                else if (OktwCommon.GetKsDamage(t, Q) > t.Health)
                    CastSpell(Q, t);

                if (!None && Player.Mana > RMANA + QMANA + EMANA)
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                        Q.Cast(enemy);
                }
            }
            else if (LaneClear && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("farmQ").Enabled)
            {
                var minionList = Cache.GetMinions(Player.PreviousPosition, Q.Range - 150);
                var farmPosition = Q.GetCircularFarmLocation(minionList, 150);
                if (farmPosition.MinionsHit >= 2)
                    Q.Cast(farmPosition.Position);
            }
        }

        private void SetMana()
        {
            if ((Config["extraSet"].GetValue<MenuBool>("manaDisable").Enabled && Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Instance.SData.ManaArray[Math.Max(0, Q.Level - 1)];
            WMANA = W.Instance.SData.ManaArray[Math.Max(0, W.Level - 1)];
            EMANA = E.Instance.SData.ManaArray[Math.Max(0, E.Level - 1)];

            if (!R.IsReady())
                RMANA = WMANA - Player.PARRegenRate * W.Instance.Cooldown;
            else
                RMANA = R.Instance.SData.ManaArray[Math.Max(0, R.Level - 1)];
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            var onlyRdy = Config[Player.CharacterName]["draw"].GetValue<MenuBool>("onlyRdy");
            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("qRange").Enabled)
            {
                if (onlyRdy)
                {
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
            }
             if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("wRange").Enabled)
                {
                if (onlyRdy)
                {
                    if (W.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1);
            }
            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("eRange").Enabled)
            {
                if (onlyRdy)
                {
                    if (E.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
            }
        }
    }
}
