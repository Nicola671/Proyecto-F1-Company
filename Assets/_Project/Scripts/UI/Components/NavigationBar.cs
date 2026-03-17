// ============================================================
// F1 Career Manager — NavigationBar.cs
// Barra lateral de navegación del hub
// ============================================================
// PREFAB: NavBar_Prefab (Panel vertical de botones)
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace F1CareerManager.UI.Components
{
    /// <summary>
    /// Barra lateral de navegación con íconos pixel art.
    /// Badge numérico para notificaciones pendientes.
    /// Highlight de sección activa.
    /// </summary>
    public class NavigationBar : MonoBehaviour
    {
        // ── Datos de un botón de nav ─────────────────────────
        [System.Serializable]
        public class NavItem
        {
            public string Id;
            public string Label;
            public string Emoji;
            public Button Button;
            public Text EmojiText;
            public Text LabelText;
            public Image Background;
            public GameObject BadgeObj;
            public Text BadgeText;
            public int NotificationCount;
        }

        // ── Referencias ──────────────────────────────────────
        [Header("Items de navegación")]
        [SerializeField] private List<NavItem> _navItems = new List<NavItem>();

        [Header("Contenedor")]
        [SerializeField] private Transform _buttonContainer;

        // ── Estado ───────────────────────────────────────────
        private string _activeSection = "hub";
        private System.Action<string> _onSectionChanged;

        // ── Definición de secciones ──────────────────────────
        private static readonly string[][] SECTIONS = {
            new[] { "hub",     "🏠", "Hub" },
            new[] { "garage",  "🏎️", "Garaje" },
            new[] { "pilots",  "👤", "Pilotos" },
            new[] { "race",    "📅", "Carrera" },
            new[] { "finance", "💵", "Finanzas" },
            new[] { "market",  "🛒", "Mercado" },
            new[] { "rnd",     "🏭", "R&D" },
            new[] { "staff",   "👔", "Staff" }
        };

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Inicializa la barra con un callback de cambio de sección
        /// </summary>
        public void Initialize(System.Action<string> onSectionChanged)
        {
            _onSectionChanged = onSectionChanged;

            // Si no hay items creados, crear por código
            if (_navItems.Count == 0 && _buttonContainer != null)
            {
                CreateNavItemsFromCode();
            }

            // Configurar todos los botones
            foreach (var item in _navItems)
            {
                SetupNavItem(item);
            }

            // Activar hub por defecto
            SetActiveSection("hub");
        }

        private void CreateNavItemsFromCode()
        {
            foreach (var section in SECTIONS)
            {
                var item = new NavItem
                {
                    Id = section[0],
                    Emoji = section[1],
                    Label = section[2]
                };

                // Crear GameObject del botón
                GameObject btnObj = new GameObject($"Nav_{item.Id}",
                    typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(_buttonContainer, false);

                RectTransform rt = btnObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(UITheme.SIDEBAR_WIDTH,
                    UITheme.SIDEBAR_WIDTH);

                item.Background = btnObj.GetComponent<Image>();
                item.Background.color = Color.clear;
                item.Button = btnObj.GetComponent<Button>();

                // Emoji
                GameObject emojiObj = new GameObject("Emoji", typeof(Text));
                emojiObj.transform.SetParent(btnObj.transform, false);
                item.EmojiText = emojiObj.GetComponent<Text>();
                item.EmojiText.text = item.Emoji;
                item.EmojiText.fontSize = UITheme.FONT_SIZE_LG;
                item.EmojiText.alignment = TextAnchor.MiddleCenter;
                RectTransform emojiRt = emojiObj.GetComponent<RectTransform>();
                emojiRt.anchorMin = new Vector2(0, 0.3f);
                emojiRt.anchorMax = new Vector2(1, 1);
                emojiRt.offsetMin = Vector2.zero;
                emojiRt.offsetMax = Vector2.zero;

                // Label
                GameObject labelObj = new GameObject("Label", typeof(Text));
                labelObj.transform.SetParent(btnObj.transform, false);
                item.LabelText = labelObj.GetComponent<Text>();
                item.LabelText.text = item.Label;
                item.LabelText.fontSize = UITheme.FONT_SIZE_XS;
                item.LabelText.alignment = TextAnchor.MiddleCenter;
                item.LabelText.color = UITheme.TextMuted;
                RectTransform labelRt = labelObj.GetComponent<RectTransform>();
                labelRt.anchorMin = new Vector2(0, 0);
                labelRt.anchorMax = new Vector2(1, 0.35f);
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;

                // Badge de notificación
                GameObject badgeObj = new GameObject("Badge",
                    typeof(RectTransform), typeof(Image));
                badgeObj.transform.SetParent(btnObj.transform, false);
                Image badgeBg = badgeObj.GetComponent<Image>();
                badgeBg.color = UITheme.AccentPrimary;
                RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(1, 1);
                badgeRt.anchorMax = new Vector2(1, 1);
                badgeRt.pivot = new Vector2(1, 1);
                badgeRt.sizeDelta = new Vector2(18f, 18f);
                badgeRt.anchoredPosition = new Vector2(-4f, -4f);

                GameObject badgeTextObj = new GameObject("Count", typeof(Text));
                badgeTextObj.transform.SetParent(badgeObj.transform, false);
                item.BadgeText = badgeTextObj.GetComponent<Text>();
                item.BadgeText.fontSize = UITheme.FONT_SIZE_XS;
                item.BadgeText.color = UITheme.TextPrimary;
                item.BadgeText.alignment = TextAnchor.MiddleCenter;
                RectTransform btRt = badgeTextObj.GetComponent<RectTransform>();
                btRt.anchorMin = Vector2.zero;
                btRt.anchorMax = Vector2.one;
                btRt.offsetMin = Vector2.zero;
                btRt.offsetMax = Vector2.zero;

                item.BadgeObj = badgeObj;
                badgeObj.SetActive(false);

                _navItems.Add(item);
            }
        }

        private void SetupNavItem(NavItem item)
        {
            if (item.Button != null)
            {
                item.Button.onClick.RemoveAllListeners();
                string sectionId = item.Id;
                item.Button.onClick.AddListener(() => OnNavClicked(sectionId));
            }
        }

        // ══════════════════════════════════════════════════════
        // NAVEGACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>Cambia la sección activa visualmente</summary>
        public void SetActiveSection(string sectionId)
        {
            _activeSection = sectionId;

            foreach (var item in _navItems)
            {
                bool isActive = item.Id == sectionId;

                if (item.Background != null)
                {
                    item.Background.color = isActive
                        ? UITheme.WithAlpha(UITheme.AccentPrimary, 0.15f)
                        : Color.clear;
                }

                if (item.LabelText != null)
                {
                    item.LabelText.color = isActive
                        ? UITheme.AccentPrimary : UITheme.TextMuted;
                }

                if (item.EmojiText != null)
                {
                    item.EmojiText.color = isActive
                        ? UITheme.TextPrimary : UITheme.TextSecondary;
                }
            }
        }

        private void OnNavClicked(string sectionId)
        {
            if (sectionId == _activeSection) return;

            SetActiveSection(sectionId);
            _onSectionChanged?.Invoke(sectionId);
        }

        // ══════════════════════════════════════════════════════
        // BADGES
        // ══════════════════════════════════════════════════════

        /// <summary>Actualiza el badge numérico de una sección</summary>
        public void SetBadge(string sectionId, int count)
        {
            var item = _navItems.Find(i => i.Id == sectionId);
            if (item == null) return;

            item.NotificationCount = count;

            if (item.BadgeObj != null)
                item.BadgeObj.SetActive(count > 0);

            if (item.BadgeText != null)
                item.BadgeText.text = count > 99 ? "99+" : count.ToString();
        }

        /// <summary>Obtiene la sección activa</summary>
        public string GetActiveSection() => _activeSection;
    }
}
