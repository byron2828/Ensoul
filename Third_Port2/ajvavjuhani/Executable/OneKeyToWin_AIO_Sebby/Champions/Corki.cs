using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.Events;
using System.Windows.Forms;

using SebbyLib;

using SharpDX;

using Menu = EnsoulSharp.SDK.MenuUI.Menu;
namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Corki : Program
    {
        public Corki()
        {
            Q = new Spell(SpellSlot.Q, 825);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 1230);

            Q.SetSkillshot(0.3f, 200f, 1000f, false, false, SkillshotType.Circle);
            R.SetSkillshot(0.2f, 40f, 2000f, true, false, SkillshotType.Line);

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
            q.Add(new MenuBool("harassQ", "Q harass", true, Player.CharacterName));
            wrapper.Add(q);

            var w = new Menu("WConfig", "W Config");
            w.Add(new MenuBool("nktdE", "NoKeyToDash", true, Player.CharacterName));
            wrapper.Add(w);

            var e = new Menu("EConfig", "E Config");
            e.Add(new MenuBool("autoE", "Auto E", true, Player.CharacterName));
            e.Add(new MenuBool("harassE", "E harass", true, Player.CharacterName));
            wrapper.Add(e);

            var r = new Menu("RConfig", "R Config");
            r.Add(new MenuBool("autoR", "Auto R", true, Player.CharacterName));
            r.Add(new MenuSlider("Rammo", "Minimum R ammo harass", 3, 0, 6, Player.CharacterName));
            r.Add(new MenuBool("minionR", "Try R on minion", true, Player.CharacterName));
            r.Add(new MenuKeyBind("useR", "Semi-manual cast R key", Keys.T, KeyBindType.Press, Player.CharacterName));
            wrapper.Add(r);

            var farm = new Menu("farm", "Farm");
            farm.Add(new MenuSlider("RammoLC", "Minimum R ammo Lane clear", 3, 0, 6, Player.CharacterName));
            farm.Add(new MenuSlider("LCMana", "Lane clear minimum Mana", 80, 30, 100, Player.CharacterName));
            farm.Add(new MenuBool("farmQ", "LaneClear + jungle Q", true, Player.CharacterName));
            farm.Add(new MenuBool("farmR", "LaneClear + jungle  R", true, Player.CharacterName));

            wrapper.Add(farm);

            Config.Add(wrapper);
            Tick.OnTick += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalker.OnAction += Orbwalker_OnAction;
        }

        private void Orbwalker_OnAction(object sender, OrbwalkerActionArgs args)
        {
            if (E.IsReady() && Sheen() && args.Target.IsValidTarget())
            {
                if (Combo && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("autoE").Enabled && Player.Mana > EMANA + RMANA)
                    E.Cast(args.Target.Position);
                if (Harass && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("harassE").Enabled && Player.Mana > EMANA + RMANA + QMANA && OktwCommon.CanHarass())
                    E.Cast(args.Target.Position);
                if (!Q.IsReady() && !R.IsReady() && args.Target.Health < Player.FlatPhysicalDamageMod * 2)
                    E.Cast();
            }
        }

        private void OnUpdate(EventArgs args)
        {
            if (LagFree(0))
            {
                SetMana();
                farm();
            }
            if (LagFree(1) && Q.IsReady() && !ObjectManager.Player.IsWindingUp && Sheen())
                LogicQ();
            if (LagFree(2) && Combo && W.IsReady())
                LogicW();
            if (LagFree(4) && R.IsReady() && !ObjectManager.Player.IsWindingUp && Sheen() && !ObjectManager.Player.IsWindingUp)
                LogicR();
        }

        private void LogicR()
        {
            float rSplash = 150;
            if (bonusR)
            {
                rSplash = 300;
            }

            var t = TargetSelector.GetTarget(R.Range);

            if (t.IsValidTarget())
            {
                var rDmg = OktwCommon.GetKsDamage(t, R);
                var qDmg = Q.GetDamage(t);
                var r = Config[Player.CharacterName]["RConfig"] as Menu;
                if (rDmg * 2 > t.Health)
                    CastR(R, t);
                else if (t.IsValidTarget(Q.Range) && qDmg + rDmg > t.Health)
                    CastR(R, t);
                if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Ammo > 1)
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(R.Range) && enemy.CountEnemyHeroesInRange(rSplash) > 1))
                        t = enemy;

                    if (Combo && Player.Mana > RMANA * 3)
                    {
                        CastR(R, t);
                    }
                    else if (Harass && Player.Mana > RMANA + EMANA + QMANA + WMANA && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Ammo >= r.GetValue<MenuSlider>("Rammo").Value && OktwCommon.CanHarass())
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(R.Range) && Config["harass"].GetValue<MenuBool>("harass" + t.CharacterName).Enabled))///////////
                            CastR(R, enemy);
                    }

                    if (!None && Player.Mana > RMANA + QMANA + EMANA)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(R.Range) && !OktwCommon.CanMove(enemy)))
                            CastR(R, t);
                    }
                }
            }
        }

        private void CastR(Spell R, AIHeroClient t)
        {
            Program.CastSpell(R, t);
            if (Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("minionR").Enabled)
            {
                // collision + predictio R
                var poutput = R.GetPrediction(t);
                var col = poutput.CollisionObjects.Count(ColObj => ColObj.IsEnemy && ColObj.IsMinion && !ColObj.IsDead);

                //hitchance
                var prepos = SpellPrediction.GetPrediction(t, 0.4f);

                if (col == 0 && (int)prepos.Hitchance < 5)
                    return;

                float rSplash = 140;
                if (bonusR)
                    rSplash = 290f;

                var minions = Cache.GetMinions(Player.PreviousPosition, R.Range - rSplash);
                foreach (var minion in minions.Where(minion => minion.Distance(poutput.CastPosition) < rSplash))
                {
                    R.Cast(minion);
                    return;
                }
            }
        }

        private void LogicW()
        {
            var dashPosition = Player.Position.Extend(Game.CursorPos, W.Range);

            if (Game.CursorPos.Distance(Player.Position) > Player.AttackRange + Player.BoundingRadius * 2 && Config[Player.CharacterName]["WConfig"].GetValue<MenuBool>("nktdE").Enabled && Player.Mana > RMANA + WMANA - 10)
            {
                W.Cast(dashPosition);
            }
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range);
            if (t.IsValidTarget())
            {
                if (Combo && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("autoQ").Enabled && Player.Mana > RMANA + QMANA)
                    CastSpell(Q, t);
                else if (Harass && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("harassQ").Enabled && Config["harass"].GetValue<MenuBool>("harass" + t.CharacterName).Enabled && Player.Mana > RMANA + EMANA + WMANA + RMANA && OktwCommon.CanHarass())
                    CastSpell(Q, t);
                else
                {
                    var qDmg = OktwCommon.GetKsDamage(t, Q);
                    var rDmg = R.GetDamage(t);
                    if (qDmg > t.Health)
                        Q.Cast(t);
                    else if (rDmg + qDmg > t.Health && Player.Mana > RMANA + QMANA)
                        CastSpell(Q, t);
                    else if (rDmg + 2 * qDmg > t.Health && Player.Mana > QMANA + RMANA * 2)
                        CastSpell(Q, t);
                }

                if (!None && Player.Mana > RMANA + WMANA + EMANA)
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                        Q.Cast(enemy, true, true);
                }
            }
        }
        public void farm()
        {
            if (Program.LaneClear && !ObjectManager.Player.IsWindingUp && Sheen())
            {
                var mobs = Cache.GetMinions(Player.PreviousPosition, Q.Range, SebbyLib.MinionTeam.Neutral);
                if (mobs.Count > 0 && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                {
                    var mob = mobs[0];
                    if (Q.IsReady() && Config[Player.CharacterName]["Farm"].GetValue<MenuBool>("farmQ").Enabled)
                    {
                        Q.Cast(mob);
                        return;
                    }

                    if (R.IsReady() && Config[Player.CharacterName]["Farm"].GetValue<MenuBool>("farmR").Enabled)
                    {
                        R.Cast(mob);
                        return;
                    }
                }

                if (LaneClear)
                {
                    var minions = Cache.GetMinions(Player.PreviousPosition, Q.Range);
                    var r = Config[Player.CharacterName]["RConfig"] as Menu;
                    var farm = Config[Player.CharacterName]["farm"] as Menu;

                    if (R.IsReady() && Config[Player.CharacterName]["Farm"].GetValue<MenuBool>("farmR").Enabled && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Ammo >= r.GetValue<MenuSlider>("Rammo").Value)
                    {
                        var rfarm = R.GetCircularFarmLocation(minions, 100);
                        if (rfarm.MinionsHit >= farm.GetValue<MenuSlider>("RammoLC").Value)///////////////////////////////////////////////////////////
                        {
                            R.Cast(rfarm.Position);
                            return;
                        }
                    }
                    if (Q.IsReady() && Config[Player.CharacterName]["Farm"].GetValue<MenuBool>("farmQ").Enabled)
                    {
                        var qfarm = Q.GetCircularFarmLocation(minions, Q.Width);
                        if (qfarm.MinionsHit >= farm.GetValue<MenuSlider>("LCMinions").Value)
                        {
                            Q.Cast(qfarm.Position);
                            return;
                        }
                    }
                }
            }
        }

        private bool Sheen()
        {
            var target = Orbwalker.GetTarget();

            if (target.IsValidTarget() && Player.HasBuff("sheen"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool bonusR { get { return Player.HasBuff("corkimissilebarragecounterbig"); } }

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
            if (Config[Player.CharacterName]["WConfig"].GetValue<MenuBool>("nktdE").Enabled)
            {
                if (Game.CursorPos.Distance(Player.Position) > Player.AttackRange + Player.BoundingRadius * 2)
                    drawText("dash: ON ", Player.Position, System.Drawing.Color.Red);
                else
                    drawText("dash: OFF ", Player.Position, System.Drawing.Color.GreenYellow);
            }
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

            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("noti").Enabled)
            {
                var tr = TargetSelector.GetTarget(R.Range);

                if (tr.IsValidTarget() && R.IsReady())
                {
                    var rDamage = R.GetDamage(tr);
                    if (rDamage > tr.Health)
                    {
                        //Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + tr.CharacterName + " have: " + tr.Health + " hp");
                        //drawLine(tr.Position, Player.Position, 10, System.Drawing.Color.Yellow);
                    }
                }

                var tw = TargetSelector.GetTarget(W.Range);

                if (tw.IsValidTarget())
                {
                    //if (Q.GetDamage(tw) > tw.Health)
                        //Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q can kill: " + tw.CharacterName + " have: " + tw.Health + " hp");
                }
            }
        }
    }
}
