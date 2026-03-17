// ============================================================
// F1 Career Manager — MathUtils.cs
// Utilidades matemáticas para cálculos de simulación
// ============================================================

using UnityEngine;

namespace F1CareerManager.Utils
{
    public static class MathUtils
    {
        public static float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static float GaussianRandom(float mean, float stdDev)
        {
            float u1 = 1.0f - Random.value;
            float u2 = 1.0f - Random.value;
            float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        public static float Clamp01(float value)
        {
            return Mathf.Clamp01(value);
        }
    }
}
