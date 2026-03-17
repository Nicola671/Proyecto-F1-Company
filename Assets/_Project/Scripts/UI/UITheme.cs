// ============================================================
// F1 Career Manager — UITheme.cs
// Paleta completa de colores y constantes visuales
// ============================================================
// Todos los componentes UI referencian esta clase para colores,
// tamaños y estilos. NO hardcodear colores en ningún lado.
// ============================================================

using UnityEngine;

namespace F1CareerManager.UI
{
    /// <summary>
    /// Tema visual del juego. Centraliza todos los colores,
    /// tamaños y constantes de UI. Pixel art aesthetic.
    /// </summary>
    public static class UITheme
    {
        // ══════════════════════════════════════════════════════
        // COLORES BASE
        // ══════════════════════════════════════════════════════

        // ── Fondos ───────────────────────────────────────────
        public static readonly Color BackgroundDark = HexColor("#0D0D0D");
        public static readonly Color BackgroundPanel = HexColor("#1A1A1A");
        public static readonly Color BackgroundCard = HexColor("#242424");
        public static readonly Color BackgroundCardHover = HexColor("#2E2E2E");
        public static readonly Color BackgroundInput = HexColor("#1E1E1E");
        public static readonly Color BackgroundOverlay = new Color(0f, 0f, 0f, 0.75f);

        // ── Texto ────────────────────────────────────────────
        public static readonly Color TextPrimary = HexColor("#FFFFFF");
        public static readonly Color TextSecondary = HexColor("#8A8A8A");
        public static readonly Color TextMuted = HexColor("#555555");
        public static readonly Color TextAccent = HexColor("#E8002D");
        public static readonly Color TextPositive = HexColor("#22C55E");
        public static readonly Color TextNegative = HexColor("#EF4444");
        public static readonly Color TextWarning = HexColor("#F59E0B");

        // ── Acentos ──────────────────────────────────────────
        public static readonly Color AccentPrimary = HexColor("#E8002D");    // Rojo F1
        public static readonly Color AccentSecondary = HexColor("#15803D");  // Verde oscuro
        public static readonly Color AccentTertiary = HexColor("#1D4ED8");   // Azul
        public static readonly Color AccentGold = HexColor("#D4A017");       // Oro (P1)
        public static readonly Color AccentSilver = HexColor("#A8A8A8");     // Plata (P2)
        public static readonly Color AccentBronze = HexColor("#CD7F32");     // Bronce (P3)

        // ── Bordes ───────────────────────────────────────────
        public static readonly Color BorderDefault = HexColor("#333333");
        public static readonly Color BorderFocus = HexColor("#E8002D");
        public static readonly Color BorderSubtle = HexColor("#222222");

        // ══════════════════════════════════════════════════════
        // COLORES DE TARJETAS DE NOTICIAS
        // ══════════════════════════════════════════════════════

        public static readonly Color NewsUrgent = HexColor("#DC2626");      // 🔴 Rojo
        public static readonly Color NewsImportant = HexColor("#F59E0B");   // 🟡 Amarillo
        public static readonly Color NewsRegular = HexColor("#2563EB");     // 🔵 Azul
        public static readonly Color NewsRumor = HexColor("#D4D4D4");       // ⚪ Blanco
        public static readonly Color NewsPositive = HexColor("#22C55E");    // 🟢 Verde
        public static readonly Color NewsRival = HexColor("#9333EA");       // 🟣 Morado

        // ══════════════════════════════════════════════════════
        // COLORES DE EQUIPOS F1 2025
        // ══════════════════════════════════════════════════════

        public static Color GetTeamColor(string teamId)
        {
            switch (teamId)
            {
                case "redbull":      return HexColor("#3671C6");  // Azul Red Bull
                case "ferrari":      return HexColor("#E8002D");  // Rojo Ferrari
                case "mercedes":     return HexColor("#27F4D2");  // Turquesa Mercedes
                case "mclaren":      return HexColor("#FF8000");  // Naranja McLaren
                case "astonmartin":  return HexColor("#229971");  // Verde Aston Martin
                case "alpine":       return HexColor("#FF87BC");  // Rosa Alpine
                case "williams":     return HexColor("#64C4FF");  // Azul claro Williams
                case "haas":         return HexColor("#B6BABD");  // Gris Haas
                case "rb":           return HexColor("#6692FF");  // Azul RB
                case "sauber":       return HexColor("#52E252");  // Verde Sauber/Kick
                default:             return AccentPrimary;
            }
        }

        public static Color GetTeamSecondaryColor(string teamId)
        {
            switch (teamId)
            {
                case "redbull":      return HexColor("#FFD700");
                case "ferrari":      return HexColor("#FFF200");
                case "mercedes":     return HexColor("#000000");
                case "mclaren":      return HexColor("#47C7FF");
                case "astonmartin":  return HexColor("#04352B");
                case "alpine":       return HexColor("#0093CC");
                case "williams":     return HexColor("#FFFFFF");
                case "haas":         return HexColor("#E8002D");
                case "rb":           return HexColor("#FFFFFF");
                case "sauber":       return HexColor("#000000");
                default:             return TextPrimary;
            }
        }

        // ══════════════════════════════════════════════════════
        // COLORES DE STATS
        // ══════════════════════════════════════════════════════

