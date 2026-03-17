// ============================================================
// F1 Career Manager — StarRating.cs
// Display de 1-5 estrellas pixel art
// ============================================================
// PREFAB: StarRating_Prefab (5 Image children)
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace F1CareerManager.UI.Components
{
    /// <summary>
    /// Display de estrellas 1-5. Versión interactiva para filtros
    /// y versión display-only. Estrellas llenas, medias y vacías.
    /// </summary>
    public class StarRating : MonoBehaviour
    {
        // ── Referencias ──────────────────────────────────────
        [Header("Estrellas (5 Image)")]
        [SerializeField] private Image[] _starImages = new Image[5];

        [Header("Sprites")]
        [SerializeField] private Sprite _starFull;
        [SerializeField] private Sprite _starHalf;
        [SerializeField] private Sprite _starEmpty;

        [Header("Configuración")]
        [SerializeField] private bool _isInteractive = false;
        [SerializeField] private float _starSize = 16f;

        // ── Estado ───────────────────────────────────────────
        private int _currentRating;
        private System.Action<int> _onRatingChanged;

        // ══════════════════════════════════════════════════════
        // SETUP
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Configura las estrellas para mostrar un rating (1-5)
        /// </summary>
        public void SetRating(int rating)
        {
            _currentRating = Mathf.Clamp(rating, 0, 5);
            Color starColor = UITheme.GetStarColor(_currentRating);

            for (int i = 0; i < 5; i++)
            {
                if (_starImages[i] == null) continue;

                if (i < _currentRating)
                {
                    _starImages[i].sprite = _starFull;
                    _starImages[i].color = starColor;
                }
                else
                {
                    _starImages[i].sprite = _starEmpty;
                    _starImages[i].color = UITheme.TextMuted;
                }

                // Tamaño pixel art
                RectTransform rt = _starImages[i].GetComponent<RectTransform>();
                if (rt != null)
                    rt.sizeDelta = new Vector2(_starSize, _starSize);
            }
        }

        /// <summary>Configura el callback para modo interactivo</summary>
        public void SetInteractive(bool interactive, System.Action<int> onChanged = null)
        {
            _isInteractive = interactive;
            _onRatingChanged = onChanged;

            // Agregar botones si es interactivo
            for (int i = 0; i < 5; i++)
            {
                if (_starImages[i] == null) continue;

                var button = _starImages[i].GetComponent<Button>();
                if (interactive)
                {
                    if (button == null)
                        button = _starImages[i].gameObject.AddComponent<Button>();

                    int rating = i + 1;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnStarClicked(rating));
                }
                else if (button != null)
                {
                    Destroy(button);
                }
            }
        }

        private void OnStarClicked(int rating)
        {
            if (!_isInteractive) return;

            // Si clickea la misma estrella, toggle off
            if (rating == _currentRating)
                rating = 0;

            SetRating(rating);
            _onRatingChanged?.Invoke(rating);
        }

        /// <summary>Obtiene el rating actual</summary>
        public int GetRating() => _currentRating;

        /// <summary>Cambia el tamaño de las estrellas</summary>
        public void SetSize(float size)
        {
            _starSize = size;
            SetRating(_currentRating); // Re-aplicar
        }
    }
}
