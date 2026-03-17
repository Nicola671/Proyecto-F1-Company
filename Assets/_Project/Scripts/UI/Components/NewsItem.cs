// ============================================================
// F1 Career Manager — NewsItem.cs
// Tarjeta visual de noticia individual
// ============================================================
// PREFAB: NewsItem_Prefab
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using F1CareerManager.AI.PressAI;

namespace F1CareerManager.UI.Components
{
    /// <summary>
    /// Tarjeta de noticia con color por tipo.
    /// URGENTE=rojo, IMPORTANTE=amarillo, NOTICIA=azul,
    /// RUMOR=blanco con %, POSITIVO=verde, RIVAL=morado.
    /// Botón de acción directo en tarjetas urgentes.
    /// Animación slide-in al aparecer.
    /// </summary>
    public class NewsItem : MonoBehaviour
    {
        // ── Referencias UI ───────────────────────────────────
        [Header("Layout")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _typeIndicator;       // Barra lateral de color
        [SerializeField] private Image _typeBadge;           // Badge de tipo
        [SerializeField] private Text _headlineText;
        [SerializeField] private Text _bodyText;
        [SerializeField] private Text _sourceText;
        [SerializeField] private Text _typeLabel;
        [SerializeField] private Text _confidenceText;       // Solo para rumores

        [Header("Acción")]
        [SerializeField] private Button _actionButton;
        [SerializeField] private Text _actionButtonText;
        [SerializeField] private Button _dismissButton;

        [Header("Sprites")]
        [SerializeField] private Image _iconImage;

        // ── Estado ───────────────────────────────────────────
        private GeneratedNews _newsData;
        private System.Action<GeneratedNews> _onActionClicked;
        private System.Action<GeneratedNews> _onDismissed;
        private bool _isExpanded = false;

        // ══════════════════════════════════════════════════════
        // SETUP
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Configura la tarjeta con datos de noticia.
        /// </summary>
        public void Setup(GeneratedNews news,
            System.Action<GeneratedNews> onAction = null,
            System.Action<GeneratedNews> onDismiss = null)
        {
            _newsData = news;
            _onActionClicked = onAction;
            _onDismissed = onDismiss;

            // Color según tipo
            Color typeColor = GetTypeColor(news.CardType);

            // Background
            if (_backgroundImage != null)
                _backgroundImage.color = UITheme.BackgroundCard;

            // Indicador lateral de color
            if (_typeIndicator != null)
                _typeIndicator.color = typeColor;

            // Badge de tipo
            if (_typeBadge != null)
                _typeBadge.color = UITheme.WithAlpha(typeColor, 0.2f);

            if (_typeLabel != null)
            {
                _typeLabel.text = GetTypeLabel(news.CardType);
                _typeLabel.color = typeColor;
                _typeLabel.fontSize = UITheme.FONT_SIZE_XS;
            }

            // Titular
            if (_headlineText != null)
            {
                _headlineText.text = news.Headline;
                _headlineText.color = UITheme.TextPrimary;
                _headlineText.fontSize = UITheme.FONT_SIZE_MD;
            }

            // Cuerpo (oculto hasta expandir)
            if (_bodyText != null)
            {
                _bodyText.text = news.Body;
                _bodyText.color = UITheme.TextSecondary;
                _bodyText.fontSize = UITheme.FONT_SIZE_SM;
                _bodyText.gameObject.SetActive(false);
            }

            // Fuente del medio
            if (_sourceText != null)
            {
                _sourceText.text = $"📰 {news.MediaSource}";
                _sourceText.color = UITheme.TextMuted;
                _sourceText.fontSize = UITheme.FONT_SIZE_XS;
            }

            // Confianza (solo rumores)
            if (_confidenceText != null)
            {
                if (news.CardType == NewsCardType.Rumor)
                {
                    _confidenceText.gameObject.SetActive(true);
                    _confidenceText.text = $"Confianza: {news.Credibility * 100:F0}%";
                    _confidenceText.color = UITheme.TextWarning;
                    _confidenceText.fontSize = UITheme.FONT_SIZE_XS;
                }
                else
                {
                    _confidenceText.gameObject.SetActive(false);
                }
            }

            // Botón de acción (solo urgentes)
            if (_actionButton != null)
            {
                bool showAction = news.RequiresAction;
                _actionButton.gameObject.SetActive(showAction);

                if (showAction)
                {
                    _actionButton.onClick.RemoveAllListeners();
                    _actionButton.onClick.AddListener(OnActionClicked);

                    if (_actionButtonText != null)
                    {
                        _actionButtonText.text = GetActionLabel(news);
                        _actionButtonText.color = UITheme.TextPrimary;
                        _actionButtonText.fontSize = UITheme.FONT_SIZE_SM;
                    }

                    // Color del botón
                    Image btnBg = _actionButton.GetComponent<Image>();
                    if (btnBg != null) btnBg.color = typeColor;
                }
            }

            // Botón dismiss
            if (_dismissButton != null)
            {
                _dismissButton.onClick.RemoveAllListeners();
                _dismissButton.onClick.AddListener(OnDismissClicked);
            }

            // Estado de lectura
            UpdateReadState(news.IsRead);
        }

        // ══════════════════════════════════════════════════════
        // INTERACCIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>Expande/colapsa el cuerpo de la noticia</summary>
        public void ToggleExpand()
        {
            _isExpanded = !_isExpanded;

            if (_bodyText != null)
                _bodyText.gameObject.SetActive(_isExpanded);

            // Marcar como leída al expandir
            if (_isExpanded && _newsData != null && !_newsData.IsRead)
            {
                _newsData.IsRead = true;
                UpdateReadState(true);
            }
        }

        private void OnActionClicked()
        {
            _onActionClicked?.Invoke(_newsData);
        }

        private void OnDismissClicked()
        {
            if (_newsData != null) _newsData.IsRead = true;
            _onDismissed?.Invoke(_newsData);

            // Animación de salida
            if (gameObject.activeInHierarchy)
                StartCoroutine(DismissAnimation());
        }

        private void UpdateReadState(bool isRead)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = isRead
                    ? UITheme.WithAlpha(UITheme.BackgroundCard, 0.6f)
                    : UITheme.BackgroundCard;
            }
        }

