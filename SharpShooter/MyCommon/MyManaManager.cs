namespace SharpShooter.MyCommon
{
    using EnsoulSharp;
    #region 

    using EnsoulSharp.SDK.MenuUI;
    using EnsoulSharp.SDK.MenuUI.Values;
    using EnsoulSharp.SDK.Events;
    using Keys = System.Windows.Forms.Keys;

    #endregion

    public class MyManaManager
    {
        public static bool SpellFarm { get; set; } = true;
        public static bool SpellHarass { get; set; } = true;

        private static bool FarmScrool { get; set; } = true;
        private static bool HarassScrool { get; set; } = true;

        public static void AddFarmToMenu(Menu mainMenu)
        {
            if (mainMenu != null)
            {
                var farmMenu = new Menu("MyManaManager.SpellFarmSettings", "Spell Settings")
                {
                    new MenuBool("MyManaManager.SpellFarm", "Enabled Spell Farm"),
                    new MenuList("MyManaManager.SpellFarmMode", "Control Mode: ",
                        new[] {"Mouse scrool", "Key Toggle", "Off"}),
                    new MenuKeyBind("MyManaManager.SpellFarmKey", "Spell Farm Key", Keys.J,
                        KeyBindType.Toggle){ Active = true },
                    new MenuBool("MyManaManager.SpellHarass", "Enabled Spell Harass"),
                    new MenuList("MyManaManager.SpellHarassMode", "Control Mode: ",
                        new[] {"Mouse scrool", "Key Toggle", "Off"}, 1),
                    new MenuKeyBind("MyManaManager.SpellHarassKey", "Spell Harass Key", Keys.H,
                        KeyBindType.Toggle){ Active = true }
                };
                mainMenu.Add(farmMenu);

                farmMenu["MyManaManager.SpellFarm"].GetValue<MenuBool>().Permashow();
                farmMenu["MyManaManager.SpellHarass"].GetValue<MenuBool>().Permashow();

                Game.OnWndProc += delegate (GameWndProcEventArgs Args)
                {
                    if (Args.Msg == 519)
                    {
                        if (farmMenu["MyManaManager.SpellFarmMode"].GetValue<MenuList>().Index == 0)
                        {
                            FarmScrool = !FarmScrool;
                        }

                        if (farmMenu["MyManaManager.SpellHarassMode"].GetValue<MenuList>().Index == 0)
                        {
                            HarassScrool = !HarassScrool;
                        }
                    }
                };

                Tick.OnTick += delegate
                {
                    SpellFarm = farmMenu["MyManaManager.SpellFarmMode"].GetValue<MenuList>().Index == 0 && FarmScrool ||
                                farmMenu["MyManaManager.SpellFarmMode"].GetValue<MenuList>().Index == 1 &&
                                farmMenu["MyManaManager.SpellFarmKey"].GetValue<MenuKeyBind>().Active ||
                                farmMenu["MyManaManager.SpellFarmMode"].GetValue<MenuList>().Index == 2;
                    SpellHarass = farmMenu["MyManaManager.SpellHarassMode"].GetValue<MenuList>().Index == 0 && FarmScrool ||
                                farmMenu["MyManaManager.SpellHarassMode"].GetValue<MenuList>().Index == 1 &&
                                farmMenu["MyManaManager.SpellHarassKey"].GetValue<MenuKeyBind>().Active;

                    farmMenu["MyManaManager.SpellFarm"].GetValue<MenuBool>().Enabled = SpellFarm;
                    farmMenu["MyManaManager.SpellHarass"].GetValue<MenuBool>().Enabled = SpellHarass;

                };
            }
        }
    }
}
