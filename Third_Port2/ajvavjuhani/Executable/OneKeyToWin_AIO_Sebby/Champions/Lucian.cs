using System;
using System.Linq;
using System.Windows.Forms;
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
    class Lucian : Program
    {
        private bool passRdy = false;
        private float castR = Game.Time;
        public static Core.OKTWdash Dash;

        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 675f);
            Q1 = new Spell(SpellSlot.Q, 900f);
            W = new Spell(SpellSlot.W, 1100f);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 1200f);
            R1 = new Spell(SpellSlot.R, 1200f);

            Q1.SetSkillshot(0.40f, 10f, float.MaxValue, true, false, SkillshotType.Line);
            Q.SetTargetted(0.25f, 1400f);
            W.SetSkillshot(0.30f, 80f, 1600f, true, false, SkillshotType.Line);
            R.SetSkillshot(0.1f, 110, 2800, true, false, SkillshotType.Line);
            R1.SetSkillshot(0.1f, 110, 2800, false, false, SkillshotType.Line);

            var wrapper = new Menu(Player.CharacterName, Player.CharacterName);


            var draw = new Menu("draw", "Draw");
            draw.Add(new MenuBool("noti", "Show notification & line", false, Player.CharacterName));
            draw.Add(new MenuBool("qRange", "Q range", false, Player.CharacterName));
            draw.Add(new MenuBool("wRange", "W range", false, Player.CharacterName));
            draw.Add(new MenuBool("eRange", "E range", false, Player.CharacterName));
            draw.Add(new MenuBool("rRange", "R range", false, Player.CharacterName));
            draw.Add(new MenuBool("onlyRdy", "Draw only ready spells", true, Player.CharacterName));
            wrapper.Add(draw);


            var q = new Menu("QConfig", "Q Config");
            q.Add(new MenuBool("autoQ", "Auto Q", true, Player.CharacterName));
            q.Add(new MenuBool("harassQ", "Use Q on minion", true, Player.CharacterName));
            wrapper.Add(q);

            var w = new Menu("WConfig", "W Config");
            w.Add(new MenuBool("autoW", "Auto W", true, Player.CharacterName));
            w.Add(new MenuBool("ignoreCol", "Ignore collision", true, Player.CharacterName));
            w.Add(new MenuBool("wInAaRange", "Cast only in AA range", true, Player.CharacterName));
            wrapper.Add(w);

            var e = new Menu("EConfig", "E Config");
            e.Add(new MenuBool("autoE", "Auto E", true, Player.CharacterName));
            e.Add(new MenuBool("slowE", "Auto SlowBuff E", true, Player.CharacterName));
            
            wrapper.Add(e);
           
            var r = new Menu("RConfig", "R Config");
            r.Add(new MenuBool("autoR", "Auto R", true, Player.CharacterName));
            r.Add(new MenuKeyBind("useR", "Semi-manual cast R key", Keys.T, KeyBindType.Press, Player.CharacterName));
            wrapper.Add(r);

            var farm = new Menu("farm", "Farm");
            farm.Add(new MenuBool("farmQ", "LaneClear Q", true, Player.CharacterName));
            farm.Add(new MenuBool("farmW", "LaneClear W", true, Player.CharacterName));

            wrapper.Add(farm);

            Config.Add(wrapper);

            Dash = new Core.OKTWdash(E);
            Tick.OnTick += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            //Orbwalking.AfterAttack += afterAttack;
            AIBaseClient.OnDoCast += AIBaseClient_OnDoCast;
            Spellbook.OnCastSpell +=Spellbook_OnCastSpell;
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E)
            {
                passRdy = true;
            }
        }
       
        private void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
        }

        private void AIBaseClient_OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "LucianW" || args.SData.Name == "LucianE" || args.SData.Name == "LucianQ")
                {
                    passRdy = true;
                }
                else
                    passRdy = false;

                if (args.SData.Name == "LucianR")
                    castR = Game.Time;
            }
        }

        private void OnUpdate(EventArgs args)
        {
            if ( (int)(Game.Time * 10) % 2 == 0)
            {
                //Console.WriteLine("chaneling");
                //Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosRaw);
            }

            if (R1.IsReady() && Game.Time - castR > 5 && Config[Player.CharacterName]["RConfig"].GetValue<MenuKeyBind>("useR").Active)
            {
                var t = TargetSelector.GetTarget(R.Range);
                if (t.IsValidTarget(R1.Range))
                {
                    R1.Cast(t);
                    return;
                }
            }
            if (LagFree(0))
            {
                SetMana();
                
            }
            if (LagFree(1) && Q.IsReady() && !passRdy && !SpellLock )
                LogicQ();
            if (LagFree(2) && W.IsReady() && !passRdy && !SpellLock && Config[Player.CharacterName]["WConfig"].GetValue<MenuBool>("autoW").Enabled)
                LogicW();
            if (LagFree(3) && E.IsReady() )
                LogicE();
            if (LagFree(4))
            {
                if (R.IsReady() && Game.Time - castR > 5 && Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("autoR").Enabled)
                    LogicR();

                if (!passRdy && !SpellLock)
                    farm();
            }
        }

        private double AaDamage(AIHeroClient target)
        {
            if (Player.Level > 12)
                return Player.GetAutoAttackDamage(target) * 1.3;
            else if (Player.Level > 6)
                return Player.GetAutoAttackDamage(target) * 1.4;
            else if (Player.Level > 0)
                return Player.GetAutoAttackDamage(target) * 1.5;
            return 0;
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range);
            var t1 = TargetSelector.GetTarget(Q1.Range);
            if (t.IsValidTarget(Q.Range))
            {
                if (OktwCommon.GetKsDamage(t, Q) + AaDamage(t) > t.Health)
                    Q.Cast(t);
                else if (Combo && Player.Mana > RMANA + QMANA)
                    Q.Cast(t);
                else if (Harass && Config[Player.CharacterName]["Harras" + t.CharacterName].GetValue<MenuBool>().Enabled && Player.Mana > RMANA + QMANA + EMANA + WMANA)
                    Q.Cast(t);
            }
            else if ((Harass || Combo) && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("harassQ").Enabled && t1.IsValidTarget(Q1.Range) && Config[Player.CharacterName]["Harras" + t.CharacterName].GetValue<MenuBool>().Enabled && Player.Distance(t1.PreviousPosition) > Q.Range + 100)
            {
                if (Combo && Player.Mana < RMANA + QMANA)
                    return;
                if (Harass && Player.Mana < RMANA + QMANA + EMANA + WMANA )
                    return;
                if (!OktwCommon.CanHarass())
                    return;
                var prepos = SpellPrediction.GetPrediction(t1, Q1.Delay); 
                if ((int)prepos.Hitchance < 5)
                    return;
                var distance = Player.Distance(prepos.CastPosition);
                var minions = Cache.GetMinions(Player.PreviousPosition, Q.Range);
                
                foreach (var minion in minions.Where(minion => minion.IsValidTarget(Q.Range)))
                {
                    if (prepos.CastPosition.Distance(Player.Position.Extend(minion.Position, distance)) < 25)
                    {
                        Q.Cast(minion);
                        return;
                    }
                }
            }
        }

        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range);
            if (t.IsValidTarget())
            {
                var qDmg = OktwCommon.GetKsDamage(t, Q);
                var wDmg = W.GetDamage(t);
                if (t.IsValidTarget(W.Range) && qDmg + wDmg > t.Health)
                    CastSpell(W, t);
                
                else if (Combo && Player.Mana > RMANA + QMANA * 2 + WMANA)
                    CastSpell(W, t);

                else if ((Harass && Player.Mana > RMANA + EMANA + QMANA * 2 + WMANA) && !Player.IsUnderEnemyTurret())
                    CastSpell(W, t);
                else if ((Combo || Harass) && Player.Mana > RMANA + QMANA + EMANA)
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && !OktwCommon.CanMove(enemy)))
                        W.Cast(enemy, true);

                }
            }














            
        }
        
        private void LogicR()
        {
            var t = TargetSelector.GetTarget(R.Range);

            if (t.IsValidTarget(R.Range) && t.CountAllyHeroesInRange(500) == 0 && OktwCommon.ValidUlt(t) && !t.InAutoAttackRange())
            {
                var rDmg = R.GetDamage(t) * (10 + 5 * R.Level);

                var tDis = Player.Distance(t.PreviousPosition);
                if (rDmg * 0.8 > t.Health && tDis < 700 && !Q.IsReady())
                    R.Cast(t, true, true);
                else if (rDmg * 0.7 > t.Health && tDis < 800)
                    R.Cast(t, true, true);
                else if (rDmg * 0.6 > t.Health && tDis < 900)
                    R.Cast(t, true, true);
                else if (rDmg * 0.5 > t.Health && tDis < 1000)
                    R.Cast(t, true, true);
                else if (rDmg * 0.4 > t.Health && tDis < 1100)
                    R.Cast(t, true, true);
                else if (rDmg * 0.3 > t.Health && tDis < 1200)
                    R.Cast(t, true, true);
                return;
            }
        }

        private void LogicE()
        {
            if (Player.Mana < RMANA + EMANA || !Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("autoE").Enabled)
                return;

            if (GameObjects.EnemyHeroes.Any(target => target.IsValidTarget(270) && target.IsMelee))
            {
                var dashPos = Dash.CastDash(true);
                if (!dashPos.IsZero)
                {
                    E.Cast(dashPos);
                }
            }
            else
            {
                if (!Combo || passRdy || SpellLock)
                    return;

                var dashPos = Dash.CastDash();
                if (!dashPos.IsZero)
                {
                    E.Cast(dashPos);
                }
            }
        }

        public void farm()
        {
            if (LaneClear && Player.Mana > RMANA + QMANA)
            {
                var mobs = Cache.GetMinions(Player.PreviousPosition, Q.Range, SebbyLib.MinionTeam.Neutral);
                if (mobs.Count > 0 )
                {
                    var mob = mobs[0];
                    if (Q.IsReady())
                    {
                        Q.Cast(mob);
                        return;
                    }

                    if (W.IsReady())
                    {
                        W.Cast(mob);
                        return;
                    }
                }

                if (LaneClear)
                {
                    
                    if (Q.IsReady() && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("farmQ").Enabled)
                    {
                        var minions = Cache.GetMinions(Player.PreviousPosition, Q1.Range);
                        foreach (var minion in minions)
                        {
                            var poutput = Q1.GetPrediction(minion);
                            var col = poutput.CollisionObjects;
                            
                            if (col.Count() > 2)
                            {
                                var minionQ = col.First();
                                if (minionQ.IsValidTarget(Q.Range))
                                {
                                    Q.Cast(minion);
                                    return;
                                }
                            }
                        }
                    }
                    if (W.IsReady() && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("farmW").Enabled)
                    {
                        var minions = Cache.GetMinions(Player.PreviousPosition, Q1.Range);
                        var Wfarm = W.GetCircularFarmLocation(minions, 150);
                        if (Wfarm.MinionsHit > 3 )
                            W.Cast(Wfarm.Position);
                    }
                }
            }
        }


        private bool SpellLock
        {
            get
            {
                if (Player.HasBuff("lucianpassivebuff"))
                    return true;
                else
                    return false;
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
                RMANA = QMANA - Player.PARRegenRate * Q.Instance.Cooldown;
            else
                RMANA = R.Instance.SData.ManaArray[Math.Max(0, R.Level - 1)];
        }

        public static void drawText(string msg, Vector3 Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1] - 200, color, msg);
        }

        private void Drawing_OnDraw(EventArgs args)
        {

            var onlyRdy = Config[Player.CharacterName]["draw"].GetValue<MenuBool>("onlyRdy");

            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("qRange").Enabled)
            {
                if (onlyRdy)
                {
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
                }
                else
                    Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
            }

            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("wRange").Enabled)
            {
                if (onlyRdy)
                {
                    if (W.IsReady())
                        Render.Circle.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Orange, 1);
                }
                else
                    Render.Circle.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Orange, 1);
            }

            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("eRange").Enabled)
            {
                if (onlyRdy)
                {
                    if (E.IsReady())
                        Render.Circle.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
                }
                else
                    Render.Circle.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
            }

            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("rRange").Enabled)
            {
                if (onlyRdy)
                {
                    if (R.IsReady())
                        Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Gray, 1);
                }
                else
                    Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Gray, 1);
            }
        }
    }
}
