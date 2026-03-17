// ============================================================
// F1 Career Manager — RandomUtils.cs
// Generación de aleatoriedad controlada y con pesos
// ============================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace F1CareerManager.Utils
{
    public static class RandomUtils
    {
        public static T GetRandomByWeight<T>(Dictionary<T, int> weights)
        {
            int totalWeight = weights.Values.Sum();
            int randomValue = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var item in weights)
            {
                currentWeight += item.Value;
                if (randomValue < currentWeight)
                    return item.Key;
            }

            return weights.Keys.First();
        }

        public static bool Chance(float percent)
        {
            return Random.Range(0f, 100f) < percent;
        }

        public static int Range(int min, int max)
        {
            return Random.Range(min, max);
        }
    }
}
