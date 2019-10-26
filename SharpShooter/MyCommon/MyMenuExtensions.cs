namespace SharpShooter.MyCommon
{
    #region

    using System.Linq;
    using System.Drawing;

    using EnsoulSharp;
    using EnsoulSharp.SDK;
    using EnsoulSharp.SDK.MenuUI;
    using EnsoulSharp.SDK.MenuUI.Values;
    using EnsoulSharp.SDK.Utility;

    using Keys = System.Windows.Forms.Keys;

    #endregion

    public static class MyMenuExtensions
    {
        public static Menu myMenu { get; set; }
        public static Menu AxeMenu { get; set; }
        public static Menu ComboMenu { get; set; }
        public static Menu HarassMenu { get; set; }
        public static Menu LaneClearMenu { get; set; }
        public static Menu JungleClearMenu { get; set; }
        public static Menu LastHitMenu { get; set; }
        public static Menu FleeMenu { get; set; }
        public static Menu KillStealMenu { get; set; }
        public static Menu MiscMenu { get; set; }
        public static Menu DrawMenu { get; set; }

        public class AxeOption
        {
            private static Menu axeMenu => AxeMenu;

            public static void AddMenu()
            {
                AxeMenu = new Menu("AxeSettings", "Axe Settings");
                myMenu.Add(AxeMenu);
            }

            public static void AddSeperator(string name)
            {
                axeMenu.Add(new MenuSeparator("Axe" + name, name));
            }

            public static void AddBool(string name, string defaultName, bool enabled = true)
            {
                axeMenu.Add(new MenuBool(name, defaultName, enabled));
            }

            public static void AddSlider(string name, string defaultName, int defaultValue, int minValue, int maxValue)
            {
                axeMenu.Add(new MenuSlider(name, defaultName, defaultValue, minValue, maxValue));
            }

            public static void AddKey(string name, string defaultName, Keys Keys, KeyBindType type, bool enabled = false)
            {
                axeMenu.Add(new MenuKeyBind(name, defaultName, Keys, type){ Active = enabled });
            }

            public static void AddList(string name, string defaultName, string[] values, int defaultValue = 0)
            {
                axeMenu.Add(new MenuList(name, defaultName, values, defaultValue));
            }

            public static void AddSliderBool(string name, string defaultName, int defaultValue, int minValue,
                int maxValue, bool enabled = false)
            {
                axeMenu.Add(new MenuSliderButton(name, defaultName, defaultValue, minValue, maxValue, enabled));
            }

            public static MenuBool GetBool(string name)
            {
                return axeMenu[name].GetValue<MenuBool>();
            }

            public static MenuSlider GetSlider(string name)
            {
                return axeMenu[name].GetValue<MenuSlider>();
            }

            public static MenuKeyBind GetKey(string name)
            {
                return axeMenu[name].GetValue<MenuKeyBind>();
            }

            public static MenuList GetList(string name)
            {
                return axeMenu[name].GetValue<MenuList>();
            }

            public static MenuSliderButton GetSliderBool(string name)
            {
                return axeMenu[name].GetValue<MenuSliderButton>();
            }
        }

        public class ComboOption
        {
            private static Menu comboMenu => ComboMenu;

            public static void AddMenu()
            {
                ComboMenu = new Menu("ComboSettings", "Combo Settings");
                myMenu.Add(ComboMenu);
            }

            public static bool UseQ
                =>
                    comboMenu["ComboQ"] != null &&
                    comboMenu["ComboQ"].GetValue<MenuBool>().Enabled;

            public static bool UseW
                =>
                    comboMenu["ComboW"] != null &&
                    comboMenu["ComboW"].GetValue<MenuBool>().Enabled;

            public static bool UseE
                =>
                    comboMenu["ComboE"] != null &&
                    comboMenu["ComboE"].GetValue<MenuBool>().Enabled;

            public static bool UseR
                =>
                    comboMenu["ComboR"] != null &&
                    comboMenu["ComboR"].GetValue<MenuBool>().Enabled;

            public static void AddSeperator(string name)
            {
                comboMenu.Add(new MenuSeparator("Combo" + name, name));
            }

            public static void AddQ(bool enabled = true)
            {
                comboMenu.Add(new MenuBool("ComboQ", "Use Q", enabled));
            }

            public static void AddW(bool enabled = true)
            {
                comboMenu.Add(new MenuBool("ComboW", "Use W", enabled));
            }

            public static void AddE(bool enabled = true)
            {
                comboMenu.Add(new MenuBool("ComboE", "Use E", enabled));
            }

            public static void AddR(bool enabled = true)
            {
                comboMenu.Add(new MenuBool("ComboR", "Use R", enabled));
            }

            public static void AddBool(string name, string defaultName, bool enabled = true)
            {
                comboMenu.Add(new MenuBool(name, defaultName, enabled));
            }

            public static void AddSlider(string name, string defaultName, int defaultValue, int minValue, int maxValue)
            {
                comboMenu.Add(new MenuSlider(name, defaultName, defaultValue, minValue, maxValue));
            }

            public static void AddKey(string name, string defaultName, Keys Keys, KeyBindType type, bool enabled = false)
            {
                comboMenu.Add(new MenuKeyBind(name, defaultName, Keys, type){Active = enabled });
            }

            public static void AddList(string name, string defaultName, string[] values, int defaultValue = 0)
            {
                comboMenu.Add(new MenuList(name, defaultName, values, defaultValue));
            }

            public static void AddSliderBool(string name, string defaultName, int defaultValue, int minValue,
                int maxValue, bool enabled = false)
            {
                comboMenu.Add(new MenuSliderButton(name, defaultName, defaultValue, minValue, maxValue, enabled));
            }

            public static MenuBool GetBool(string name)
            {
                return comboMenu[name].GetValue<MenuBool>();
            }

            public static MenuSlider GetSlider(string name)
            {
                return comboMenu[name].GetValue<MenuSlider>();
            }

            public static MenuKeyBind GetKey(string name)
            {
                return comboMenu[name].GetValue<MenuKeyBind>();
            }

            public static MenuList GetList(string name)
            {
                return comboMenu[name].GetValue<MenuList>();
            }

            public static MenuSliderButton GetSliderBool(string name)
            {
                return comboMenu[name].GetValue<MenuSliderButton>();
            }
        }

        public class HarassOption
        {
            private static Menu harassMenu => HarassMenu;

            public static void AddMenu()
            {
                HarassMenu = new Menu("HarassSettings", "Harass Settings");
                myMenu.Add(HarassMenu);
            }

            public static bool UseQ
                =>
                    harassMenu["HarassQ"] != null &&
                    harassMenu["HarassQ"].GetValue<MenuBool>().Enabled;

            public static bool UseW
                =>
                    harassMenu["HarassW"] != null &&
                    harassMenu["HarassW"].GetValue<MenuBool>().Enabled;

            public static bool UseE
                =>
                    harassMenu["HarassE"] != null &&
                    harassMenu["HarassE"].GetValue<MenuBool>().Enabled;

            public static bool UseR
                =>
                    harassMenu["HarassR"] != null &&
                    harassMenu["HarassR"].GetValue<MenuBool>().Enabled;

            public static void AddQ(bool enabled = true)
            {
                harassMenu.Add(new MenuBool("HarassQ", "Use Q", enabled));
            }

            public static void AddW(bool enabled = true)
            {
                harassMenu.Add(new MenuBool("HarassW", "Use W", enabled));
            }

            public static void AddE(bool enabled = true)
            {
                harassMenu.Add(new MenuBool("HarassE", "Use E", enabled));
            }

            public static void AddR(bool enabled = true)
            {
                harassMenu.Add(new MenuBool("HarassR", "Use R", enabled));
            }

            public static void AddTargetList()
            {
                harassMenu.Add(new MenuSeparator("HarassListSettings", "Harass Target List"));

                foreach (var target in GameObjects.EnemyHeroes)
                {
                    if (target != null)
                    {
                        harassMenu.Add(new MenuBool("HarassList" + target.CharacterName.ToLower(), target.CharacterName));
                    }
                }
            }

            public static bool GetHarassTargetEnabled(string name)
            {
                return harassMenu["HarassList" + name.ToLower()] != null &&
                       harassMenu["HarassList" + name.ToLower()].GetValue<MenuBool>().Enabled;
            }

            public static AIHeroClient GetTarget(float range)
            {
                return MyTargetSelector.GetTargets(range).FirstOrDefault(x => x.IsValidTarget(range) && GetHarassTargetEnabled(x.CharacterName));
            }

            public static void AddMana(int defalutValue = 30)
            {
                harassMenu.Add(new MenuSlider("HarassMana", "When Player ManaPercent >= x%", defalutValue, 1, 99));
            }

            public static bool HasEnouguMana(bool underTurret = false)
                =>
                    ObjectManager.Player.ManaPercent >= GetSlider("HarassMana").Value &&
                    (underTurret || !ObjectManager.Player.IsUnderEnemyTurret());

            public static void AddBool(string name, string defaultName, bool enabled = true)
            {
                harassMenu.Add(new MenuBool(name, defaultName, enabled));
            }

            public static void AddSlider(string name, string defaultName, int defaultValue, int minValue, int maxValue)
            {
                harassMenu.Add(new MenuSlider(name, defaultName, defaultValue, minValue, maxValue));
            }

            public static void AddSliderBool(string name, string defaultName, int defaultValue, int minValue,
                int maxValue, bool enabled = false)
            {
                harassMenu.Add(new MenuSliderButton(name, defaultName, defaultValue, minValue, maxValue, enabled));
            }


            public static void AddKey(string name, string defaultName, Keys Keys, KeyBindType type, bool enabled = false)
            {
                harassMenu.Add(new MenuKeyBind(name, defaultName, Keys, type){ Active = enabled });
            }

            public static void AddList(string name, string defaultName, string[] values, int defaultValue = 0)
            {
                harassMenu.Add(new MenuList(name, defaultName, values, defaultValue));
            }

            public static MenuBool GetBool(string name)
            {
                return harassMenu[name].GetValue<MenuBool>();
            }

            public static MenuSlider GetSlider(string name)
            {
                return harassMenu[name].GetValue<MenuSlider>();
            }

            public static MenuKeyBind GetKey(string name)
            {
                return harassMenu[name].GetValue<MenuKeyBind>();
            }

            public static MenuList GetList(string name)
            {
                return harassMenu[name].GetValue<MenuList>();
            }

            public static MenuSliderButton GetSliderBool(string name)
            {
                return harassMenu[name].GetValue<MenuSliderButton>();
            }
        }

        public class LaneClearOption
        {
            private static Menu laneClearMenu => LaneClearMenu;

            public static void AddMenu()
            {
                LaneClearMenu = new Menu("LaneClearSettings", "LaneClear Settings");
                myMenu.Add(LaneClearMenu);
            }

            public static bool UseQ
                =>
                    laneClearMenu["LaneClearQ"] != null &&
                    laneClearMenu["LaneClearQ"].GetValue<MenuBool>().Enabled;

            public static bool UseW
                =>
                    laneClearMenu["LaneClearW"] != null &&
                    laneClearMenu["LaneClearW"].GetValue<MenuBool>().Enabled;

            public static bool UseE
                =>
                    laneClearMenu["LaneClearE"] != null &&
                    laneClearMenu["LaneClearE"].GetValue<MenuBool>().Enabled;

            public static bool UseR
                =>
                    LaneClearMenu["LaneClearR"] != null &&
                    LaneClearMenu["LaneClearR"].GetValue<MenuBool>().Enabled;

            public static void AddQ(bool enabled = true)
            {
                laneClearMenu.Add(new MenuBool("LaneClearQ", "Use Q", enabled));
            }

            public static void AddW(bool enabled = true)
            {
                laneClearMenu.Add(new MenuBool("LaneClearW", "Use W", enabled));
            }

            public static void AddE(bool enabled = true)
            {
                laneClearMenu.Add(new MenuBool("LaneClearE", "Use E", enabled));
            }

            public static void AddR(bool enabled = true)
            {
                laneClearMenu.Add(new MenuBool("LaneClearR", "Use R", enabled));
            }

            public static void AddMana(int defalutValue = 60)
            {
                laneClearMenu.Add(new MenuSlider("LaneClearMana", "When Player ManaPercent >= x%", defalutValue, 1, 99));
            }

            public static bool HasEnouguMana(bool underTurret = false)
                =>
                    ObjectManager.Player.ManaPercent >= GetSlider("LaneClearMana").Value && MyManaManager.SpellFarm &&
                    (underTurret || !ObjectManager.Player.IsUnderEnemyTurret());

            public static void AddBool(string name, string defaultName, bool enabled = true)
            {
                laneClearMenu.Add(new MenuBool(name, defaultName, enabled));
            }

            public static void AddSlider(string name, string defaultName, int defaultValue, int minValue, int maxValue)
            {
                laneClearMenu.Add(new MenuSlider(name, defaultName, defaultValue, minValue, maxValue));
            }

            public static void AddSliderBool(string name, string defaultName, int defaultValue, int minValue,
                int maxValue, bool enabled = false)
            {
                laneClearMenu.Add(new MenuSliderButton(name, defaultName, defaultValue, minValue, maxValue, enabled));
            }


            public static void AddList(string name, string defaultName, string[] values, int defaultValue = 0)
            {
                laneClearMenu.Add(new MenuList(name, defaultName, values, defaultValue));
            }

            public static MenuBool GetBool(string name)
            {
                return laneClearMenu[name].GetValue<MenuBool>();
            }

            public static MenuSlider GetSlider(string name)
            {
                return laneClearMenu[name].GetValue<MenuSlider>();
            }

            public static MenuList GetList(string name)
            {
                return laneClearMenu[name].GetValue<MenuList>();
            }

            public static MenuSliderButton GetSliderBool(string name)
            {
                return laneClearMenu[name].GetValue<MenuSliderButton>();
            }
        }

        public class JungleClearOption
        {
            private static Menu jungleClearMenu => JungleClearMenu;

            public static void AddMenu()
            {
                JungleClearMenu = new Menu("JungleClearSettings", "JungleClear Settings");
                myMenu.Add(JungleClearMenu);
            }

            public static bool UseQ
                =>
                    jungleClearMenu["JungleClearQ"] != null &&
                    jungleClearMenu["JungleClearQ"].GetValue<MenuBool>().Enabled;

            public static bool UseW
                =>
                    jungleClearMenu["JungleClearW"] != null &&
                    jungleClearMenu["JungleClearW"].GetValue<MenuBool>().Enabled;

            public static bool UseE
                =>
                    jungleClearMenu["JungleClearE"] != null &&
                    jungleClearMenu["JungleClearE"].GetValue<MenuBool>().Enabled;

            public static bool UseR
                =>
                    jungleClearMenu["JungleClearR"] != null &&
                    jungleClearMenu["JungleClearR"].GetValue<MenuBool>().Enabled;

            public static void AddQ(bool enabled = true)
            {
                jungleClearMenu.Add(new MenuBool("JungleClearQ", "Use Q", enabled));
            }

            public static void AddW(bool enabled = true)
            {
                jungleClearMenu.Add(new MenuBool("JungleClearW", "Use W", enabled));
            }

            public static void AddE(bool enabled = true)
            {
                jungleClearMenu.Add(new MenuBool("JungleClearE", "Use E", enabled));
            }

            public static void AddR(bool enabled = true)
            {
                jungleClearMenu.Add(new MenuBool("JungleClearR", "Use R", enabled));
            }

            public static void AddBool(string name, string defaultName, bool enabled = true)
            {
                jungleClearMenu.Add(new MenuBool(name, defaultName, enabled));
            }

            public static void AddList(string name, string defaultName, string[] values, int defaultValue = 0)
            {
                jungleClearMenu.Add(new MenuList(name, defaultName, values, defaultValue));
            }

            public static void AddMana(int defalutValue = 30)
            {
                jungleClearMenu.Add(new MenuSlider("JungleClearMana", "When Player ManaPercent >= x%",
                    defalutValue, 1, 99));
            }

            public static void AddSlider(string name, string defaultName, int defaultValue, int minValue, int maxValue)
            {
                jungleClearMenu.Add(new MenuSlider(name, defaultName, defaultValue, minValue, maxValue));
            }

            public static void AddSliderBool(string name, string defaultName, int defaultValue, int minValue,
                int maxValue, bool enabled = false)
            {
                jungleClearMenu.Add(new MenuSliderButton(name, defaultName, defaultValue, minValue, maxValue, enabled));
            }

            public static bool HasEnouguMana(bool underTurret = false)
                =>
                    ObjectManager.Player.ManaPercent >= GetSlider("JungleClearMana").Value && MyManaManager.SpellFarm &&
                    (underTurret || !ObjectManager.Player.IsUnderEnemyTurret());

            public static MenuBool GetBool(string name)
            {
                return jungleClearMenu[name].GetValue<MenuBool>();
            }

            public static MenuSlider GetSlider(string name)
            {
                return jungleClearMenu[name].GetValue<MenuSlider>();
            }

            public static MenuList GetList(string name)
            {
                return jungleClearMenu[name].GetValue<MenuList>();
            }

            public static MenuSliderButton GetSliderBool(string name)
            {
                return jungleClearMenu[name].GetValue<MenuSliderButton>();
            }
        }

        public class LastHitOption
        {
            private static Menu lastHitMenu => LastHitMenu;

            public static void AddMenu()
            {
                LastHitMenu = new Menu("LastHitSettings", "LastHit Settings");
                myMenu.Add(LastHitMenu);
            }

            public static bool HasEnouguMana
                => ObjectManager.Player.ManaPercent >= GetSlider("LastHitMana").Value && MyManaManager.SpellFarm;

            public static bool UseQ
                =>
                    lastHitMenu["LastHitQ"] != null &&
                    lastHitMenu["LastHitQ"].GetValue<MenuBool>().Enabled;

            public static bool UseW
                =>
                    lastHitMenu["LastHitW"] != null &&
                    lastHitMenu["LastHitW"].GetValue<MenuBool>().Enabled;

            public static bool UseE
                =>
                    lastHitMenu["LastHitE"] != null &&
                    lastHitMenu["LastHitE"].GetValue<MenuBool>().Enabled;

            public static bool UseR
                =>
                    lastHitMenu["LastHitR"] != null &&
                    lastHitMenu["LastHitR"].GetValue<MenuBool>().Enabled;
            public static void AddQ(bool enabled = true)
            {
                lastHitMenu.Add(new MenuBool("LastHitQ", "Use Q", enabled));
            }

            public static void AddW(bool enabled = true)
            {
                lastHitMenu.Add(new MenuBool("LastHitW", "Use W", enabled));
            }

            public static void AddE(bool enabled = true)
            {
                lastHitMenu.Add(new MenuBool("LastHitE", "Use E", enabled));
            }

            public static void AddR(bool enabled = true)
            {
                lastHitMenu.Add(new MenuBool("LastHitR", "Use R", enabled));
            }

            public static void AddBool(string name, string defaultName, bool enabled = true)
            {
                lastHitMenu.Add(new MenuBool(name, defaultName, enabled));
            }

            public static void AddMana(int defalutValue = 30)
            {
                lastHitMenu.Add(new MenuSlider("LastHitMana", "When Player ManaPercent >= x%", defalutValue));
            }

            public static void AddSlider(string name, string defaultName, int defaultValue, int minValue, int maxValue)
            {
                lastHitMenu.Add(new MenuSlider(name, defaultName, defaultValue, minValue, maxValue));
            }

            public static void AddSliderBool(string name, string defaultName, int defaultValue, int minValue,
                int maxValue, bool enabled = false)
            {
                lastHitMenu.Add(new MenuSliderButton(name, defaultName, defaultValue, minValue, maxValue, enabled));
            }

            public static MenuBool GetBool(string name)
            {
                return lastHitMenu[name].GetValue<MenuBool>();
            }

            public static MenuSlider GetSlider(string name)
            {
                return lastHitMenu[name].GetValue<MenuSlider>();
            }

            public static MenuList GetList(string name)
            {
                return lastHitMenu[name].GetValue<MenuList>();
            }

            public static MenuSliderButton GetSliderBool(string name)
            {
                return lastHitMenu[name].GetValue<MenuSliderButton>();
            }
        }

        public class FleeOption
        {
            private static Menu fleeMenu => FleeMenu;

            public static void AddMenu()
            {
                FleeMenu = new Menu("FleeSettings", "Flee Settings")
                {
                    new MenuKeyBind("FleeKey", "Flee Key", Keys.Z, KeyBindType.Press)
                };
                myMenu.Add(FleeMenu);
            }

            public static bool isFleeKeyActive
                => fleeMenu["FleeKey"] != null && fleeMenu["FleeKey"].GetValue<MenuKeyBind>().Active;

            public static bool UseQ
                =>
                    fleeMenu["FleeQ"] != null &&
                    fleeMenu["FleeQ"].GetValue<MenuBool>().Enabled;

            public static bool UseW
                =>
                    fleeMenu["FleeW"] != null &&
                    fleeMenu["FleeW"].GetValue<MenuBool>().Enabled;

            public static bool UseE
                =>
                    fleeMenu["FleeE"] != null &&
                    fleeMenu["FleeE"].GetValue<MenuBool>().Enabled;

            public static bool UseR
                =>
                    fleeMenu["FleeR"] != null &&
                    fleeMenu["FleeR"].GetValue<MenuBool>().Enabled;

            public static void AddQ(bool enabled = true)
            {
                fleeMenu.Add(new MenuBool("FleeQ", "Use Q", enabled));
            }

            public static void AddW(bool enabled = true)
            {
                fleeMenu.Add(new MenuBool("FleeW", "Use W", enabled));
            }

            public static void AddE(bool enabled = true)
            {
                fleeMenu.Add(new MenuBool("FleeE", "Use E", enabled));
            }

            public static void AddR(bool enabled = true)
            {
                fleeMenu.Add(new MenuBool("FleeR", "Use R", enabled));
            }

            public static void AddBool(string name, string defaultName, bool enabled = true)
            {
                fleeMenu.Add(new MenuBool(name, defaultName, enabled));
            }

            public static MenuBool GetBool(string name)
            {
                return fleeMenu[name].GetValue<MenuBool>();
            }
        }

        public class KillStealOption
        {
            private static Menu killStealMenu => KillStealMenu;

            public static bool UseQ
                =>
                    killStealMenu["KillStealQ"] != null &&
                    killStealMenu["KillStealQ"].GetValue<MenuBool>().Enabled;

            public static bool UseW
                =>
                    killStealMenu["KillStealW"] != null &&
                    killStealMenu["KillStealW"].GetValue<MenuBool>().Enabled;

            public static bool UseE
                =>
                    killStealMenu["KillStealE"] != null &&
                    killStealMenu["KillStealE"].GetValue<MenuBool>().Enabled;

            public static bool UseR
                =>
                    killStealMenu["KillStealR"] != null &&
                    killStealMenu["KillStealR"].GetValue<MenuBool>().Enabled;

            public static void AddMenu()
            {
                KillStealMenu = new Menu("KillStealSettings", "KillSteal Settings");
                myMenu.Add(KillStealMenu);
            }

            public static void AddQ(bool enabled = true)
            {
                killStealMenu.Add(new MenuBool("KillStealQ", "Use Q", enabled));
            }

            public static void AddW(bool enabled = true)
            {
                killStealMenu.Add(new MenuBool("KillStealW", "Use W", enabled));
            }

            public static void AddE(bool enabled = true)
            {
                killStealMenu.Add(new MenuBool("KillStealE", "Use E", enabled));
            }

            public static void AddR(bool enabled = true)
            {
                killStealMenu.Add(new MenuBool("KillStealR", "Use R", enabled));
            }

            public static void AddSliderBool(string name, string defaultName, int defaultValue, int minValue,
                int maxValue, bool enabled = false)
            {
                killStealMenu.Add(new MenuSliderButton(name, defaultName, defaultValue, minValue, maxValue, enabled));
            }

            public static void AddSlider(string name, string defaultName, int defalueValue, int minValue = 0,
                int maxValue = 100)
            {
                killStealMenu.Add(new MenuSlider(name, defaultName, defalueValue, minValue, maxValue));
            }

            public static MenuSlider GetSlider(string name)
            {
                return killStealMenu[name].GetValue<MenuSlider>();
            }

            public static MenuSliderButton GetSliderBool(string itemName)
            {
                return killStealMenu[itemName].GetValue<MenuSliderButton>();
            }

            public static void AddTargetList()
            {
                killStealMenu.Add(new MenuSeparator("KillStealListSettings", "KillSteal Target List"));

                foreach (var target in GameObjects.EnemyHeroes)
                {
                    if (target != null)
                    {
                        killStealMenu.Add(new MenuBool("KillStealList" + target.CharacterName.ToLower(), "Use On: " + target.CharacterName));
                    }
                }
            }

            public static bool GetKillStealTarget(string name)
            {
                return killStealMenu["KillStealList" + name.ToLower()] != null &&
                       killStealMenu["KillStealList" + name.ToLower()].GetValue<MenuBool>().Enabled;
            }

            public static void AddBool(string name, string defaultName, bool enabled = true)
            {
                killStealMenu.Add(new MenuBool(name, defaultName, enabled));
            }

            public static MenuBool GetBool(string name)
            {
                return killStealMenu[name].GetValue<MenuBool>();
            }
        }

        public class MiscOption
        {
            private static Menu miscMenu => MiscMenu;

            public static void AddMenu()
            {
                MiscMenu = new Menu("MiscSettings", "Misc Settings");
                myMenu.Add(MiscMenu);
            }

            public static void AddBasic()
            {
                MyManaManager.AddFarmToMenu(miscMenu);
            }

            public static void AddBool(string name, string defaultName, bool enabled = true)
            {
                miscMenu.Add(new MenuBool(name, defaultName, enabled));
            }

            public static void AddSlider(string name, string defaultName, int defalueValue, int minValue = 0,
                int maxValue = 100)
            {
                miscMenu.Add(new MenuSlider(name, defaultName, defalueValue, minValue, maxValue));
            }

            public static void AddKey(string name, string defaultName, Keys Keys, KeyBindType type, bool enabled = false)
            {
                miscMenu.Add(new MenuKeyBind(name, defaultName, Keys, type){ Active = enabled });
            }

            public static void AddList(string name, string defaultName, string[] values, int defaultValue = 0)
            {
                miscMenu.Add(new MenuList(name, defaultName, values, defaultValue));
            }

            public static void AddSliderBool(string name, string defaultName, int defaultValue, int minValue,
                int maxValue, bool enabled = false)
            {
                miscMenu.Add(new MenuSliderButton(name, defaultName, defaultValue, minValue, maxValue, enabled));
            }

            public static void AddBool(string menuName, string name, string defaultName, bool enabled = true)
            {
                var subMeun = miscMenu["SharpShooter.MiscSettings." + menuName] as Menu;
                subMeun?.Add(new MenuBool(name, defaultName, enabled));
            }

            public static void AddSlider(string menuName, string name, string defaultName, int defalueValue, int minValue = 0,
                int maxValue = 100)
            {
                var subMeun = miscMenu["SharpShooter.MiscSettings." + menuName] as Menu;
                subMeun?.Add(new MenuSlider(name, defaultName, defalueValue, minValue, maxValue));
            }

            public static void AddKey(string menuName, string name, string defaultName, Keys Keys, KeyBindType type, bool enabled = false)
            {
                var subMeun = miscMenu["SharpShooter.MiscSettings." + menuName] as Menu;
                subMeun?.Add(new MenuKeyBind(name, defaultName, Keys, type){Active = enabled });
            }

            public static void AddList(string menuName, string name, string defaultName, string[] values, int defaultValue = 0)
            {
                var subMeun = miscMenu["SharpShooter.MiscSettings." + menuName] as Menu;
                subMeun?.Add(new MenuList(name, defaultName, values, defaultValue));
            }

            public static void AddSliderBool(string menuName, string name, string defaultName, int defaultValue, int minValue,
                int maxValue, bool enabled = false)
            {
                var subMeun = miscMenu["SharpShooter.MiscSettings." + menuName] as Menu;
                subMeun?.Add(new MenuSliderButton(name, defaultName, defaultValue, minValue, maxValue, enabled));
            }

            public static void AddSetting(string name)
            {
                var nameMenu = new Menu("SharpShooter.MiscSettings." + name, name + " Settings");
                miscMenu.Add(nameMenu);
            }

            public static void AddSubMenu(string name, string disableName)
            {
                var subMenu = new Menu("SharpShooter.MiscSettings." + name, disableName);
                miscMenu.Add(subMenu);
            }

            public static void AddQ()
            {
                var qMenu = new Menu("SharpShooter.MiscSettings.Q", "Q Settings");
                miscMenu.Add(qMenu);
            }

            public static void AddW()
            {
                var wMenu = new Menu("SharpShooter.MiscSettings.W", "W Settings");
                miscMenu.Add(wMenu);
            }

            public static void AddE()
            {
                var eMenu = new Menu("SharpShooter.MiscSettings.E", "E Settings");
                miscMenu.Add(eMenu);
            }

            public static void AddR()
            {
                var rMenu = new Menu("SharpShooter.MiscSettings.R", "R Settings");
                miscMenu.Add(rMenu);
            }

            public static MenuBool GetBool(string name)
            {
                return miscMenu[name].GetValue<MenuBool>();
            }

            public static MenuSlider GetSlider(string name)
            {
                return miscMenu[name].GetValue<MenuSlider>();
            }

            public static MenuKeyBind GetKey(string name)
            {
                return miscMenu[name].GetValue<MenuKeyBind>();
            }

            public static MenuList GetList(string name)
            {
                return miscMenu[name].GetValue<MenuList>();
            }

            public static MenuSliderButton GetSliderBool(string name)
            {
                return miscMenu[name].GetValue<MenuSliderButton>();
            }

            public static MenuBool GetBool(string menuName, string itemName)
            {
                return miscMenu["SharpShooter.MiscSettings." + menuName][itemName].GetValue<MenuBool>();
            }

            public static MenuSlider GetSlider(string menuName, string itemName)
            {
                return miscMenu["SharpShooter.MiscSettings." + menuName][itemName].GetValue<MenuSlider>();
            }

            public static MenuKeyBind GetKey(string menuName, string itemName)
            {
                return miscMenu["SharpShooter.MiscSettings." + menuName][itemName].GetValue<MenuKeyBind>();
            }

            public static MenuList GetList(string menuName, string itemName)
            {
                return miscMenu["SharpShooter.MiscSettings." + menuName][itemName].GetValue<MenuList>();
            }

            public static MenuSliderButton GetSliderBool(string menuName, string itemName)
            {
                return miscMenu["SharpShooter.MiscSettings." + menuName][itemName].GetValue<MenuSliderButton>();
            }
        }

        public class DrawOption
        {
            private static Menu drawMenu => DrawMenu;

            public static Menu spellMenu;
            public static Menu DamageHeroMenu;

            public static void AddMenu()
            {
                DrawMenu = new Menu("DrawSettings", "Draw Settings");
                myMenu.Add(DrawMenu);

                spellMenu = new Menu("SharpShooter.DrawSettings.SpellMenu", "Spell Range");
                DrawMenu.Add(spellMenu);
            }

            public static void AddDamageIndicatorToHero(bool q, bool w, bool e, bool r, bool attack, bool enabledHero = true, bool fill = true)
            {
                DamageHeroMenu = new Menu("SharpShooter.DrawSettings.DamageIndicatorToHero", "Damage Indicator")
                {
                    new MenuBool("SharpShooter.DrawSettings.DamageIndicatorToHero.EnabledHero", "Draw On Heros",
                        enabledHero),
                    new MenuBool("SharpShooter.DrawSettings.DamageIndicatorToHero.Q", "Draw Q Damage", q),
                    new MenuBool("SharpShooter.DrawSettings.DamageIndicatorToHero.W", "Draw W Damage", w),
                    new MenuBool("SharpShooter.DrawSettings.DamageIndicatorToHero.E", "Draw E Damage", e),
                    new MenuBool("SharpShooter.DrawSettings.DamageIndicatorToHero.R", "Draw R Damage", r),
                    new MenuBool("SharpShooter.DrawSettings.DamageIndicatorToHero.Attack", "Draw Attack Damage", attack),
                    new MenuBool("SharpShooter.DrawSettings.DamageIndicatorToHero.Fill", "Draw Fill Damage", fill)
                };

                DrawMenu.Add(DamageHeroMenu);

                MyDamageIndicator.OnDamageIndicator();
            }

            public static void AddBool(string name, string defaultName, bool enabled = true)
            {
                drawMenu.Add(new MenuBool(name, defaultName, enabled));
            }

            public static void AddKey(string name, string defaultName, Keys Keys, KeyBindType type, bool enabled = false)
            {
                drawMenu.Add(new MenuKeyBind(name, defaultName, Keys, type){ Active = enabled });
            }

            public static void AddSlider(string name, string defaultName, int defalueValue, int minValue = 0,
                int maxValue = 100)
            {
                drawMenu.Add(new MenuSlider(name, defaultName, defalueValue, minValue, maxValue));
            }

            public static MenuBool GetBool(string name)
            {
                return drawMenu[name].GetValue<MenuBool>();
            }

            public static MenuSlider GetSlider(string name)
            {
                return drawMenu[name].GetValue<MenuSlider>();
            }

            public static MenuKeyBind GetKey(string name)
            {
                return drawMenu[name].GetValue<MenuKeyBind>();
            }

            public static MenuList GetList(string name)
            {
                return drawMenu[name].GetValue<MenuList>();
            }

            public static void AddList(string name, string defaultName, string[] values, int defaultValue = 0)
            {
                drawMenu.Add(new MenuList(name, defaultName, values, defaultValue));
            }

            public static void AddRange(Spell spell, string name, bool enabled = false)
            {
                spellMenu.Add(new MenuBool("Draw" + spell.Slot, "Draw" + name + " Range", enabled));

                Drawing.OnDraw += delegate
                {
                    if (ObjectManager.Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
                    {
                        return;
                    }

                    if (spellMenu["Draw" + spell.Slot].GetValue<MenuBool>().Enabled &&
                        ObjectManager.Player.Spellbook.GetSpell(spell.Slot).Level > 0 &&
                        ObjectManager.Player.Spellbook.CanUseSpell(spell.Slot) == SpellState.Ready)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.PreviousPosition, spell.Range, Color.FromArgb(199, 5, 255), 1);
                    }
                };
            }

            public static void AddQ(Spell spell, bool enabled = false)
            {
                spellMenu.Add(new MenuBool("DrawQ", "Draw Q Range", enabled));

                Drawing.OnDraw += delegate
                {
                    if (ObjectManager.Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
                    {
                        return;
                    }

                    if (spellMenu["DrawQ"].GetValue<MenuBool>().Enabled && 
                        ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level > 0 && 
                        ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.Q) == SpellState.Ready)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.PreviousPosition, spell.Range, Color.FromArgb(19, 130, 234), 1);
                    }
                };
            }

            public static void AddW(Spell spell, bool enabled = false)
            {
                spellMenu.Add(new MenuBool("DrawW", "Draw W Range", enabled));

                Drawing.OnDraw += delegate
                {
                    if (ObjectManager.Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
                    {
                        return;
                    }

                    if (spellMenu["DrawW"].GetValue<MenuBool>().Enabled &&
                        ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level > 0 &&
                        ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.W) == SpellState.Ready)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.PreviousPosition, spell.Range, Color.FromArgb(248, 246, 6), 1);
                    }
                };
            }

            public static void AddE(Spell spell, bool enabled = false)
            {
                spellMenu.Add(new MenuBool("DrawE", "Draw E Range", enabled));

                Drawing.OnDraw += delegate
                {
                    if (ObjectManager.Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
                    {
                        return;
                    }

                    if (spellMenu["DrawE"].GetValue<MenuBool>().Enabled && 
                        ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level > 0 &&
                        ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.E) == SpellState.Ready)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.PreviousPosition, spell.Range, Color.FromArgb(188, 6, 248), 1);
                    }
                };
            }

            public static void AddR(Spell spell, bool enabled = false)
            {
                spellMenu.Add(new MenuBool("DrawR", "Draw R Range", enabled));

                Drawing.OnDraw += delegate
                {
                    if (ObjectManager.Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
                    {
                        return;
                    }

                    if (spellMenu["DrawR"].GetValue<MenuBool>().Enabled &&
                        ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level > 0 &&
                        ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.PreviousPosition, spell.Range, Color.Red, 1);
                    }
                };
            }

            public static void AddQExtend(Spell spell, bool enabled = false)
            {
                spellMenu.Add(new MenuBool("DrawQExtend", "Draw Q Extend Range", enabled));

                Drawing.OnDraw += delegate
                {
                    if (ObjectManager.Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
                    {
                        return;
                    }

                    if (spellMenu["DrawQExtend"].GetValue<MenuBool>().Enabled && 
                        ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level > 0 &&
                        ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.Q) == SpellState.Ready)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.PreviousPosition, spell.Range, Color.FromArgb(0, 255, 161), 1);
                    }
                };
            }
        }
    }
}