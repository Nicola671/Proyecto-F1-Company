// ============================================================
// F1 Career Manager — StringUtils.cs
// Formateo de textos, monedas y tiempos de carrera
// ============================================================

using System;

namespace F1CareerManager.Utils
{
    public static class StringUtils
    {
        public static string FormatCurrency(long amount)
        {
            if (amount >= 1000000)
                return $"${amount / 1000000f:F1}M";
            if (amount >= 1000)
                return $"${amount / 1000f:F1}K";
            return $"${amount}";
        }

        public static string FormatTime(float seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}.{2:D3}", t.Minutes, t.Seconds, t.Milliseconds);
        }

        public static string ToTitleCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1).ToLower();
        }
    }
}
