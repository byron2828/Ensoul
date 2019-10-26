namespace SharpShooter
{
    #region

    using EnsoulSharp.SDK;

    #endregion

    public class MyLoader
    {
        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += () =>
            {
                new MyBase.MyChampions();
            };
        }
    }
}
