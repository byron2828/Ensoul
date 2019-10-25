using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.Events;

using SebbyLib;
using Menu = EnsoulSharp.SDK.MenuUI.Menu;
using SharpDX;
namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Twitch : Program
    {
        private int count = 0, countE = 0;
        private float grabTime = Game.Time;

        public Twitch()
        {
            Q = new Spell(SpellSlot.Q, 0);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 1200);
            R = new Spell(SpellSlot.R, 975);

            W.SetSkillshot(0.25f, 100f, 1410f, false, false, SkillshotType.Circle);

            var wrapper = new Menu(Player.CharacterName, Player.CharacterName);

            var draw = new Menu("draw", "Draw");
            draw.Add(new MenuBool("notif", "Show notification & line", true, Player.CharacterName));
            draw.Add(new MenuBool("eRange", "E range", true, Player.CharacterName));
            draw.Add(new MenuBool("rRange", "R range", true, Player.CharacterName));
            draw.Add(new MenuBool("onlyRdy", "Draw only ready spells", true, Player.CharacterName));
            wrapper.Add(draw);

            var q = new Menu("QConfig", "Q Config");
            q.Add(new MenuSlider("countQ", "Auto Q if x enemies are going in your direction 0-disable", 3, 0, 5, Player.CharacterName));
            q.Add(new MenuBool("autoQ", "Auto Q in combo", true, Player.CharacterName));
            q.Add(new MenuBool("recallSafe", "Safe Q recall", true, Player.CharacterName));
            wrapper.Add(q);

            var w = new Menu("WConfig", "W Config");
            w.Add(new MenuBool("autoW", "AutoW", true, Player.CharacterName));
            wrapper.Add(w);

            var e = new Menu("EConfig", "E Config");
            e.Add(new MenuBool("Eks", "E ks", true, Player.CharacterName));
            e.Add(new MenuSlider("countE", "Auto E if x stacks & out range AA", 6, 0, 6, Player.CharacterName));
            e.Add(new MenuBool("5e", "Always E if 6 stacks", true, Player.CharacterName));
            e.Add(new MenuBool("jungleE", "Jungle ks E", true, Player.CharacterName));
            e.Add(new MenuBool("Edead", "Cast E before Twitch die", true, Player.CharacterName));
            wrapper.Add(e);

            var r = new Menu("RConfig", "R Config");

            r.Add(new MenuBool("Rks", "R KS out range AA", true, Player.CharacterName));
            r.Add(new MenuSlider("countR", "Auto R if x enemies (combo)", 3, 0, 5, Player.CharacterName));
            wrapper.Add(r);
            Config.Add(wrapper);

            Tick.OnTick += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AIBaseClient.OnDoCast += AIBaseClient_OnDoCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }


    
        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.Recall && Q.IsReady() && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("recallSafe").Enabled)
            {
                ObjectManager.Player.Spellbook.CastSpell(SpellSlot.Q);
                DelayAction.Add(200, () => ObjectManager.Player.Spellbook.CastSpell(SpellSlot.Recall));
                args.Process = false;
                return;
            }

        }

        private void OnUpdate(EventArgs args)
        {
            if (LagFree(0))
            {
                SetMana();
            }
            if (LagFree(1) && E.IsReady())
                LogicE();
            if (LagFree(2) && Q.IsReady() && !ObjectManager.Player.Spellbook.IsAutoAttack)
                LogicQ();
            if (LagFree(3) && Config[Player.CharacterName]["WConfig"].GetValue<MenuBool>("autoW").Enabled && W.IsReady() && !ObjectManager.Player.Spellbook.IsAutoAttack)
                LogicW();
            if (LagFree(4) && R.IsReady() && Combo)
                LogicR();
        }


        

        private void AIBaseClient_OnDoCast(AIBaseClient target, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("Edead").Enabled && E.IsReady() && target.IsEnemy && target.IsValidTarget(1500))
            {
                double dmg = 0;

                if (args.Target != null && args.Target.IsMe)
                {
                    dmg = dmg + E.GetDamage(target);
                }
                else
                {
                    var castArea = Player.Distance(args.End) * (args.End - Player.PreviousPosition).Normalized() + Player.PreviousPosition;
                    if (castArea.Distance(Player.PreviousPosition) < Player.BoundingRadius / 2)
                    {
                        dmg = dmg + E.GetDamage(target);
                    }
                }

                if (Player.Health - dmg < (Player.CountEnemyHeroesInRange(600) * Player.Level * 10))
                {
                    E.Cast();
                }
            }
        }

        private void LogicR()
        {
            var t = TargetSelector.GetTarget(R.Range);
            var r = Config[Player.CharacterName]["RConfig"] as Menu;
            if (t.IsValidTarget())
            {
                if (!t.InAutoAttackRange() && Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("Rks").Enabled && Player.GetAutoAttackDamage(t) * 4 > t.Health)
                    R.Cast();

                if (t.CountEnemyHeroesInRange(450) >= r.GetValue<MenuSlider>("countR").Value && 0 != r.GetValue<MenuSlider>("countR").Value)
                    R.Cast();
            }
        }

        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range);
            if (t.IsValidTarget())
            {

                if (Combo && Player.Mana > WMANA + RMANA + EMANA && (Player.GetAutoAttackDamage(t) * 2 < t.Health || !t.InAutoAttackRange()))
                    CastSpell(W, t);
                else if (!None && Player.Mana > RMANA + WMANA + EMANA)
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && !OktwCommon.CanMove(enemy)))
                        W.Cast(enemy, true);
                }
            }
        }

        private void LogicQ()
        {
            var q = Config[Player.CharacterName]["QConfig"] as Menu;
            if (Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("autoQ").Enabled && Combo && Player.Mana > RMANA + QMANA)
                Q.Cast();

            if (q.GetValue<MenuSlider>("countQ").Value == 0 || Player.Mana < RMANA + QMANA)
                return;

            var count = 0;
            foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(3000)))
            {
                List<Vector2> waypoints = enemy.Path.ToList().ToVector2();

                if (Player.Distance(waypoints.Last().ToVector3()) < 600)
                    count++;
            }

            if (count  >= q.GetValue<MenuSlider>("countQ").Value)
                Q.Cast();
        }

        private void LogicE()
        {
            var e = Config[Player.CharacterName]["EConfig"] as Menu;
            foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(E.Range) && enemy.HasBuff("TwitchDeadlyVenom") && OktwCommon.ValidUlt(enemy)))
            {
                if (Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("Eks").Enabled && E.GetDamage(enemy) > enemy.Health)
                {
                    Console.WriteLine("ks error");
                    E.Cast();
                }
                if (Player.Mana > RMANA + EMANA)
                {
                    int buffsNum = OktwCommon.GetBuffCount(enemy, "TwitchDeadlyVenom");
                    if (Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("5e").Enabled && buffsNum == 6)
                    {
                        E.Cast();
                    }
                    float buffTime = OktwCommon.GetPassiveTime(enemy, "TwitchDeadlyVenom");
                
                    if (!enemy.InAutoAttackRange() && (Player.PreviousPosition.Distance(enemy.PreviousPosition) > 950 || buffTime < 1) && 0 < e.GetValue<MenuSlider>("countE").Value && buffsNum >= e.GetValue<MenuSlider>("countE").Value)
                    {
                        E.Cast();
                    }
                }
            }
            JungleE();
        }

        private float passiveDmg(AIBaseClient target)
        {
            if (!target.HasBuff("TwitchDeadlyVenom"))
                return 0;
            float dmg = 6;
            if (Player.Level < 17)
                dmg = 5;
            if (Player.Level < 13)
                dmg = 4;
            if (Player.Level < 9)
                dmg = 3;
            if (Player.Level < 5)
                dmg = 2;
            float buffTime = OktwCommon.GetPassiveTime(target, "TwitchDeadlyVenom");
            return (dmg * OktwCommon.GetBuffCount(target, "TwitchDeadlyVenom") * buffTime) - target.HPRegenRate * buffTime;
        }

        private void JungleE()
        {
            if (!Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("jungleE").Enabled || Player.Mana < RMANA + EMANA || Player.Level == 1)
                return;

            var mobs = Cache.GetMinions(Player.PreviousPosition, E.Range, SebbyLib.MinionTeam.Neutral);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (mob.Health < 100)
                {
                    E.Cast();
                }
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
            WMANA = W.Instance.SData.ManaArray[Math.Max(0, Q.Level - 1)];
            EMANA = E.Instance.SData.ManaArray[Math.Max(0, Q.Level - 1)];

            if (!R.IsReady())
                RMANA = EMANA - Player.PARRegenRate * E.Instance.Cooldown;
            else
                RMANA = R.Instance.SData.ManaArray[Math.Max(0, Q.Level - 1)];
        }

        public static void drawText(string msg, AIHeroClient Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1], color, msg);
        }

        public static void drawText(string msg, Vector3 Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1] - 200, color, msg);
        }

        public static void drawText2(string msg, Vector3 Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1] - 200, color, msg);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            var onlyRdy = Config[Player.CharacterName]["draw"].GetValue<MenuBool>("onlyRdy");
            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("notif").Enabled)
            {
                if (Player.HasBuff("TwitchHideInShadows"))
                    drawText2("Q:  " + String.Format("{0:0.0}", OktwCommon.GetPassiveTime(Player, "TwitchHideInShadows")), Player.Position, System.Drawing.Color.Yellow);
                if (Player.HasBuff("twitchhideinshadowsbuff"))
                    drawText2("Q AS buff:  " + String.Format("{0:0.0}", OktwCommon.GetPassiveTime(Player, "twitchhideinshadowsbuff")), Player.Position, System.Drawing.Color.YellowGreen);
                if (Player.HasBuff("TwitchFullAutomatic"))
                    drawText2("R ACTIVE:  " + String.Format("{0:0.0}", OktwCommon.GetPassiveTime(Player, "TwitchFullAutomatic")), Player.Position, System.Drawing.Color.OrangeRed);

            }

            foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(2000) && enemy.HasBuff("TwitchDeadlyVenom")))
            {
                if (passiveDmg(enemy) > enemy.Health)
                    drawText("IS DEAD", enemy, System.Drawing.Color.Yellow);
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
