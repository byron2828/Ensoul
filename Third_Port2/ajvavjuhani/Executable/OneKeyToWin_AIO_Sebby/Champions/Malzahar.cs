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
using SPrediction;
using Menu = EnsoulSharp.SDK.MenuUI.Menu;
namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Malzahar : Program
    {
        private float Rtime = 0;

        public Malzahar()
        {
            Q = new Spell(SpellSlot.Q, 900);
            Q1 = new Spell(SpellSlot.Q, 900);
            W = new Spell(SpellSlot.W, 750);
            E = new Spell(SpellSlot.E, 650);
            R = new Spell(SpellSlot.R, 700);

            Q1.SetSkillshot(0.25f, 100, float.MaxValue, false, false,SkillshotType.Circle);
            Q.SetSkillshot(0.75f, 80, float.MaxValue, false, false,SkillshotType.Circle);
            W.SetSkillshot(1.2f, 230, float.MaxValue, false, false,SkillshotType.Circle);

            var wrapper = new Menu(Player.CharacterName, Player.CharacterName);

            var draw = new Menu("draw", "Draw");
            draw.Add(new MenuBool("noti", "Show notification & line", true, Player.CharacterName));
            draw.Add(new MenuBool("qRange", "Q range", true, Player.CharacterName));
            draw.Add(new MenuBool("wRange", "W range", true, Player.CharacterName));
            draw.Add(new MenuBool("eRange", "E range", true, Player.CharacterName));
            draw.Add(new MenuBool("rRange", "R range", true, Player.CharacterName));
            draw.Add(new MenuBool("onlyRdy", "Draw only ready spells", true, Player.CharacterName));
            wrapper.Add(draw);

            var q = new Menu("QConfig", "Q Config");
            q.Add(new MenuBool("autoQ", "Auto Q", true, Player.CharacterName));
            q.Add(new MenuBool("harassQ", "Harass Q", true, Player.CharacterName));
            q.Add(new MenuBool("intQ", "Interrupt spells Q", true, Player.CharacterName));
            q.Add(new MenuBool("gapQ", "Gapcloser Q", true, Player.CharacterName));
            wrapper.Add(q);

            var w = new Menu("WConfig", "W Config");
            w.Add(new MenuBool("autoW", "Auto W", true, Player.CharacterName));
            w.Add(new MenuBool("harassW", "Harass W", true, Player.CharacterName));
            wrapper.Add(w);

            var e = new Menu("EConfig", "E Config");
            e.Add(new MenuBool("autoE", "Auto E", true, Player.CharacterName));
            e.Add(new MenuBool("harassE", "Harass E", true, Player.CharacterName));
            e.Add(new MenuBool("harrasEminion", "Try harras E on minion", true, Player.CharacterName));
            wrapper.Add(e);

            var r = new Menu("RConfig", "R Config");
            r.Add(new MenuBool("autoR", "Auto R", true, Player.CharacterName));
            r.Add(new MenuBool("Rturrent", "Don't R under turret", true, Player.CharacterName));
            //r.Add(new MenuKeyBind("smartR", "Semi-manual cast R key", Keys.T, KeyBindType.Press, Player.CharacterName));
            var rgap = new Menu("rgap", "R Gap Closer");
            var rgaplist = new Menu("rgaplist", "Cast on enemy:");
            foreach (var enemy in GameObjects.EnemyHeroes)
               rgaplist.Add(new MenuBool("gapcloser" + enemy.CharacterName, enemy.CharacterName, true, Player.CharacterName));
            rgap.Add(rgaplist);
            r.Add(rgap);
            wrapper.Add(r);

            var misc = new Menu("MiscConfig", "Misc");
            misc.Add(new MenuKeyBind("useR", "Fast combo key", Keys.T, KeyBindType.Press, Player.CharacterName));
            var cast = new Menu("castat", "Fast cast at: ");
            foreach (var enemy in GameObjects.EnemyHeroes)
                cast.Add(new MenuBool("Ron" + enemy.CharacterName, enemy.CharacterName, true, Player.CharacterName));
            misc.Add(cast);
            wrapper.Add(misc);

            var farm = new Menu("farm", "Farm");
            farm.Add(new MenuBool("farmQ", "Lane clear Q", true, Player.CharacterName));
            farm.Add(new MenuBool("farmW", "Lane clear W", true, Player.CharacterName));
            farm.Add(new MenuBool("farmE", "Lane clear E", true, Player.CharacterName));

            farm.Add(new MenuBool("jungleE", "Jungle clear E", true, Player.CharacterName));
            farm.Add(new MenuBool("jungleQ", "Jungle clear Q", true, Player.CharacterName));
            farm.Add(new MenuBool("jungleW", "Jungle clear W", true, Player.CharacterName));
            wrapper.Add(farm);

            Config.Add(wrapper);

            Tick.OnTick += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnInterrupterSpell += OnInterrupterSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {

            if (args.Slot == SpellSlot.R && !Config[Player.CharacterName]["RConfig"].GetValue<MenuKeyBind>("smartR").Active)
            {
                var t = args.Target as AIHeroClient;
                if (t != null && t.Health - OktwCommon.GetIncomingDamage(t) > R.GetDamage(t) * 2.5)
                {
                    if (E.IsReady() && Player.Mana > RMANA + EMANA)
                    {
                        E.CastOnUnit(t);
                        args.Process = false;
                        return;
                    }

                    if (W.IsReady() && Player.Mana > RMANA + WMANA)
                    {
                        W.Cast(t.Position);
                        args.Process = false;
                        return;
                    }

                    if (Q.IsReady() && t.IsValidTarget(Q.Range) && Player.Mana > RMANA + QMANA)
                    {
                        Q1.Cast(t);
                        args.Process = false;
                        return;
                    }

                }
                if(R.IsReady() && t.IsValidTarget())
                     Rtime = Game.Time;
                
            }
        }

        private void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {

            

            if (Q.IsReady() && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("gapQ").Enabled && sender.IsValidTarget(Q.Range))
            {
                Q.Cast(args.EndPosition);
            }
            else if (R.IsReady() && Config[Player.CharacterName]["RConfig"]["rgap"]["rgaplist"].GetValue<MenuBool>("gapcloser" + sender.CharacterName).Enabled && sender.IsValidTarget(R.Range))
            {
                R.CastOnUnit(sender);
            }
        }

        private static void OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (!Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("intQ").Enabled || !Q.IsReady())
                return;

            if (sender.IsValidTarget(Q.Range))
            {
                 Q.Cast(sender);
            }
        }

        private void OnUpdate(EventArgs args)
        {
            if (Player.IsCastingImporantSpell() || Game.Time - Rtime < 2.5 || Player.HasBuff("malzaharrsound"))
            {
                debug("R chaneling");
                OktwCommon.blockMove = true;
                OktwCommon.blockAttack = true;
                OktwCommon.blockSpells = true;
                //Orbwalking.Attack = false;
                //Orbwalking.Move = false;
                return;
            }
            else
            {
                OktwCommon.blockSpells = false;
                OktwCommon.blockMove = false;
                OktwCommon.blockAttack = false;
                //Orbwalking.Attack = true;
                //Orbwalking.Move = true;
            }

            

            if (R.IsReady() && Config[Player.CharacterName]["MiscConfig"].GetValue<MenuKeyBind>("useR").Active)
            {
                if (Config[Player.CharacterName]["MiscConfig"].GetValue<MenuKeyBind>("useR").Active)
                {
                    var t = TargetSelector.GetTarget(R.Range);
                    if (t.IsValidTarget(R.Range) && Config[Player.CharacterName]["MiscConfig"]["castat"].GetValue<MenuBool>("Ron" + t.CharacterName).Enabled && OktwCommon.ValidUlt(t))
                    {
                        R.CastOnUnit(t);
                        return;
                    }
                }
                else if (Config[Player.CharacterName]["RConfig"].GetValue<MenuKeyBind>("smartR").Active)
                {
                    var t = TargetSelector.GetTarget(R.Range);
                    if (t.IsValidTarget(R.Range) && OktwCommon.ValidUlt(t))
                    {
                        R.CastOnUnit(t);
                        
                    }
                }
            }

            if (LagFree(0))
            {
                SetMana();
                Jungle();
            }

            if (LagFree(1) && E.IsReady() && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("autoE").Enabled)
                LogicE();
            if (Program.LagFree(2) && Q.IsReady() && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("autoQ").Enabled)
                LogicQ();
            if (Program.LagFree(3) && W.IsReady() && Config[Player.CharacterName]["WConfig"].GetValue<MenuBool>("autoW").Enabled)
                LogicW();
            if (Program.LagFree(4) && R.IsReady() && Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("autoR").Enabled)
                LogicR();
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range);
            if (t.IsValidTarget())
            {
                var qDmg = OktwCommon.GetKsDamage(t, Q) + BonusDmg(t);

                if (qDmg > t.Health)
                    CastSpell(Q, t);

                if (R.IsReady() && t.IsValidTarget(R.Range))
                {
                    return;
                }
                if (Combo && Player.Mana > RMANA + QMANA)
                    CastSpell(Q, t);
                else if (Harass && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("harassQ").Enabled && Player.Mana > RMANA + EMANA + WMANA + EMANA)
                    CastSpell(Q, t);

                if (Player.Mana > RMANA + QMANA)
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                        Q.Cast(enemy);
                }
            }
            else if (LaneClear && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("farmQ").Enabled)
            {
                var allMinions = Cache.GetMinions(Player.PreviousPosition, Q.Range);
                var farmPos = Q.GetCircularFarmLocation(allMinions, 150);
                if (farmPos.MinionsHit >= 2)
                    Q.Cast(farmPos.Position);
            }
        }

        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range);
            if (t.IsValidTarget())
            {
                var qDmg = Q.GetDamage(t);
                var wDmg = OktwCommon.GetKsDamage(t, W) + BonusDmg(t) ;
                if (wDmg > t.Health)
                {
                    W.Cast(Player.Position.Extend(t.Position,450));
                }
                else if (wDmg + qDmg > t.Health && Player.Mana > QMANA + EMANA)
                    W.Cast(Player.Position.Extend(t.Position, 450));
                else if (Combo && Player.Mana > RMANA + WMANA)
                    W.Cast(Player.Position.Extend(t.Position, 450));
                else if (Harass && Config[Player.CharacterName]["WConfig"].GetValue<MenuBool>("harassW").Enabled &&  !Player.IsUnderEnemyTurret() && Player.Mana > RMANA + WMANA + EMANA + QMANA + WMANA && OktwCommon.CanHarass())
                    W.Cast(Player.Position.Extend(t.Position, 450));
            }
            else if (LaneClear && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("farmW").Enabled)
            {
                var allMinions = Cache.GetMinions(Player.PreviousPosition, W.Range);
                var farmPos = W.GetCircularFarmLocation(allMinions, W.Width);
                if (farmPos.MinionsHit >= 2)
                    W.Cast(farmPos.Position);
            }
        }

        private void LogicE()
        {
            var t = TargetSelector.GetTarget(E.Range);
            if (t.IsValidTarget())
            {
                var eDmg = OktwCommon.GetKsDamage(t, E) + BonusDmg(t);
                var wDmg = W.GetDamage(t);

                if (eDmg > t.Health)
                    E.CastOnUnit(t);
                else if (W.IsReady() && wDmg + eDmg > t.Health && Player.Mana > WMANA + EMANA)
                    E.CastOnUnit(t);
                else if (R.IsReady() && W.IsReady() && wDmg + eDmg + R.GetDamage(t) > t.Health && Player.Mana > WMANA + EMANA + RMANA)
                    E.CastOnUnit(t);
                if (Combo && Player.Mana > RMANA + EMANA)
                    E.CastOnUnit(t);
                else if (Harass && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("harassE").Enabled && Player.Mana > RMANA + EMANA + WMANA + EMANA)
                    E.CastOnUnit(t);
            }
            else if (LaneClear && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("farmE").Enabled)
            {
                var allMinions = Cache.GetMinions(Player.PreviousPosition, E.Range);
                if (allMinions.Count >= 2)
                {
                    foreach (var minion in allMinions.Where(minion => minion.IsValidTarget(E.Range) && minion.Health < E.GetDamage(minion) && !minion.HasBuff("AlZaharMaleficVisions")))
                    {
                        E.CastOnUnit(minion);
                    }
                }
            }
            else if (Program.Harass && Player.Mana > RMANA + EMANA + WMANA + EMANA && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("harrasEminion").Enabled)
            {
                var te = TargetSelector.GetTarget(E.Range + 400);
                if (te.IsValidTarget())
                {
                    var allMinions = Cache.GetMinions(Player.PreviousPosition, E.Range);
                    foreach (var minion in allMinions.Where(minion => minion.IsValidTarget(E.Range) && minion.Health < E.GetDamage(minion) && te.Distance(minion.Position) < 500 && !minion.HasBuff("AlZaharMaleficVisions")))
                    {
                        E.CastOnUnit(minion);
                    }
                }
            }
        }

        private void LogicR()
        {
            if (Player.IsUnderEnemyTurret() && Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("Rturrent").Enabled)
                return;
            if (Player.CountEnemyHeroesInRange(800) < 3)
                return;

            foreach (var t in GameObjects.EnemyHeroes.Where(t => t.IsValidTarget(R.Range)))
            { 
                var totalComboDamage = R.GetDamage(t) * 2.5;

                totalComboDamage += E.GetDamage(t);

                if (W.IsReady() && Player.Mana > RMANA + WMANA)
                {
                    totalComboDamage += Q.GetDamage(t);
                }

                if (Player.Mana > RMANA + QMANA)
                    totalComboDamage += Q.GetDamage(t);

                if (totalComboDamage > t.Health - OktwCommon.GetIncomingDamage(t) && OktwCommon.ValidUlt(t))
                {
                    R.CastOnUnit(t);
                }
            }
        }

        private void Jungle()
        {
            if (LaneClear && Player.Mana > RMANA + EMANA)
            {
                var mobs = Cache.GetMinions(Player.PreviousPosition, 600, SebbyLib.MinionTeam.Neutral);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (W.IsReady() && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("jungleW").Enabled)
                    {
                        W.Cast(mob.PreviousPosition);
                        return;
                    }

                    if (Q.IsReady() && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("jungleQ").Enabled)
                    {
                        Q.Cast(mob.PreviousPosition);
                        return;
                    }

                    if (E.IsReady() && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("jungleE").Enabled && mob.HasBuff("brandablaze"))
                    {
                        E.Cast(mob);
                        return;
                    }
                }
            }
        }

        private int CountMinionsInRange(float range, Vector3 pos)
        {
            
            var minions = MinionManager.GetMinions(pos, range, MinionManager.MinionTypes.All, MinionManager.MinionTeam.Enemy, MinionManager.MinionOrderTypes.MaxHealth);
            int count = 0;
            foreach (var minion in minions)
            {
                count++;
            }
            return count;
        }

        private float BonusDmg(AIHeroClient target)
        {
            return (float)Player.CalculateDamage(target, DamageType.Magical, (target.MaxHealth * 0.08) - (target.HPRegenRate * 5));
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

            public new static void drawLine(Vector3 pos1, Vector3 pos2, int bold, System.Drawing.Color color)
            {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
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

            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("noti").Enabled && R.IsReady())
            {
              
            }
        }
    }
}
