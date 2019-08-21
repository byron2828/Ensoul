using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SharpDX;
using SebbyLib;

using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;



using Menu = EnsoulSharp.SDK.MenuUI.Menu;
namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Vaynee : Program
    {
        

        public Vaynee()
        {
            Q = new Spell(SpellSlot.Q, 300);
            E = new Spell(SpellSlot.E, 670);
            W = new Spell(SpellSlot.E, 670);
            R = new Spell(SpellSlot.R, 3000);

            //E.SetTargetted(0.25f, 2200f);

            var wrapper = new Menu(Player.CharacterName, Player.CharacterName);

            var draw = new Menu("draw", "Draw");
            draw.Add(new MenuBool("onlyRdy", "Draw only ready spells", true, Player.CharacterName));
            draw.Add(new MenuBool("qRange", "Q range", true, Player.CharacterName));
            draw.Add(new MenuBool("eRange2", "E push position", true, Player.CharacterName));
            wrapper.Add(draw);

            var q = new Menu("QConfig", "Q Config");
             q.Add(new MenuBool("autoQ", "Auto Q", true, Player.CharacterName));
            q.Add(new MenuSlider("Qstack", "Q at X stack", 2, 0, 3, Player.CharacterName));
            q.Add(new MenuBool("QE", "try Q + E ", true, Player.CharacterName));
            q.Add(new MenuBool("Qonly", "Q only after AA", true, Player.CharacterName));
            wrapper.Add(q);
            //Dash = new Core.OKTWdash(Q);

            var e = new Menu("EConfig", "E Config");
            e.Add(new MenuBool("gapE", "Enable", true, Player.CharacterName));

            foreach (var enemy in GameObjects.EnemyHeroes)
                e.Add(new MenuBool("gap" + enemy.CharacterName, enemy.CharacterName, true, Player.CharacterName));
            foreach (var enemy in GameObjects.EnemyHeroes)
                e.Add(new MenuBool("stun" + enemy.CharacterName, enemy.CharacterName, true, Player.CharacterName));
      

           
            e.Add(new MenuKeyBind("useE", "OneKeyToCast E closest person", Keys.T, KeyBindType.Press, Player.CharacterName)); //32 == space
            e.Add(new MenuBool("Eks", "E KS", true, Player.CharacterName));
            e.Add(new MenuBool("Ecombo", "E combo only", false, Player.CharacterName));
            wrapper.Add(e);
            var r = new Menu("RConfig", "R Config");
            r.Add(new MenuBool("autoR", "Auto R", false, Player.CharacterName));
            r.Add(new MenuBool("visibleR", "Unvisable block AA ", true, Player.CharacterName));
            r.Add(new MenuBool("autoQR", "Auto Q when R active ", true, Player.CharacterName));
            wrapper.Add(r);

            var farm = new Menu("farm", "Farm");
            farm.Add(new MenuBool("farmQ", "Q farm helper", true, Player.CharacterName));
            farm.Add(new MenuBool("farmQjungle", "Q jungle", true, Player.CharacterName));
            wrapper.Add(farm);

            Config.Add(wrapper);

            Dash = new Core.OKTWdash(Q);

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterrupterSpell += OnInterrupterSpell;
          
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private void OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (E.IsReady() && sender.IsValidTarget(E.Range))
                E.Cast(sender);
        }

        private void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            //var target = gapcloser.Sender;

            if (E.IsReady() && sender.IsValidTarget(E.Range) && (Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("gapE").Enabled) && (Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("gap" + sender.CharacterName).Enabled))
                E.Cast(sender);
        }



        public static Core.OKTWdash Dash;

        private double Wdmg(AIBaseClient target)
        {
            return target.MaxHealth * (4.5 + W.Level * 1.5) * 0.01;
        }

        private void Game_OnUpdate(EventArgs args)
        {
            var dashPosition = Player.Position.Extend(Game.CursorPosRaw, Q.Range);

            if (E.IsReady())
            {
                if (!(Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("Ecombo").Enabled) || Combo)
                    {
                    var ksTarget = Player;
                    foreach (var target in GameObjects.EnemyHeroes.Where(target => target.IsValidTarget(E.Range) && target.Path.Count() < 2))
                    {
                        if (CondemnCheck(Player.PreviousPosition, target) && (Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("stun" + target.CharacterName).Enabled))
                            E.Cast(target);
                        else if (Q.IsReady() && Dash.IsGoodPosition(dashPosition) && (Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("QE").Enabled) && CondemnCheck(dashPosition, target))
                        {
                            Q.Cast(dashPosition);
                            debug("Q + E");
                        }
                    }
                }
            }

            if (LagFree(1) && Q.IsReady())
            {
                if ((Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("autoQR").Enabled) && Player.HasBuff("vayneinquisition")  && Player.CountEnemyHeroesInRange(1500) > 0 && Player.CountEnemyHeroesInRange(670) != 1)
                {
                    var dashPos = Dash.CastDash();
                    if (!dashPos.IsZero)
                    {
                        Q.Cast(dashPos);
                    }
                }
                if (Combo && (Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("autoQ").Enabled) && !(Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("Qonly").Enabled))
                {
                    var t = TargetSelector.GetTarget(900);
                    //var orbT = Orbwalker.GetTarget() as AIHeroClient;

                    if (t.IsValidTarget() && !t.InAutoAttackRange() && t.Position.Distance(Game.CursorPosRaw) < t.Position.Distance(Player.Position) &&  !t.IsFacing(Player))
                    {
                        var dashPos = Dash.CastDash();
                        if (!dashPos.IsZero)
                        {
                            Q.Cast(dashPos);
                        }
                    }
                }
            }

            if (LagFree(2))
            {
                AIHeroClient bestEnemy = null;
                foreach (var target in GameObjects.EnemyHeroes.Where(target => target.IsValidTarget(E.Range)))
                {
                    if (target.IsValidTarget(250) && target.IsMelee)
                    {
                        if (Q.IsReady() && (Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("autoQ").Enabled))
                        {
                            var dashPos = Dash.CastDash(true);
                            if (!dashPos.IsZero)
                            {
                                Q.Cast(dashPos);
                            }
                        }
                        else if (E.IsReady() && Player.Health < Player.MaxHealth * 0.4)
                        {
                            E.Cast(target);
                            debug("push");
                        }
                    }
                    if (bestEnemy == null)
                        bestEnemy = target;
                    else if (Player.Distance(target.Position) < Player.Distance(bestEnemy.Position))
                        bestEnemy = target;
                }
                var e = Config[Player.CharacterName]["EConfig"];
                if (e.GetValue<MenuKeyBind>("useE").Active && bestEnemy != null)
                {
                    E.Cast(bestEnemy);
                }
            }

            if (LagFree(3) && R.IsReady() )
            {
                if ((Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("autoR").Enabled))
                {
                    if (Player.CountEnemyHeroesInRange(700) > 2)
                        R.Cast();
                    else if (Combo && Player.CountEnemyHeroesInRange(600) > 1)
                        R.Cast();
                    else if (Player.Health < Player.MaxHealth * 0.5 && Player.CountEnemyHeroesInRange(500) > 0)
                        R.Cast();
                }
            }
        }

        private bool CondemnCheck(Vector3 fromPosition, AIHeroClient target)
        {
            var prepos = E.GetPrediction(target);

            float pushDistance = 470;

            if (Player.PreviousPosition != fromPosition)
                pushDistance = 410 ;

            int radius = 250;
            var start2 = target.PreviousPosition;
            var end2 = prepos.CastPosition.Extend(fromPosition, -pushDistance);

            Vector2 start = start2.ToVector2();
            Vector2 end = end2.ToVector2();
            var dir = (end - start).Normalized();
            var pDir = dir.Perpendicular();

            var rightEndPos = end + pDir * radius;
            var leftEndPos = end - pDir * radius;


            var rEndPos = new Vector3(rightEndPos.X, rightEndPos.Y, ObjectManager.Player.Position.Z);
            var lEndPos = new Vector3(leftEndPos.X, leftEndPos.Y, ObjectManager.Player.Position.Z);


            var step = start2.Distance(rEndPos) / 10;
            for (var i = 0; i < 10; i++)
            {
                var pr = start2.Extend(rEndPos, step * i);
                var pl = start2.Extend(lEndPos, step * i);
                if (pr.IsWall() && pl.IsWall())
                    return true;
            }

            return false;
        }

        private int GetWStacks(AIBaseClient target)
        {
            foreach (var buff in target.Buffs)
            {
                if (buff.Name.ToLower() == "vaynesilvereddebuff")
                    return buff.Count;
            }
            return 0;
        }

        private List<Vector3> CirclePoint(float CircleLineSegmentN, float radius, Vector3 position)
        {
            List<Vector3> points = new List<Vector3>();
            for (var i = 1; i <= CircleLineSegmentN; i++)
            {
                var angle = i * 2 * Math.PI / CircleLineSegmentN;
                var point = new Vector3(position.X + radius * (float)Math.Cos(angle), position.Y + radius * (float)Math.Sin(angle), position.Z);
                points.Add(point);
            }
            return points;
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

            

            

            
        }
    }
}
