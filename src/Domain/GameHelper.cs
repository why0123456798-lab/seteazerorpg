using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Domain
{
    public static class GameHelper
    {

        public static Dictionary<int, double> GetRarityWeight(int level)
        {

            var rarityWeights = new Dictionary<int, double> { };

            if (level == 1 || level == 2)
                rarityWeights = new Dictionary<int, double> { { 1, 0.60 }, { 2, 0.25 }, { 3, 0.10 }, { 4, 0.04 }, { 5, 0.01 } };
            else if (level == 3 || level == 4)
                rarityWeights = new Dictionary<int, double> { { 1, 0.40 }, { 2, 0.30 }, { 3, 0.15 }, { 4, 0.10 }, { 5, 0.05 } };
            else if (level == 5 || level == 6)
                rarityWeights = new Dictionary<int, double> { { 1, 0.15 }, { 2, 0.25 }, { 3, 0.30 }, { 4, 0.23 }, { 5, 0.07 } };
            else
                rarityWeights = new Dictionary<int, double> { { 1, 0.15 }, { 2, 0.20 }, { 3, 0.35 }, { 4, 0.20 }, { 5, 0.10 } };

            return rarityWeights;
        }
    }
}