        // ══════════════════════════════════════════════════════
        // ANIMACIONES
        // ══════════════════════════════════════════════════════

        /// <summary>Animación slide-in al aparecer</summary>
        public IEnumerator SlideInAnimation(float delay = 0f)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            RectTransform rt = GetComponent<RectTransform>();
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

            Vector2 startPos = rt.anchoredPosition + new Vector2(400f, 0f);
            Vector2 endPos = rt.anchoredPosition;
            cg.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < UITheme.ANIM_NORMAL)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / UITheme.ANIM_NORMAL);
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                cg.alpha = t;
                yield return null;
            }

            rt.anchoredPosition = endPos;
            cg.alpha = 1f;
        }

        /// <summary>Animación shake para noticias urgentes</summary>
        public IEnumerator ShakeAnimation()
        {
            RectTransform rt = GetComponent<RectTransform>();
            Vector2 originalPos = rt.anchoredPosition;

            for (int i = 0; i < 3; i++)
            {
                rt.anchoredPosition = originalPos + new Vector2(6f, 0f);
                yield return new WaitForSeconds(0.05f);
                rt.anchoredPosition = originalPos - new Vector2(6f, 0f);
                yield return new WaitForSeconds(0.05f);
            }

            rt.anchoredPosition = originalPos;
        }

        private IEnumerator DismissAnimation()
        {
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

            RectTransform rt = GetComponent<RectTransform>();
            Vector2 startPos = rt.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(-400f, 0f);

            float elapsed = 0f;
            while (elapsed < UITheme.ANIM_FAST)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / UITheme.ANIM_FAST;
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                cg.alpha = 1f - t;
                yield return null;
            }

            Destroy(gameObject);
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private Color GetTypeColor(NewsCardType type)
        {
            switch (type)
            {
                case NewsCardType.Urgent:    return UITheme.NewsUrgent;
                case NewsCardType.Important: return UITheme.NewsImportant;
                case NewsCardType.News:      return UITheme.NewsRegular;
                case NewsCardType.Rumor:     return UITheme.NewsRumor;
                case NewsCardType.Positive:  return UITheme.NewsPositive;
                case NewsCardType.Rival:     return UITheme.NewsRival;
                default:                     return UITheme.NewsRegular;
            }
        }

        private string GetTypeLabel(NewsCardType type)
        {
            switch (type)
            {
                case NewsCardType.Urgent:    return "🔴 URGENTE";
                case NewsCardType.Important: return "🟡 IMPORTANTE";
                case NewsCardType.News:      return "🔵 NOTICIA";
                case NewsCardType.Rumor:     return "⚪ RUMOR";
                case NewsCardType.Positive:  return "🟢 POSITIVO";
                case NewsCardType.Rival:     return "🟣 RIVAL";
                default:                     return "NOTICIA";
            }
        }

        private string GetActionLabel(GeneratedNews news)
        {
            // Inferir acción según contenido
            if (news.Headline.Contains("lesión") || news.Headline.Contains("Lesión"))
                return "Ver piloto";
            if (news.Headline.Contains("FIA") || news.Headline.Contains("sanción"))
                return "Ir a regulaciones";
            if (news.Headline.Contains("contrato") || news.Headline.Contains("firma"))
                return "Ver contrato";
            if (news.Headline.Contains("R&D") || news.Headline.Contains("técnico"))
                return "Ir a R&D";
            return "Atender";
        }
    }
}
