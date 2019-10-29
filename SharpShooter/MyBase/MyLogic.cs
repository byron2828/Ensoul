namespace SharpShooter.MyBase
{
    #region

    using EnsoulSharp;
    using EnsoulSharp.SDK;

    #endregion

    public class MyLogic
    {
        public static Spell Q { get; set; }
        public static Spell Q2 { get; set; }
        public static Spell QE { get; set; }
        public static Spell EQ { get; set; }
        public static Spell W { get; set; }
        public static Spell W2 { get; set; }
        public static Spell E { get; set; }
        public static Spell E2 { get; set; }
        public static Spell R { get; set; }
        public static Spell R2 { get; set; }
        public static Spell Flash { get; set; }
        public static Spell Ignite { get; set; }

        public static SpellSlot IgniteSlot { get; set; } = SpellSlot.Unknown;
        public static SpellSlot FlashSlot { get; set; } = SpellSlot.Unknown;

        public static int LastForcusTime { get; set; } = 0;

        public static AIHeroClient Me => ObjectManager.Player;
    }
}
