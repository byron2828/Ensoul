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
    class Darius : Program
    {
        public Darius()
        {
            Q = new Spell(SpellSlot.Q, 400);
            W = new Spell(SpellSlot.W, 145);
            E = new Spell(SpellSlot.E, 540);
            R = new Spell(SpellSlot.R, 460);

            E.SetSkillshot(0.01f, 100f, float.MaxValue, false, false, SkillshotType.Line);

            var wrapper = new Menu(Player.CharacterName, Player.CharacterName);

            var draw = new Menu("draw", "Draw");
            draw.Add(new MenuBool("qRange", "Q range", false, Player.CharacterName));
            draw.Add(new MenuBool("eRange", "E range", false, Player.CharacterName));
            draw.Add(new MenuBool("rRange", "R range", true, Player.CharacterName));
            draw.Add(new MenuBool("onlyRdy", "Draw only ready spells", true, Player.CharacterName));
            wrapper.Add(draw);

            var qo = new Menu("Qoption", "Q option");
            qo.Add(new MenuBool("Harass", "Harass Q", true, Player.CharacterName));
            qo.Add(new MenuBool("qOutRange", "Auto Q only out range AA", true, Player.CharacterName));
            wrapper.Add(qo);


            var e = new Menu("EConfig", "E Config");
            foreach (var enemy in GameObjects.EnemyHeroes)
                e.Add(new MenuBool("Use E on", "Eon" + enemy.CharacterName, true, Player.CharacterName));
            wrapper.Add(e);

            var r = new Menu("Roption", "R Option");

            r.Add(new MenuBool("autoR", "Auto R", true, Player.CharacterName));
            r.Add(new MenuKeyBind("useR", "Semi-manual cast R key", Keys.T, KeyBindType.Press, Player.CharacterName)); //32 == space
            r.Add(new MenuBool("autoRbuff", "Auto R if darius execute multi cast time out ", true, Player.CharacterName));
            r.Add(new MenuBool("autoRdeath", "Auto R if darius execute multi cast and under 10 % hp", true, Player.CharacterName));
            wrapper.Add(r);


            var farm = new Menu("farm", "Farm");
            farm.Add(new MenuBool("farmW", "Farm W", true, Player.CharacterName));
            farm.Add(new MenuBool("farmQ", "Farm Q", true, Player.CharacterName));

            wrapper.Add(farm);

            Config.Add(wrapper);



            Tick.OnTick += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            //Orbwalker.OnAction += Orbwalker_OnAction;
            Interrupter.OnInterrupterSpell += OnInterrupterSpell;
        }

        private void OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (E.IsReady() && sender.IsValidTarget(E.Range))
                E.Cast(sender);
        }




        private void OnUpdate(EventArgs args)
        {
            if (R.IsReady() && Config[Player.CharacterName]["Roption"].GetValue<MenuKeyBind>("useR").Active)
            {
                var targetR = TargetSelector.GetTarget(R.Range);
                if (targetR.IsValidTarget())
                    R.Cast(targetR, true);
            }

            if (LagFree(0))
            {
                SetMana();
            }

            if (LagFree(1) && W.IsReady())
                LogicW();
            if (LagFree(2) && Q.IsReady() &&  !Player.IsWindingUp)
                LogicQ();
            if (LagFree(3) && E.IsReady())
                LogicE();
            if (LagFree(4) && R.IsReady() && Config[Player.CharacterName]["Roption"].GetValue<MenuBool>("autoR").Enabled)
                LogicR();
        }

        private void LogicW()
        {
            if (!Player.IsWindingUp && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("farmW").Enabled && LaneClear)
            {
                var minions = Cache.GetMinions(Player.Position, Player.AttackRange);

                int countMinions = 0;

                foreach (var minion in minions.Where(minion => minion.Health < W.GetDamage(minion)))
                {
                    countMinions++;
                }

                if (countMinions > 0)
                    W.Cast();
            }
        }

        private void LogicE()
        {
            if (Player.Mana > RMANA + EMANA)
            {
                var target = TargetSelector.GetTarget(E.Range);
                if (target.IsValidTarget() && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("Eon" + target.CharacterName).Enabled && Player.IsUnderEnemyTurret() || Combo)
                //Config.Item("Eon" + target.ChampionName, true).GetValue<bool>() && ((Player.UnderTurret(false) && !Player.UnderTurret(true)) || Program.Combo) )
                {
                    if (!target.InAutoAttackRange())
                    {
                        E.Cast(target);
                    }
                }
            }
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range);
            if (t.IsValidTarget())
            {
                if (!Config[Player.CharacterName]["Qoption"].GetValue<MenuBool>("qOutRange").Enabled || t.InAutoAttackRange())
                {
                    if (Player.Mana > RMANA + QMANA && Combo)
                        Q.Cast();
                    else if (Harass && Player.Mana > RMANA + QMANA + EMANA + WMANA && Config[Player.CharacterName]["Qoption"].GetValue<MenuBool>("Harras").Enabled && Config[Player.CharacterName]["Qoption"].GetValue<MenuBool>("Harass" + t.CharacterName).Enabled)
                        Q.Cast();
                }

                if (!R.IsReady() && OktwCommon.GetKsDamage(t, Q) > t.Health)
                    Q.Cast();
            }

            else if (Config[Player.CharacterName]["farm"].GetValue<MenuBool>("farmW").Enabled && LaneClear)
            {
                var minionsList = Cache.GetMinions(Player.PreviousPosition, Q.Range);

                if (minionsList.Any(x => Player.Distance(x.PreviousPosition) > 300 && x.Health < Q.GetDamage(x) * 0.6))
                    Q.Cast();

            }
        }

        private void LogicR()
        {
            var targetR = TargetSelector.GetTarget(R.Range);
            if (targetR.IsValidTarget() && OktwCommon.ValidUlt(targetR) && Config[Player.CharacterName]["Roption"].GetValue<MenuBool>("autoRbuff").Enabled)
            {
                var buffTime = OktwCommon.GetPassiveTime(Player, "dariusexecutemulticast");
                if ((buffTime < 2 || (Player.HealthPercent < 10 && Config[Player.CharacterName]["Roption"].GetValue<MenuBool>("autoRdeath").Enabled)) && buffTime > 0)
                    R.Cast(targetR, true);
            }

            foreach (var target in GameObjects.EnemyHeroes.Where(target => target.IsValidTarget(R.Range) && OktwCommon.ValidUlt(target)))
            {
                var dmgR = OktwCommon.GetKsDamage(target, R);

                if (target.HasBuff("dariushemo"))
                    dmgR += R.GetDamage(target) * target.GetBuff("dariushemo").Count * 0.2f;

                if (dmgR > target.Health)
                {
                    R.Cast(target);
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
                RMANA = QMANA - Player.PARRegenRate * Q.Instance.Cooldown;
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
                        Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
                }
                else
                    Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);

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
}