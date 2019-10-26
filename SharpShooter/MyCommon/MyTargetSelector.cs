namespace SharpShooter.MyCommon
{
    #region

    using System.Collections.Generic;
    using System.Linq;

    using EnsoulSharp;
    using EnsoulSharp.SDK;

    #endregion

    public class MyTargetSelector
    {
        public static AIHeroClient GetTarget(float range, bool ForcusOrbwalkerTarget = true, bool checkKillAble = true, bool checkShield = false)
        {
            var selectTarget = TargetSelector.SelectedTarget;

            if (selectTarget != null && selectTarget.IsValidTarget(range))
            {
                if (!checkKillAble || !selectTarget.IsUnKillable())
                {
                    if (!checkShield || !selectTarget.HaveShiledBuff())
                    {
                        return selectTarget;
                    }
                }
            }

            var orbTarget = Orbwalker.GetTarget() as AIHeroClient;

            if (ForcusOrbwalkerTarget && orbTarget != null && orbTarget.IsValidTarget(range) && orbTarget.InAutoAttackRange())
            {
                if (!checkKillAble || !orbTarget.IsUnKillable())
                {
                    if (!checkShield || !orbTarget.HaveShiledBuff())
                    {
                        return orbTarget;
                    }
                }
            }

            var finallyTarget = TargetSelector.GetTargets(range).FirstOrDefault();

            if (finallyTarget != null && finallyTarget.IsValidTarget(range))
            {
                if (!checkKillAble || !finallyTarget.IsUnKillable())
                {
                    if (!checkShield || !finallyTarget.HaveShiledBuff())
                    {
                        return finallyTarget;
                    }
                }
            }

            return null;
        }

        public static List<AIHeroClient> GetTargets(float range, bool checkKillAble = true, bool checkShield = false)
        {
            return
                TargetSelector.GetTargets(range)
                    .Where(x => !checkKillAble || !x.IsUnKillable())
                    .Where(x => !checkShield || !x.HaveShiledBuff())
                    .ToList();
        }
    }
}
