// ============================================================
// F1 Career Manager — StatBar.cs
// Barra de progreso pixel art — versión compacta y completa
// ============================================================
// PREFAB: StatBar_Prefab (RectTransform con Image + Text)
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace F1CareerManager.UI.Components
{
    /// <summary>
    /// Barra de stats pixel art estilizada con animación de llenado.
    /// Color dinámico según valor: verde > 75, amarillo 50-74, rojo < 50.
    /// Dos versiones: compacta (mini) y completa.
    /// </summary>
    public class StatBar : MonoBehaviour
    {
        // ── Referencias UI ───────────────────────────────────
        [Header("Componentes UI")]
        [SerializeField] private Image _backgroundBar;
        [SerializeField] private Image _fillBar;
        [SerializeField] private Text _labelText;
        [SerializeField] private Text _valueText;

        // ── Configuración ────────────────────────────────────
        [Header("Configuración")]
        [SerializeField] private bool _isCompact = false;
        [SerializeField] private bool _animateOnShow = true;
        [SerializeField] private float _animDuration = 0.4f;

        // ── Estado ───────────────────────────────────────────
        private int _currentValue;
        private int _maxValue = 100;
        private Coroutine _animCoroutine;

        // ══════════════════════════════════════════════════════
        // CONFIGURACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Configura la barra con nombre, valor y máximo.
        /// Anima el llenado si animateOnShow está activo.
        /// </summary>
        public void Setup(string label, int value, int maxValue = 100)
        {
            _currentValue = Mathf.Clamp(value, 0, maxValue);
            _maxValue = maxValue;

            if (_labelText != null)
            {
                _labelText.text = label;
                _labelText.color = UITheme.TextSecondary;
                _labelText.fontSize = _isCompact
                    ? UITheme.FONT_SIZE_XS : UITheme.FONT_SIZE_SM;
            }

            if (_valueText != null)
            {
                _valueText.text = _currentValue.ToString();
                _valueText.color = UITheme.GetStatColor(_currentValue);
                _valueText.fontSize = _isCompact
                    ? UITheme.FONT_SIZE_XS : UITheme.FONT_SIZE_SM;
            }

            if (_backgroundBar != null)
                _backgroundBar.color = UITheme.BackgroundInput;

            // Aplicar color y animación
            Color fillColor = UITheme.GetStatColor(_currentValue);
            float fillAmount = (float)_currentValue / _maxValue;

            if (_animateOnShow && gameObject.activeInHierarchy)
            {
                if (_animCoroutine != null)
                    StopCoroutine(_animCoroutine);
                _animCoroutine = StartCoroutine(AnimateFill(fillAmount, fillColor));
            }
            else
            {
                ApplyFill(fillAmount, fillColor);
            }
        }

        /// <summary>Actualiza el valor sin re-crear</summary>
        public void UpdateValue(int newValue)
        {
            int oldValue = _currentValue;
            _currentValue = Mathf.Clamp(newValue, 0, _maxValue);

            if (_valueText != null)
            {
                _valueText.text = _currentValue.ToString();
                _valueText.color = UITheme.GetStatColor(_currentValue);
            }

            Color fillColor = UITheme.GetStatColor(_currentValue);
            float fillAmount = (float)_currentValue / _maxValue;

            if (gameObject.activeInHierarchy)
            {
                if (_animCoroutine != null)
                    StopCoroutine(_animCoroutine);
                _animCoroutine = StartCoroutine(AnimateFill(fillAmount, fillColor));
            }
            else
            {
                ApplyFill(fillAmount, fillColor);
            }
        }

        /// <summary>Cambia a modo compacto o completo</summary>
        public void SetCompact(bool compact)
        {
            _isCompact = compact;

            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                float height = compact
                    ? UITheme.STAT_BAR_HEIGHT * 0.7f
                    : UITheme.STAT_BAR_HEIGHT;
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
            }

            if (_labelText != null)
                _labelText.gameObject.SetActive(!compact);
        }

        // ══════════════════════════════════════════════════════
        // ANIMACIÓN
        // ══════════════════════════════════════════════════════

        private IEnumerator AnimateFill(float targetFill, Color targetColor)
        {
            float startFill = _fillBar != null ? _fillBar.fillAmount : 0f;
            float elapsed = 0f;

            while (elapsed < _animDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _animDuration);
                float currentFill = Mathf.Lerp(startFill, targetFill, t);
                ApplyFill(currentFill, targetColor);
                yield return null;
            }

            ApplyFill(targetFill, targetColor);
            _animCoroutine = null;
        }

        private void ApplyFill(float fillAmount, Color color)
        {
            if (_fillBar != null)
            {
                _fillBar.fillAmount = fillAmount;
                _fillBar.color = color;
            }
        }
    }
}
