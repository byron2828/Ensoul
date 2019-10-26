namespace SharpShooter.MyCommon
{
    #region

    using System;
    using System.Linq;

    using SharpDX;

    using EnsoulSharp.SDK;
    using EnsoulSharp.SDK.MenuUI.Values;
    using EnsoulSharp.SDK.MenuUI;
    using EnsoulSharp;
    using EnsoulSharp.SDK.Events;
    #endregion

    public class MyEvadeManager
    {
        public static Menu Menu;
        private static int RivenQTime;
        private static float RivenQRange;
        private static Vector2 RivenDashPos;

        public static void Attach(Menu evadeMenu)
        {
            Menu = evadeMenu;

            Menu.Add(new MenuSeparator("MadeByNightMoon", "Made by NightMoon"));
            Menu.Add(new MenuSeparator("MoreSpellDodoge", "More Spell Dodge Pls Check Evade"));
            Menu.Add(new MenuSeparator("123123321123XD", " "));
            Menu.Add(new MenuBool("EnabledDodge", "Enabled Block Spell"));
            Menu.Add(new MenuSlider("EnabledHP", "When Player HealthPercent <= x%",
                ObjectManager.Player.CharacterName == "Sivir" ? 100 : 30));

            foreach (
                var hero in
                GameObjects.EnemyHeroes.Where(
                    i => MyBlockSpellDataBase.Spells.Any(a => a.CharacterName == i.CharacterName)))
            {
                var heroMenu = new Menu("Block" + hero.CharacterName.ToLower(), hero.CharacterName);
                Menu.Add(heroMenu);
            }

            foreach (
                var spell in
                MyBlockSpellDataBase.Spells.Where(
                    x =>
                        ObjectManager.Get<AIHeroClient>().Any(
                            a => a.IsEnemy &&
                                 string.Equals(a.CharacterName, x.CharacterName,
                                     StringComparison.CurrentCultureIgnoreCase))))
            {
                var heroMenu = Menu["Block" + spell.CharacterName.ToLower()] as Menu;
                heroMenu?.Add(new MenuBool("BlockSpell" + spell.SpellSlot, spell.CharacterName + " " + spell.SpellSlot));
            }

            Tick.OnTick += OnUpdate;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            AIBaseClient.OnPlayAnimation += OnPlayAnimation;
            Dash.OnDash += OnDash;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || !Menu["EnabledDodge"].GetValue<MenuBool>().Enabled ||
                ObjectManager.Player.HealthPercent > Menu["EnabledHP"].GetValue<MenuSlider>().Value)
            {
                return;
            }

            if (ObjectManager.Player.CharacterName == "Sivir" &&
                ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.E) != SpellState.Ready)
            {
                return;
            }

            if (ObjectManager.Player.CharacterName == "Xayah" &&
                ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.R) != SpellState.Ready)
            {
                return;
            }
            
            var buffs = ObjectManager.Player.Buffs;

            foreach (var buff in buffs)
            {
                var time = buff.EndTime;

                switch (buff.Name.ToLower())
                {
                    case "karthusfallenonetarget":
                        if ((time - Game.Time) * 1000 <= 300)
                        {
                            CastSpell();
                        }
                        break;
                    case "nautilusgrandlinetarget":
                        if ((time - Game.Time) * 1000 <= 300)
                        {
                            CastSpell();
                        }
                        break;
                    case "nocturneparanoiadash":
                        if (GameObjects.EnemyHeroes.FirstOrDefault(
                                x =>
                                    !x.IsDead && x.CharacterName.ToLower() == "nocturne" &&
                                    x.Distance(ObjectManager.Player) < 500) != null)
                        {
                            CastSpell();
                        }
                        break;
                    case "soulshackles":
                        if ((time - Game.Time) * 1000 <= 300)
                        {
                            CastSpell();
                        }
                        break;
                    case "vladimirhemoplaguedebuff":
                        if ((time - Game.Time) * 1000 <= 300)
                        {
                            CastSpell();
                        }
                        break;
                    case "zedrdeathmark":
                        if ((time - Game.Time) * 1000 <= 300)
                        {
                            CastSpell();
                        }
                        break;
                }
            }
            

            foreach (var target in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget()))
            {
                switch (target.CharacterName)
                {
                    case "Jax":
                        {
                            if (Menu["Blockjax"]["BlockSpellE"] != null && Menu["Blockjax"]["BlockSpellE"].GetValue<MenuBool>().Enabled)
                            {
                                if (target.HasBuff("jaxcounterstrike"))
                                {
                                    var buff = target.Buffs.FirstOrDefault(x => x.Name.ToLower() == "jaxcounterstrike");

                                    if (buff != null && (buff.EndTime - Game.Time) * 1000 <= 650 &&
                                        ObjectManager.Player.PreviousPosition.Distance(target.PreviousPosition) <= 350f)
                                    {
                                        CastSpell();
                                    }
                                }
                            }
                        }
                        break;
                    case "Riven":
                        {
                            if (Menu["Blockriven"]["BlockSpellQ"] != null && Menu["Blockriven"]["BlockSpellQ"].GetValue<MenuBool>().Enabled)
                            {
                                if ((int)(Game.Time * 1000) - RivenQTime <= 100 && !RivenDashPos.IsZero &&
                                    ObjectManager.Player.Distance(target) <= RivenQRange)
                                {
                                    CastSpell();
                                }
                            }
                        }
                        break;
                }
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs Args)
        {
            if (ObjectManager.Player.IsDead || !Menu["EnabledDodge"].GetValue<MenuBool>().Enabled ||
                ObjectManager.Player.HealthPercent > Menu["EnabledHP"].GetValue<MenuSlider>().Value)
            {
                return;
            }

            if (ObjectManager.Player.CharacterName == "Sivir" &&
                ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.E) != SpellState.Ready)
            {
                return;
            }

            if (ObjectManager.Player.CharacterName == "Xayah" &&
                ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.R) != SpellState.Ready)
            {
                return;
            }

            var target = sender as AIHeroClient;

            if (target == null || target.Team == ObjectManager.Player.Team || !target.IsValid ||
                Args.Target == null || string.IsNullOrEmpty(Args.SData.Name))
            {
                return;
            }

            var spells =
                MyBlockSpellDataBase.Spells.Where(
                    x =>
                        string.Equals(x.CharacterName, target.CharacterName, StringComparison.CurrentCultureIgnoreCase) &&
                         Menu["Block" + target.CharacterName.ToLower()]["BlockSpell" + x.SpellSlot.ToString()] != null &&
                        Menu["Block" + target.CharacterName.ToLower()]["BlockSpell" + x.SpellSlot.ToString()].GetValue<MenuBool>().Enabled).ToList();

            if (spells.Any())
            {
                foreach (var x in spells)
                {
                    switch (x.CharacterName)
                    {
                        case "Alistar":
                            if (x.SpellSlot == SpellSlot.Q && Args.Slot == x.SpellSlot)
                            {
                                if (target.Distance(ObjectManager.Player) <= 350)
                                {
                                    CastSpell("Alistar", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.W && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Alistar", x.SpellSlot);
                                }
                            }
                            break;
                        case "Blitzcrank":
                            if (x.SpellSlot == SpellSlot.E && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SData.Name == "PowerFistAttack")
                                {
                                    CastSpell("Blitzcrank", x.SpellSlot);
                                }
                            }
                            break;
                        case "Chogath":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Chogath", x.SpellSlot);
                                }
                            }
                            break;
                        case "Darius":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Darius", x.SpellSlot);
                                }
                            }
                            break;
                        case "Elise":
                            if (x.SpellSlot == SpellSlot.Q && Args.SData.Name == "EliseHumanQ" && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Elise", x.SpellSlot);
                                }
                            }
                            break;
                        case "Fiddlesticks":
                            if (x.SpellSlot == SpellSlot.Q && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Fiddlesticks", x.SpellSlot);
                                }
                            }
                            break;
                        case "Gangplank":
                            if (x.SpellSlot == SpellSlot.Q && Args.SData.Name == "Parley" && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Gangplank", x.SpellSlot);
                                }
                            }
                            break;
                        case "Garen":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Garen", x.SpellSlot);
                                }
                            }
                            break;
                        case "Hecarim":
                            if (x.SpellSlot == SpellSlot.E && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SData.Name == "HecarimRampAttack")
                                {
                                    CastSpell("Hecarim", x.SpellSlot);
                                }
                            }
                            break;
                        case "Irelia":
                            if (x.SpellSlot == SpellSlot.E && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Irelia", x.SpellSlot);
                                }
                            }
                            break;
                        case "Jarvan":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Jarvan", x.SpellSlot);
                                }
                            }
                            break;
                        case "Kalista":
                            if (x.SpellSlot == SpellSlot.E && Args.Slot == x.SpellSlot)
                            {
                                if (ObjectManager.Player.HasBuff("kalistaexpungemarker") &&
                                    ObjectManager.Player.Distance(target) <= 950f)
                                {
                                    CastSpell("Kalista", x.SpellSlot);
                                }
                            }
                            break;
                        case "Kayle":
                            if (x.SpellSlot == SpellSlot.Q && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Kayle", x.SpellSlot);
                                }
                            }
                            break;
                        case "Leesin":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Leesin", x.SpellSlot);
                                }
                            }
                            break;
                        case "Lissandra":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Lissandra", x.SpellSlot);
                                }
                            }
                            break;
                        case "Morgana":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Morgana", x.SpellSlot);
                                }
                            }
                            break;
                        case "Malzahar":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Mordekaiser", x.SpellSlot);
                                }
                            }
                            break;
                        case "Mordekaiser":
                            if (x.SpellSlot == SpellSlot.Q && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SData.Name == "MordekaiserQAttack2")
                                {
                                    CastSpell("Mordekaiser", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Mordekaiser", x.SpellSlot);
                                }
                            }
                            break;
                        case "Nasus":
                            if (x.SpellSlot == SpellSlot.W && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Nasus", x.SpellSlot);
                                }
                            }
                            break;
                        case "Olaf":
                            if (x.SpellSlot == SpellSlot.E && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Olaf", x.SpellSlot);
                                }
                            }
                            break;
                        case "Pantheon":
                            if (x.SpellSlot == SpellSlot.Q && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Pantheon", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.W && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Pantheon", x.SpellSlot);
                                }
                            }
                            break;
                        case "Renekton":
                            if (x.SpellSlot == SpellSlot.W && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Renekton", x.SpellSlot);
                                }
                            }
                            break;
                        case "Rengar":
                            if (x.SpellSlot == SpellSlot.Q && Args.Slot == x.SpellSlot)
                            {
                                if (ObjectManager.Player.Distance(target) <= 300 && Args.Target.IsMe)
                                {
                                    CastSpell("Rengar", x.SpellSlot);
                                }
                            }
                            break;
                        case "Riven":
                            if (x.SpellSlot == SpellSlot.W && Args.Slot == x.SpellSlot)
                            {
                                if (ObjectManager.Player.Position.Distance(target.Position) <=
                                    125f + ObjectManager.Player.BoundingRadius + target.BoundingRadius)
                                {
                                    CastSpell("Riven", x.SpellSlot);
                                }
                            }
                            break;
                        case "Ryze":
                            if (x.SpellSlot == SpellSlot.W && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Ryze", x.SpellSlot);
                                }
                            }
                            break;
                        case "Singed":
                            if (x.SpellSlot == SpellSlot.E && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Singed", x.SpellSlot);
                                }
                            }
                            break;
                        case "Syndra":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SData.Name == "SyndraR")
                                {
                                    CastSpell("Syndra", x.SpellSlot);
                                }
                            }
                            break;
                        case "TahmKench":
                            if (x.SpellSlot == SpellSlot.W && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("TahmKench", x.SpellSlot);
                                }
                            }
                            break;
                        case "Tristana":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SData.Name == "TristanaR")
                                {
                                    CastSpell("Tristana", x.SpellSlot);
                                }
                            }
                            break;
                        case "Trundle":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Trundle", x.SpellSlot);
                                }
                            }
                            break;
                        case "TwistedFate":
                            if (Args.SData.Name.Contains("attack") && Args.Target.IsMe &&
                                target.Buffs.Any(
                                    buff =>
                                        buff.Name == "BlueCardAttack" || buff.Name == "GoldCardAttack" ||
                                        buff.Name == "RedCardAttack"))
                            {
                                CastSpell("TwistedFate", x.SpellSlot);
                            }
                            break;
                        case "Veigar":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SData.Name == "VeigarPrimordialBurst")
                                {
                                    CastSpell("Veigar", x.SpellSlot);
                                }
                            }
                            break;
                        case "Vi":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Vi", x.SpellSlot);
                                }
                            }
                            break;
                        case "Volibear":
                            if (x.SpellSlot == SpellSlot.Q && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Volibear", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.W && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Volibear", x.SpellSlot);
                                }
                            }
                            break;
                        case "Warwick":
                            if (x.SpellSlot == SpellSlot.R && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Warwick", x.SpellSlot);
                                }
                            }
                            break;
                        case "XinZhao":
                            if (x.SpellSlot == SpellSlot.Q && Args.Slot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SData.Name == "XenZhaoThrust3")
                                {
                                    CastSpell("XinZhao", x.SpellSlot);
                                }
                            }
                            break;
                    }
                }
            }
        }

        private static void OnPlayAnimation(AIBaseClient sender, AIBaseClientPlayAnimationEventArgs Args)
        {
            var riven = sender as AIHeroClient;

            if (riven == null || riven.Team == ObjectManager.Player.Team || riven.CharacterName != "Riven" || !riven.IsValid)
            {
                return;
            }


            if (Menu["Block" + riven.CharacterName.ToLower()]["BlockSpell" + SpellSlot.Q.ToString()] != null &&
                Menu["Block" + riven.CharacterName.ToLower()]["BlockSpell" + SpellSlot.Q.ToString()].GetValue<MenuBool>().Enabled)
            {
                if (Args.Animation.ToLower() == "spell1c")
                {
                    RivenQTime = (int) (Game.Time * 1000);
                    RivenQRange = riven.HasBuff("RivenFengShuiEngine") ? 225f : 150f;
                }
            }
        }

        private static void OnDash(AIBaseClient sender, Dash.DashArgs Args)
        {
            var riven = sender as AIHeroClient;
            if (riven == null || riven.Team == ObjectManager.Player.Team || riven.CharacterName != "Riven" || !riven.IsValid)
            {
                return;
            }

            if (Menu["Block" + riven.CharacterName.ToLower()]["BlockSpell" + SpellSlot.Q.ToString()] != null &&
               Menu["Block" + riven.CharacterName.ToLower()]["BlockSpell" + SpellSlot.Q.ToString()].GetValue<MenuBool>().Enabled)
            {
                RivenDashPos = Args.EndPos;
            }
        }

        private static void CastSpell()
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            if (ObjectManager.Player.CharacterName == "Sivir" &&
                ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.E) != SpellState.Ready)
            {
                return;
            }

            if (ObjectManager.Player.CharacterName == "Xayah" &&
                ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.R) != SpellState.Ready)
            {
                return;
            }

            var target =
                GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.Distance(ObjectManager.Player) <= 750f)
                    .OrderBy(x => x.Distance(ObjectManager.Player))
                    .FirstOrDefault();

            if (ObjectManager.Player.CharacterName == "Sivir")
            {
                ObjectManager.Player.Spellbook.CastSpell(SpellSlot.E);
            }
            else if (ObjectManager.Player.CharacterName == "Xayah")
            {
                ObjectManager.Player.Spellbook.CastSpell(SpellSlot.R, target?.Position ?? Game.CursorPos);
            }
        }

        private static void CastSpell(string name, SpellSlot spellslot)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            if (ObjectManager.Player.CharacterName == "Sivir" &&
                ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.E) !=  SpellState.Ready)
            {
                return;
            }

            if (ObjectManager.Player.CharacterName == "Xayah" &&
                ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.R) != SpellState.Ready)
            {
                return;
            }

            if (Menu["Block" + name.ToLower()]["BlockSpell" + spellslot.ToString()] != null &&
                Menu["Block" + name.ToLower()]["BlockSpell" + spellslot.ToString()].GetValue<MenuBool>().Enabled)
            {
                var target =
                   GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.Distance(ObjectManager.Player) <= 750f)
                        .OrderBy(x => x.Distance(ObjectManager.Player))
                        .FirstOrDefault();

                if (ObjectManager.Player.CharacterName == "Sivir")
                {
                    ObjectManager.Player.Spellbook.CastSpell(SpellSlot.E);
                }
                else if (ObjectManager.Player.CharacterName == "Xayah")
                {
                    ObjectManager.Player.Spellbook.CastSpell(SpellSlot.R, target?.Position ?? Game.CursorPos);
                }
            }
        }
    }
}