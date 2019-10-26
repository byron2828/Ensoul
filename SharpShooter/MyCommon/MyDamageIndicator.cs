namespace SharpShooter.MyCommon
{
    #region

    using System;
    using System.Linq;

    using SharpDX;

    using EnsoulSharp;
    using EnsoulSharp.SDK;
    using EnsoulSharp.SDK.MenuUI.Values;

    using Color = System.Drawing.Color;

    #endregion

    public class MyDamageIndicator // made by detuks and xcsoft
    {
        private const int XOffset = 10;
        private static int YOffset = 20;
        private const int Width = 103;
        private const int Height = 11;

        private static readonly Color Color = Color.Lime;
        private static readonly Color FillColor = Color.Goldenrod;

        private static bool hero => MyMenuExtensions.DrawOption
            .DamageHeroMenu["SharpShooter.DrawSettings.DamageIndicatorToHero.EnabledHero"].GetValue<MenuBool>().Enabled;

        private static bool mob => MyMenuExtensions.DrawOption
            .DamageHeroMenu["SharpShooter.DrawSettings.DamageIndicatorToHero.EnabledMob"].GetValue<MenuBool>().Enabled;

        private static bool Fill => MyMenuExtensions.DrawOption
            .DamageHeroMenu["SharpShooter.DrawSettings.DamageIndicatorToHero.Fill"].GetValue<MenuBool>().Enabled;

        private static bool q => MyMenuExtensions.DrawOption
            .DamageHeroMenu["SharpShooter.DrawSettings.DamageIndicatorToHero.Q"].GetValue<MenuBool>().Enabled;

        private static bool w => MyMenuExtensions.DrawOption
            .DamageHeroMenu["SharpShooter.DrawSettings.DamageIndicatorToHero.W"].GetValue<MenuBool>().Enabled;

        private static bool e => MyMenuExtensions.DrawOption
            .DamageHeroMenu["SharpShooter.DrawSettings.DamageIndicatorToHero.E"].GetValue<MenuBool>().Enabled;

        private static bool r => MyMenuExtensions.DrawOption
            .DamageHeroMenu["SharpShooter.DrawSettings.DamageIndicatorToHero.R"].GetValue<MenuBool>().Enabled;

        private static bool attack => MyMenuExtensions.DrawOption
            .DamageHeroMenu["SharpShooter.DrawSettings.DamageIndicatorToHero.Attack"].GetValue<MenuBool>().Enabled;

        public static void OnDamageIndicator()
        {
            Drawing.OnEndScene += delegate
            {
                if (ObjectManager.Player.IsDead)
                {
                    return;
                }

                if (hero)
                {
                    foreach (var target in GameObjects.EnemyHeroes.Where(h => h.IsValid && h.IsHPBarRendered))
                    {
                        Vector2 pos;
                        Drawing.WorldToScreen(target.Position, out pos);

                        if (!pos.IsOnScreen())
                        {
                            return;
                        }

                        var damage = (float)target.GetComboDamage(q, w, e, r, attack);
                        if (damage > 2)
                        {
                            var barPos = target.HPBarPosition - new Vector2(55, 45);

                            var percentHealthAfterDamage = Math.Max(0, target.Health - damage) / target.MaxHealth;
                            var yPos = barPos.Y + YOffset;
                            var xPosDamage = barPos.X + XOffset + Width * percentHealthAfterDamage;
                            var xPosCurrentHp = barPos.X + XOffset + Width * target.Health / target.MaxHealth;

                            if (damage > target.Health)
                            {
                                var X = (int)barPos.X + XOffset;
                                var Y = (int)barPos.Y + YOffset - 13;
                                var text = "KILLABLE: " + (target.Health - damage);
                                Drawing.DrawText(X, Y, Color.Red, text);
                            }

                            Drawing.DrawLine(xPosDamage, yPos, xPosDamage, yPos + Height, 5, Color);

                            if (Fill)
                            {
                                var differenceInHp = xPosCurrentHp - xPosDamage;
                                var pos1 = barPos.X + 9 + 107 * percentHealthAfterDamage;

                                for (var i = 0; i < differenceInHp; i++)
                                {
                                    Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + Height, 5, FillColor);
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
