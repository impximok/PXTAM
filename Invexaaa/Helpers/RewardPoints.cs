// File: Helpers/CustomerRewardPoints.cs
using System;

namespace SnomiAssignmentReal.Helpers
{
    public static class RewardPoints
    {
        public const decimal EarnRatePointsPerMYR = 1m;      // earn 1 pt per RM1 net
        public const int RedeemPointsPerMYR = 100;           // 100 pts = RM 1
        public const decimal MaxRedeemPctOfSubtotal = 0.5m;  // 50% cap

        public static int ComputeEarnedPoints(decimal netAmount)
        {
            if (netAmount <= 0) return 0;
            return (int)decimal.Floor(netAmount * EarnRatePointsPerMYR);
        }

        public static decimal ComputeDiscountForPoints(int points)
        {
            if (points <= 0) return 0m;
            return points / (decimal)RedeemPointsPerMYR;
        }

        public static int ComputePointsNeededForDiscount(decimal discountMYR)
        {
            if (discountMYR <= 0) return 0;
            return (int)decimal.Ceiling(discountMYR * RedeemPointsPerMYR);
        }
    }
}
