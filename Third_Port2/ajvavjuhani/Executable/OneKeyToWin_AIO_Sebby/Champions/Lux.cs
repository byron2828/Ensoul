using System;
using System.Collections.Generic;
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
    class Lux : Program
    {
        private Vector3 Epos = Vector3.Zero;
        private float DragonDmg = 0;
        private double DragonTime = 0;

        public Lux()
        {
            Q = new Spell(SpellSlot.Q, 1175);
            Q1 = new Spell(SpellSlot.Q, 1175);
            W = new Spell(SpellSlot.W, 1075);
            E = new Spell(SpellSlot.E, 1075);
            R = new Spell(SpellSlot.R, 3000);

            Q1.SetSkillshot(0.25f, 80f, 1200f, true,  false, SkillshotType.Line);
            Q.SetSkillshot(0.25f, 80f, 1200f, false,  false, SkillshotType.Line);
            W.SetSkillshot(0.25f, 110f, 1200f, false, false, SkillshotType.Line);
            E.SetSkillshot(0.3f, 250f, 1050f, false, false, SkillshotType.Circle);
            R.SetSkillshot(1.35f, 190f, float.MaxValue, false, false, SkillshotType.Line);

            var wrapper = new Menu(Player.CharacterName, Player.CharacterName);

            var draw = new Menu("draw", "Draw");
            draw.Add(new MenuBool("noti", "Show notification & line", true, Player.CharacterName));
            draw.Add(new MenuBool("qRange", "Q range", true, Player.CharacterName));
            draw.Add(new MenuBool("wRange", "W range", false, Player.CharacterName));
            draw.Add(new MenuBool("eRange", "E range", true, Player.CharacterName));
            draw.Add(new MenuBool("rRange", "R range", true, Player.CharacterName));
            draw.Add(new MenuBool("rRangeMini", "R range minimap", true, Player.CharacterName));
            draw.Add(new MenuBool("onlyRdy", "Draw only ready spells", true, Player.CharacterName));
            wrapper.Add(draw);

            var q = new Menu("QConfig", "Q Config");
            q.Add(new MenuBool("autoQ", "Auto Q", true, Player.CharacterName));
            q.Add(new MenuBool("harassQ", "Harass Q", true, Player.CharacterName));
            var qgap = new Menu("qgap", "Q Gap Closer");
            q.Add(new MenuBool("gapQ", "Auto Q Gap Closer", true, Player.CharacterName));
            var qgaplist = new Menu("qgaplist", "Cast on enemy:");
            foreach (var enemy in GameObjects.EnemyHeroes)
                qgaplist.Add(new MenuBool("QGCchampion" + enemy.CharacterName, enemy.CharacterName, true, Player.CharacterName));
            qgap.Add(qgaplist);
            q.Add(qgap);
            wrapper.Add(q);


            var e = new Menu("EConfig", "E Config");
            e.Add(new MenuBool("autoE", "Auto E", true, Player.CharacterName));
            e.Add(new MenuBool("harassE", "Harass E", true, Player.CharacterName));
            e.Add(new MenuBool("autoEcc", "Auto E only CC enemy", false, Player.CharacterName));
            e.Add(new MenuBool("autoEslow", "Auto E slow logic detonate", true, Player.CharacterName));
            e.Add(new MenuBool("autoEdet", "Only detonate if target in E ", false, Player.CharacterName));
            wrapper.Add(e);

            var w = new Menu("WConfig", "W Config");
            w.Add(new MenuSlider("Wdmg", "W dmg % hp", 10, 0, 100, Player.CharacterName));
            var wsh = new Menu("wsh", "Use shield on ally");
            foreach (var ally in GameObjects.AllyHeroes)
            {
                wsh.Add(new MenuBool("damage" + ally.CharacterName, "Damage incoming", true, Player.CharacterName));
                wsh.Add(new MenuBool("HardCC" + ally.CharacterName, "Hard CC", true, Player.CharacterName));
                wsh.Add(new MenuBool("Poison" + ally.CharacterName, "Poison", true, Player.CharacterName));
            }
            w.Add(wsh);
            wrapper.Add(w);

            var r = new Menu("RConfig", "R Config");
            r.Add(new MenuBool("autoR", "Auto R", true, Player.CharacterName));
            r.Add(new MenuBool("passiveR", "Include R passive damage", false, Player.CharacterName));
            r.Add(new MenuBool("Rcc", "R fast KS combo", true, Player.CharacterName));
            r.Add(new MenuSlider("RaoeCount", "R x enemies in combo [0 == off]", 3, 0, 5, Player.CharacterName));
            r.Add(new MenuSlider("hitchanceR", "Hit Chance R", 2, 0, 3, Player.CharacterName));
            r.Add(new MenuKeyBind("useR", "Semi-manual cast R key", Keys.T, KeyBindType.Press, Player.CharacterName));
            wrapper.Add(r);

            var jg = new Menu("Stealer", "R Jungle stealer");
            jg.Add(new MenuBool("Rjungle", "R Jungle stealer", true, Player.CharacterName));
            jg.Add(new MenuBool("Rdragon", "Dragon", true, Player.CharacterName));
            jg.Add(new MenuBool("Rbaron", "Baron", true, Player.CharacterName));
            jg.Add(new MenuBool("Rred", "Red", false, Player.CharacterName));
            jg.Add(new MenuBool("Rblue", "Blue", false, Player.CharacterName));
            jg.Add(new MenuBool("Rally", "Ally stealer", false, Player.CharacterName));
            wrapper.Add(jg);

            var farm = new Menu("farm", "Farm");
            farm.Add(new MenuBool("farmE", "Lane clear E", true, Player.CharacterName));
            farm.Add(new MenuBool("jungleQ", "Jungle clear Q", true, Player.CharacterName));
            farm.Add(new MenuBool("jungleE", "Jungle clear E", true, Player.CharacterName));
            wrapper.Add(farm);

            Config.Add(wrapper);

            Tick.OnTick += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AIBaseClient.OnDoCast += AIBaseClient_OnDoCast;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
        }

        private void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (Q.IsReady() && sender.IsValidTarget(Q.Range) && Config[Player.CharacterName]["QConfig"]["qgap"]["qgaplist"].GetValue<MenuBool>("QGCchampion" + sender.CharacterName).Enabled)
                Q.Cast(sender.PreviousPosition);
        }


        private void AIBaseClient_OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "LuxLightStrikeKugel")
            {
                Epos = args.End;
            }

            
        }

        private void OnUpdate(EventArgs args)
        {
            if (R.IsReady() )
            {
                if (Config[Player.CharacterName]["Stealer"].GetValue<MenuBool>("Rjungle").Enabled)
                {
                    KsJungle();
                }
                
                if (Config[Player.CharacterName]["RConfig"].GetValue<MenuKeyBind>("useR").Active)
                {
                    var t = TargetSelector.GetTarget(R.Range);
                    if (t.IsValidTarget())
                        R.Cast(t, true, true);
                }
            }
            else
                DragonTime = 0; 


            if (LagFree(0))
            {
                SetMana();
                Jungle();
            }

            if ((LagFree(4) || LagFree(1) || LagFree(3)) && W.IsReady() && !Player.IsRecalling())
                LogicW();
            if (LagFree(1) && Q.IsReady() && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("autoQ").Enabled)
                LogicQ();
            if (LagFree(2) && E.IsReady() && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("autoE").Enabled)
                LogicE();
            if (LagFree(3) && R.IsReady())
                LogicR();
        }
        private void LogicW()
        {
            foreach (var ally in GameObjects.AllyHeroes.Where(ally => ally.IsValid && !ally.IsDead  && Config[Player.CharacterName]["WConfig"]["wsh"].GetValue<MenuBool>("damage" + ally.CharacterName).Enabled && Player.PreviousPosition.Distance(ally.PreviousPosition) < W.Range))
            {
                double dmg = OktwCommon.GetIncomingDamage(ally);


                int nearEnemys = ally.CountEnemyHeroesInRange(800);

                if (dmg == 0 && nearEnemys == 0)
                    continue;

                int sensitivity = 20;
                
                double HpPercentage = (dmg * 100) / ally.Health;
                double shieldValue = 65 + W.Level * 25 + 0.35 * Player.FlatMagicDamageMod;

                if (Config[Player.CharacterName]["WConfig"]["wsh"].GetValue<MenuBool>("HardCC" + ally.CharacterName).Enabled && nearEnemys > 0 && HardCC(ally))
                {
                    W.CastOnUnit(ally);
                }
                else if (Config[Player.CharacterName]["WConfig"]["wsh"].GetValue<MenuBool>("Poison" + ally.CharacterName).Enabled && ally.HasBuffOfType(BuffType.Poison))
                {
                    W.Cast(W.GetPrediction(ally).CastPosition);
                }

                nearEnemys = (nearEnemys == 0) ? 1 : nearEnemys;
                var wdm = Config[Player.CharacterName]["WConfig"] as Menu;
                if (dmg > shieldValue)

                    W.Cast(W.GetPrediction(ally).CastPosition);
                else if (dmg > 100 + Player.Level * sensitivity)
                    W.Cast(W.GetPrediction(ally).CastPosition);
                else if (ally.Health - dmg < nearEnemys * ally.Level * sensitivity)
                    W.Cast(W.GetPrediction(ally).CastPosition);

                else if (HpPercentage >= wdm.GetValue<MenuSlider>("Wdmg").Value)
                    W.Cast(W.GetPrediction(ally).CastPosition);
            }
        }

        private void LogicQ()
        {
            foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(Q.Range) && E.GetDamage(enemy) + Q.GetDamage(enemy) + BonusDmg(enemy) > enemy.Health))
            {
                CastQ(enemy);
                return;
            }

            var t = Orbwalker.GetTarget() as AIHeroClient;
            if (!t.IsValidTarget())
                t = TargetSelector.GetTarget(Q.Range);
            if (t.IsValidTarget() && Config[Player.CharacterName]["QConfig"]["qgap"]["qgaplist"].GetValue<MenuBool>("QGCchampion" + t.CharacterName).Enabled)
            {
                if (Combo && Player.Mana > RMANA + QMANA)
                    CastQ(t);
                else if (Harass  && Config[Player.CharacterName]["QConfig"].GetValue<MenuBool>("harassQ").Enabled && Player.Mana > RMANA + EMANA + WMANA + EMANA)
                    CastQ(t);
                else if(OktwCommon.GetKsDamage(t,Q) > t.Health)
                    CastQ(t);

                foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                    CastQ(enemy);
            }
        }
        
        private void CastQ(AIBaseClient t)
        {
            var poutput = Q1.GetPrediction(t);
            var col = poutput.CollisionObjects.Count(ColObj => ColObj.IsEnemy && ColObj.IsMinion && !ColObj.IsDead); 
     
            if ( col < 4)
                CastSpell(Q, t);
        }

        private void LogicE()
        {
            if (Player.HasBuff("LuxLightStrikeKugel") && !None)
            {
                int eBig = Epos.CountEnemyHeroesInRange(350);
                if (Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("autoEslow").Enabled)
                {
                    int detonate = eBig - Epos.CountEnemyHeroesInRange(160);

                    if (detonate > 0 || eBig > 1)
                        E.Cast();
                }
                else if (Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("autoEdet").Enabled)
                {
                    if (eBig > 0)
                        E.Cast();
                }
                else
                {
                    E.Cast();
                }
            }
            else
            {
                var t = TargetSelector.GetTarget(E.Range);
                if (t.IsValidTarget() )
                {
                    if (!Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("autoEcc").Enabled)
                    {
                        if (Combo && Player.Mana > RMANA + EMANA)
                            CastSpell(E, t);
                        else if (Harass && OktwCommon.CanHarass() && Config[Player.CharacterName]["EConfig"].GetValue<MenuBool>("harassE").Enabled && Player.Mana > RMANA + EMANA + EMANA + RMANA)
                            CastSpell(E, t);
                        else if (OktwCommon.GetKsDamage(t, E) > t.Health)
                                CastSpell(E, t);
                    }

                    foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(E.Range) && !OktwCommon.CanMove(enemy)))
                        E.Cast(enemy, true);
                }
                else if (LaneClear && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("farmE").Enabled && Player.Mana > RMANA + WMANA)
                {
                    var minionList = Cache.GetMinions(Player.PreviousPosition, E.Range);
                    var farmPosition = E.GetCircularFarmLocation(minionList, E.Width);

                    if (farmPosition.MinionsHit >= 3)
                        E.Cast(farmPosition.Position);
                }
            }
        }

        private void LogicR()
        {
            var rao = Config[Player.CharacterName]["RConfig"] as Menu;
            if (Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("autoR").Enabled)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(target => target.IsValidTarget(R.Range) && target.CountAllyHeroesInRange(600) < 2 && OktwCommon.ValidUlt(target)))
                {
                    float Rdmg = OktwCommon.GetKsDamage(target, R);

                    

                    if (Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("passiveR").Enabled)
                    {
                        if (target.HasBuff("luxilluminatingfraulein"))
                            Rdmg += (float)Player.CalculateDamage(target, DamageType.Magical, 10 + (8 * Player.Level) + 0.2 * Player.FlatMagicDamageMod);
                        
                        if (Player.HasBuff("itemmagicshankcharge") && Player.GetBuff("itemmagicshankcharge").Count == 100)
                            Rdmg += (float)Player.CalculateDamage(target, DamageType.Magical, 100 + 0.1 * Player.FlatMagicDamageMod);
                    }
                    if (Rdmg > target.Health)
                    {
                        castR(target);
                        debug("R normal");
                    }
                    else if (!OktwCommon.CanMove(target) && Config[Player.CharacterName]["RConfig"].GetValue<MenuBool>("Rcc").Enabled && target.IsValidTarget(E.Range))
                    {
                        float dmgCombo = Rdmg;

                        if (E.IsReady())
                        {
                            var eDmg = E.GetDamage(target);
                            
                            if (eDmg > target.Health)
                                return;
                            else
                                dmgCombo += eDmg;
                        }

                        if (target.IsValidTarget(800))
                            dmgCombo += BonusDmg(target);

                        if (dmgCombo > target.Health)
                        {
                            R.CastIfWillHit(target, 2);
                            R.Cast(target);
                        }

                    }
                    
                    else if (Combo && rao.GetValue<MenuSlider>("RaoeCount").Value > 0)
                    {
                        R.CastIfWillHit(target, rao.GetValue<MenuSlider>("RaoeCount").Value);
                    }
                }
            }
        }

        private float BonusDmg(AIHeroClient target)
        {
            float damage = 10 + (Player.Level) * 8 + 0.2f * Player.FlatMagicDamageMod;
            if (Player.HasBuff("lichbane"))
            {
                damage += (Player.BaseAttackDamage * 0.75f) + ((Player.BaseAbilityDamage + Player.FlatMagicDamageMod) * 0.5f);
            }

            return (float)(Player.GetAutoAttackDamage(target) + Player.CalculateDamage(target, DamageType.Magical, damage));
        }

        private void castR(AIHeroClient target)
        {
            var rh = Config[Player.CharacterName]["RConfig"] as Menu;
            var inx = rh.GetValue<MenuSlider>("hitchanceR").Value;
            if (inx == 0)
            {
                R.Cast(R.GetPrediction(target).CastPosition);
            }
            else if (inx == 1)
            {
                R.Cast(target);
            }
            else if (inx == 2)
            {
                CastSpell(R, target);
            }
            else if (inx == 3)
            {
                List<Vector2> waypoints = target.Path.ToList().ToVector2();
                if ((Player.Distance(waypoints.Last<Vector2>().ToVector3()) - Player.Distance(target.Position)) > 400)
                {
                    CastSpell(R, target);
                }
            }
        }

        private void Jungle()
        {
            if (LaneClear && Player.Mana > RMANA + WMANA + RMANA + WMANA)
            {
                var mobs = Cache.GetMinions(Player.PreviousPosition, 600, SebbyLib.MinionTeam.All);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (Q.IsReady() && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("jungleQ").Enabled)
                    {
                        Q.Cast(mob.PreviousPosition);
                        return;
                    }
                    if (E.IsReady() && Config[Player.CharacterName]["farm"].GetValue<MenuBool>("jungleE").Enabled)
                    {
                        E.Cast(mob.PreviousPosition);
                        return;
                    }
                }
            }
        }

        private void KsJungle()
        {
            var mobs = Cache.GetMinions(Player.PreviousPosition, R.Range, SebbyLib.MinionTeam.Neutral);
            foreach (var mob in mobs)
            {
                //debug(mob.BaseSkinName);
                if (((mob.CharacterName == "SRU_Dragon" && Config[Player.CharacterName]["Stealer"].GetValue<MenuBool>("Rdragon").Enabled)
                    || (mob.CharacterName == "SRU_Baron" && Config[Player.CharacterName]["Stealer"].GetValue<MenuBool>("Rbaron").Enabled)
                    || (mob.CharacterName == "SRU_Red" && Config[Player.CharacterName]["Stealer"].GetValue<MenuBool>("Rred").Enabled)
                    || (mob.CharacterName == "SRU_Blue" && Config[Player.CharacterName]["Stealer"].GetValue<MenuBool>("Rblue").Enabled)
                    && (mob.CountAllyHeroesInRange(1000) == 0 || Config[Player.CharacterName]["Stealer"].GetValue<MenuBool>("Rally").Enabled)
                    && mob.Health < mob.MaxHealth
                    && mob.Distance(Player.Position) > 1000
                    ))
                {
                    if (DragonDmg == 0)
                        DragonDmg = mob.Health;

                    if (Game.Time - DragonTime > 3)
                    {
                        if (DragonDmg - mob.Health > 0)
                        {
                            DragonDmg = mob.Health;
                        }
                        DragonTime = Game.Time;
                    }
                    else
                    {
                        var DmgSec = (DragonDmg - mob.Health) * (Math.Abs(DragonTime - Game.Time) / 3);
                        //Program.debug("DS  " + DmgSec);
                        if (DragonDmg - mob.Health > 0)
                        {
                            var timeTravel = R.Delay;
                            var timeR = (mob.Health - R.GetDamage(mob)) / (DmgSec / 3);
                            //Program.debug("timeTravel " + timeTravel + "timeR " + timeR + "d " + R.GetDamage(mob));
                            if (timeTravel > timeR)
                                R.Cast(mob.Position);
                        }
                        else
                            DragonDmg = mob.Health;

                        //Program.debug("" + GetUltTravelTime(ObjectManager.Player, R.Speed, R.Delay, mob.Position));
                    }
                }
            }
        }


        private bool HardCC(AIHeroClient target)
        {
            
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Knockback) ||
                target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) ||
                target.IsStunned)
            {
                return true;

            }
            else
                return false;
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

        public new static void drawLine(Vector3 pos1, Vector3 pos2, int bold, System.Drawing.Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }

        private void Drawing_OnEndScene(EventArgs args)
        {

            if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("rRangeMini").Enabled)
            {
                if (Config[Player.CharacterName]["draw"].GetValue<MenuBool>("onlyRdy").Enabled)
                {
                    if(R.IsReady())
                        Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Aqua, 1);
                }
                else
                    Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Aqua, 1);
            }
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
            if (R.IsReady() && Config[Player.CharacterName]["draw"].GetValue<MenuBool>("noti").Enabled)
            {
                var t = TargetSelector.GetTarget(R.Range);

                if ( t.IsValidTarget() && R.GetDamage(t) > t.Health)
                {
                    Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.CharacterName + " have: " + t.Health + "hp");
                    drawLine(t.Position, Player.Position, 5, System.Drawing.Color.Red);
                }
            }
        }
    }
}
