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
    class KogMaw : Program
    {
        public bool attackNow = true;

        public KogMaw()
        {
            Q = new Spell(SpellSlot.Q, 1130);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 1280);
            R = new Spell(SpellSlot.R, 1800);

            Q.SetSkillshot(0.25f, 50f, 2000f, true, false, SkillshotType.Line);
            E.SetSkillshot(0.25f, 120f, 1400f, false, false, SkillshotType.Line);
            R.SetSkillshot(1.2f, 120f, float.MaxValue, false, false, SkillshotType.Circle);

            var wrapper = new Menu(Player.CharacterName, Player.CharacterName);

            var q = new Menu("QConfig", "Q Config");
            q.Add(new MenuBool("autoQ", "Auto Q", true, Player.CharacterName));
            q.Add(new MenuBool("harassQ", "Harass Q", true, Player.CharacterName));
            wrapper.Add(q);

            var e = new Menu("EConfig", "E Config");
            e.Add(new MenuBool("autoE", "Auto E", true, Player.CharacterName));
            e.Add(new MenuBool("harassE", "Harass E", true, Player.CharacterName));
            e.Add(new MenuBool("AGC", "AntiGapcloserE", true, Player.CharacterName));
            wrapper.Add(e);

            var w = new Menu("WConfig", "W Config");
            w.Add(new MenuBool("autoW", "Auto W", true, Player.CharacterName));
            w.Add(new MenuBool("harassW", "Harass W on max range", true, Player.CharacterName));
            wrapper.Add(w);

            var r = new Menu("RConfig", "R Config");
            r.Add(new MenuBool("autoR", "Auto R", true, Player.CharacterName));
            r.Add(new MenuSlider("RmaxHp", "Target max % HP", 50, 0, 100, Player.CharacterName));
            r.Add(new MenuSlider("comboStack", "Max combo stack R", 2, 0, 10, Player.CharacterName));
            r.Add(new MenuSlider("harasStack", "Max haras stack R", 1, 0, 40, Player.CharacterName));
            r.Add(new MenuBool("Rcc", "R cc", true, Player.CharacterName));
            r.Add(new MenuBool("Rslow", "R slow", true, Player.CharacterName));
            r.Add(new MenuBool("Raoe", "R aoe", true, Player.CharacterName));
            r.Add(new MenuBool("Raa", "R only out off AA range", true, Player.CharacterName));
            wrapper.Add(r);

            var draw = new Menu("draw", "Draw");
            draw.Add(new MenuBool("ComboInfo", "R killable info", true, Player.CharacterName));
            draw.Add(new MenuBool("qRange", "Q range", true, Player.CharacterName));
            draw.Add(new MenuBool("wRange", "W range", true, Player.CharacterName));
            draw.Add(new MenuBool("eRange", "E range", true, Player.CharacterName));
            draw.Add(new MenuBool("rRange", "R range", true, Player.CharacterName));
            draw.Add(new MenuBool("onlyRdy", "Draw only ready spells", true, Player.CharacterName));
            wrapper.Add(draw);

            var m = new Menu("misc", "Misc");
            m.Add(new MenuBool("AApriority", "AA priority over spell", true, Player.CharacterName));
            wrapper.Add(m);

            var farm = new Menu("farm", "Farm");
            farm.Add(new MenuBool("farmW", "LaneClear W", true, Player.CharacterName));
            farm.Add(new MenuBool("farmE", "LaneClear E", true, Player.CharacterName));
            farm.Add(new MenuBool("jungleW", "Jungle clear W", true, Player.CharacterName));
            farm.Add(new MenuBool("jungleE", "Jungle clear E", true, Player.CharacterName));
            //farm.GetValue<MenuSlider>("LCMinions").Value

            wrapper.Add(farm);

            Config.Add(wrapper);

            Tick.OnTick += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Orbwalker.OnAction += Orbwalker_OnAction;
            
        }

        private void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("AGC").Enabled && E.IsReady() && Player.Mana > RMANA + EMANA)
            {
                if (sender.IsValidTarget(E.Range))
                    E.Cast(args.EndPosition);
            }
            return;
        }

        private void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            attackNow = true;
            if (LaneClear && W.IsReady())
            {
                var minions = Cache.GetMinions(Player.PreviousPosition, 650);

                if (minions.Count >= 3)
                {
                    if (Config[Player.CharacterName]["Farm"].GetValue<MenuBool>("farmW").Enabled && minions.Count > 2)
                        W.Cast();
                }
            }
        }

        private void Orbwalker_OnAction(object sender, OrbwalkerActionArgs args)
        {
            attackNow = false;
        }

        private void OnUpdate(EventArgs args)
        {
            if (LagFree(0))
            {
                R.Range = 870 + 300 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level;
                W.Range = 650 + 30 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level;
                SetMana();
                Jungle();

            }
            if (LagFree(1) && E.IsReady() && !ObjectManager.Player.Spellbook.IsAutoAttack && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("autoE").Enabled)
                LogicE();

            if (LagFree(2) && Q.IsReady() && !ObjectManager.Player.Spellbook.IsAutoAttack && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("autoQ").Enabled)
                LogicQ();

            if (LagFree(3) && W.IsReady() && Config[Player.CharacterName]["WConfig"].GetValue<MenuBool>("autoW").Enabled)
                LogicW();

            if (LagFree(4) && R.IsReady() && !ObjectManager.Player.Spellbook.IsAutoAttack)
                LogicR();
            
        }
        private void Jungle()
        {
            if (LaneClear && Player.Mana > RMANA + QMANA)
            {
                var mobs = Cache.GetMinions(Player.PreviousPosition, 650, SebbyLib.MinionTeam.Neutral);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (E.IsReady() && Config[Player.CharacterName]["Farm"].GetValue<MenuBool>("jungleE").Enabled)
                    {
                        E.Cast(mob.PreviousPosition);
                        return;
                    }
                    else if (W.IsReady() && Config[Player.CharacterName]["Farm"].GetValue<MenuBool>("jungleW").Enabled)
                    {
                        W.Cast();
                        return;
                    }
                    
                }
            }
        }

        private void LogicR()
        {
            var r = Config[Player.CharacterName]["RConfig"];
            if (Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("autoR").Enabled)
            {
                var target = TargetSelector.GetTarget(R.Range);

                if (target.IsValidTarget(R.Range) && target.HealthPercent < r.GetValue<MenuSlider>("RmaxHp").Value && OktwCommon.ValidUlt(target))
                {
                    

                    if (Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("Raa").Enabled && target.InAutoAttackRange())
                        return;

                  
                    var countR = GetRStacks();

                    var Rdmg = R.GetDamage(target);
                    Rdmg = Rdmg + target.CountAllyHeroesInRange(500) * Rdmg;

                    if (R.GetDamage(target) > target.Health - OktwCommon.GetIncomingDamage(target))
                        CastSpell(R, target);
                    else if (Combo && Rdmg * 2 > target.Health && Player.Mana > RMANA * 3)
                        CastSpell(R, target);
                    else if (countR < r.GetValue<MenuSlider>("comboStack").Value + 2 && Player.Mana > RMANA * 3)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(R.Range) && !OktwCommon.CanMove(enemy)))
                        {
                                R.Cast(enemy, true);
                        }
                    }

                    if (target.HasBuffOfType(BuffType.Slow) && Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("Rslow").Enabled && countR < r.GetValue<MenuSlider>("comboStack").Value + 1 && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(R, target);
                    else if (Combo && countR < r.GetValue<MenuSlider>("comboStack").Value && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(R, target);
                    else if (Harass && countR < r.GetValue<MenuSlider>("harasStack").Value && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(R, target);
                }
            }
        }

        private void LogicW()
        {
            if (Player.CountEnemyHeroesInRange(W.Range) > 0)
            {
                if (Combo)
                    W.Cast();
                else if (Harass && Config[Player.CharacterName]["WConfig"].GetValue<MenuBool>("harrassW").Enabled && Player.CountEnemyHeroesInRange(Player.AttackRange) > 0)
                    W.Cast();
            }
        }

        private void LogicQ()
        {
            
            
                var t = TargetSelector.GetTarget(Q.Range);
                if (t.IsValidTarget())
                {
                    var qDmg = OktwCommon.GetKsDamage(t, Q);
                    var eDmg = E.GetDamage(t);
                    if (t.IsValidTarget(W.Range) && qDmg + eDmg > t.Health)
                        CastSpell(Q, t);
                    else if (Combo && Player.Mana > RMANA + QMANA * 2 + EMANA)
                        CastSpell(Q, t);
                    else if ((Harass && Player.Mana > RMANA + EMANA + QMANA * 2 + WMANA) && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("harrassQ").Enabled && !Player.IsUnderEnemyTurret())
                        CastSpell(Q, t);
                    else if ((Combo || Harass) && Player.Mana > RMANA + QMANA + EMANA)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                             Q.Cast(enemy, true);

                    }
                }
            
        }

        private void LogicE()
        {
            
            
                var t = TargetSelector.GetTarget(E.Range);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    var eDmg = OktwCommon.GetKsDamage(t, E);
                    if (eDmg > t.Health)
                        CastSpell(E, t);
                    else if (eDmg + qDmg > t.Health && Q.IsReady())
                        CastSpell(E, t);
                    else if (Combo && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(E, t);
                    else if (Harass && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("harrassE").Enabled && Player.Mana > RMANA + WMANA + EMANA + QMANA + EMANA)
                        CastSpell(E, t);
                    else if ((Combo || Harass) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(E.Range) && !OktwCommon.CanMove(enemy)))
                                E.Cast(enemy, true);
                    }
                }
                else if (LaneClear && Config[Player.CharacterName]["Farm"].GetValue<MenuBool>("farmE").Enabled)
                {
                    var minionList = Cache.GetMinions(Player.PreviousPosition, E.Range);
                    var farmPosition = E.GetLineFarmLocation(minionList, E.Width);

                    if (farmPosition.MinionsHit >= 3)
                        E.Cast(farmPosition.Position);
                }
            
        }

        

        private int GetRStacks()
        {
            foreach (var buff in ObjectManager.Player.Buffs)
            {
                if (buff.Name == "kogmawlivingartillerycost")
                    return buff.Count;
            }
            return 0;
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
            WMANA = W.Instance.SData.ManaArray[Math.Max(0, Q.Level - 1)];
            EMANA = E.Instance.SData.ManaArray[Math.Max(0, Q.Level - 1)];

            if (!R.IsReady())
                RMANA = QMANA - Player.PARRegenRate * Q.Instance.Cooldown;
            else
                RMANA = R.Instance.SData.ManaArray[Math.Max(0, Q.Level - 1)];
        }

        private void drawText(string msg, AIHeroClient Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1], color, msg);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            var onlyRdy = Config[Player.CharacterName]["draw"].GetValue<MenuBool>("onlyRdy");
            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("ComboInfo").Enabled)
            {
                var combo = "Harass";
                foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget()))
                {
                    if (R.GetDamage(enemy) > enemy.Health)
                    {
                        combo = "KILL R";
                        drawText(combo, enemy, System.Drawing.Color.GreenYellow);
                    }
                    else
                    {
                        combo = (int)(enemy.Health / R.GetDamage(enemy)) + " R";
                        drawText(combo, enemy, System.Drawing.Color.Red);
                    }
                }
            }
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
            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("rRange").Enabled)
            {
                if (onlyRdy)
                {
                    if (R.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1);
            }
        }
    }
}