        /// <summary>Color de stat según valor (0-100)</summary>
        public static Color GetStatColor(int value)
        {
            if (value >= 85) return HexColor("#22C55E");      // Verde brillante
            if (value >= 75) return HexColor("#4ADE80");      // Verde claro
            if (value >= 60) return HexColor("#FDE047");      // Amarillo
            if (value >= 45) return HexColor("#FB923C");      // Naranja
            if (value >= 30) return HexColor("#F87171");      // Rojo claro
            return HexColor("#EF4444");                        // Rojo
        }

        /// <summary>Color de forma actual del piloto</summary>
        public static Color GetFormColor(int formValue)
        {
            if (formValue >= 85) return HexColor("#22C55E");
            if (formValue >= 70) return HexColor("#FDE047");
            return HexColor("#EF4444");
        }

        // ══════════════════════════════════════════════════════
        // COLORES DE ESTADO EMOCIONAL
        // ══════════════════════════════════════════════════════

        public static Color GetMoodColor(string mood)
        {
            switch (mood)
            {
                case "Happy":    return HexColor("#22C55E");  // Verde
                case "Neutral":  return HexColor("#8A8A8A");  // Gris
                case "Upset":    return HexColor("#F59E0B");  // Amarillo
                case "Furious":  return HexColor("#EF4444");  // Rojo
                case "WantsOut": return HexColor("#DC2626");  // Rojo oscuro
                default:         return TextSecondary;
            }
        }

        public static string GetMoodEmoji(string mood)
        {
            switch (mood)
            {
                case "Happy":    return "😊";
                case "Neutral":  return "😐";
                case "Upset":    return "😤";
                case "Furious":  return "🤬";
                case "WantsOut": return "🚪";
                default:         return "😐";
            }
        }

        // ══════════════════════════════════════════════════════
        // COLORES DE ESTRELLAS
        // ══════════════════════════════════════════════════════

        public static Color GetStarColor(int stars)
        {
            switch (stars)
            {
                case 5: return HexColor("#FFD700");  // Oro
                case 4: return HexColor("#C0C0C0");  // Plata
                case 3: return HexColor("#CD7F32");  // Bronce
                case 2: return HexColor("#6B7280");  // Gris
                default: return HexColor("#4B5563");  // Gris oscuro
            }
        }

        // ══════════════════════════════════════════════════════
        // CONSTANTES DE TAMAÑO
        // ══════════════════════════════════════════════════════

        // ── Touch targets (mobile-first) ─────────────────────
        public const float TOUCH_MIN_SIZE = 44f;         // Mínimo 44px para mobile
        public const float BUTTON_HEIGHT = 48f;
        public const float BUTTON_HEIGHT_SMALL = 36f;
        public const float ICON_SIZE = 32f;
        public const float ICON_SIZE_SMALL = 24f;
        public const float ICON_SIZE_LARGE = 48f;

        // ── Spacing ──────────────────────────────────────────
        public const float PADDING_XS = 4f;
        public const float PADDING_SM = 8f;
        public const float PADDING_MD = 16f;
        public const float PADDING_LG = 24f;
        public const float PADDING_XL = 32f;
        public const float CARD_PADDING = 12f;
        public const float SECTION_GAP = 20f;

        // ── Bordes ───────────────────────────────────────────
        public const float BORDER_RADIUS = 4f;           // Pixel art = bordes pequeños
        public const float BORDER_WIDTH = 2f;
        public const float CARD_BORDER_WIDTH = 1f;

        // ── Tamaños de fuente (pixel font) ───────────────────
        public const int FONT_SIZE_XS = 10;
        public const int FONT_SIZE_SM = 12;
        public const int FONT_SIZE_MD = 16;
        public const int FONT_SIZE_LG = 20;
        public const int FONT_SIZE_XL = 28;
        public const int FONT_SIZE_HEADER = 24;
        public const int FONT_SIZE_TITLE = 32;

        // ── Animaciones ──────────────────────────────────────
        public const float ANIM_FAST = 0.15f;
        public const float ANIM_NORMAL = 0.25f;
        public const float ANIM_SLOW = 0.4f;
        public const float TOAST_DURATION = 3.0f;
        public const float NEWS_SLIDE_DELAY = 0.1f;

        // ── Layout ───────────────────────────────────────────
        public const float SIDEBAR_WIDTH = 80f;
        public const float HEADER_HEIGHT = 64f;
        public const float FOOTER_HEIGHT = 80f;
        public const float PILOT_CARD_HEIGHT = 120f;
        public const float NEWS_CARD_HEIGHT = 90f;
        public const float STAT_BAR_HEIGHT = 12f;
        public const float STAT_BAR_WIDTH = 120f;

        // ── Pixel art ────────────────────────────────────────
        public const float SPRITE_PIXELS_PER_UNIT = 16f;
        public const float PILOT_SPRITE_SIZE = 32f;
        public const float TEAM_LOGO_SIZE = 48f;

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        /// <summary>Convierte hex string a Color de Unity</summary>
        public static Color HexColor(string hex)
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(hex, out color))
                return color;
            return Color.white;
        }

        /// <summary>Color con alpha modificado</summary>
        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>Interpola entre dos colores según un valor 0-1</summary>
        public static Color LerpStatColor(float normalized)
        {
            if (normalized > 0.75f)
                return Color.Lerp(HexColor("#FDE047"), HexColor("#22C55E"),
                    (normalized - 0.75f) * 4f);
            if (normalized > 0.5f)
                return Color.Lerp(HexColor("#FB923C"), HexColor("#FDE047"),
                    (normalized - 0.5f) * 4f);
            return Color.Lerp(HexColor("#EF4444"), HexColor("#FB923C"),
                normalized * 2f);
        }
    }
}
