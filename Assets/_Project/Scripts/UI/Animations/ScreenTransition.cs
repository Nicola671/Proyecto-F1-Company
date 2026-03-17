// ============================================================
// F1 Career Manager — ScreenTransition.cs
// Transiciones entre pantallas — fade o slide
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace F1CareerManager.UI.Animations
{
    /// <summary>
    /// Transición entre pantallas: fade o slide.
    /// Duración: 0.2-0.3 segundos.
    /// No bloquea input durante transición.
    /// </summary>
    public class ScreenTransition : MonoBehaviour
    {
        public static ScreenTransition Instance { get; private set; }

        public enum TransitionType { Fade, SlideLeft, SlideRight, SlideUp }

        [SerializeField] private Image _fadeOverlay;
        private bool _isTransitioning;

        private void Awake() { Instance = this; }

        /// <summary>
        /// Ejecuta transición: oculta screenA, muestra screenB
        /// </summary>
        public void Transition(GameObject from, GameObject to,
            TransitionType type = TransitionType.Fade,
            System.Action onComplete = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(DoTransition(from, to, type, onComplete));
        }

        private IEnumerator DoTransition(GameObject from, GameObject to,
            TransitionType type, System.Action onComplete)
        {
            _isTransitioning = true;
            float duration = UITheme.ANIM_NORMAL;

            switch (type)
            {
                case TransitionType.Fade:
                    yield return FadeTransition(from, to, duration);
                    break;
                case TransitionType.SlideLeft:
                    yield return SlideTransition(from, to, Vector2.left, duration);
                    break;
                case TransitionType.SlideRight:
                    yield return SlideTransition(from, to, Vector2.right, duration);
                    break;
                case TransitionType.SlideUp:
                    yield return SlideTransition(from, to, Vector2.up, duration);
                    break;
            }

            _isTransitioning = false;
            onComplete?.Invoke();
        }

        private IEnumerator FadeTransition(GameObject from, GameObject to,
            float duration)
        {
            // Fade out
            CanvasGroup cgFrom = GetOrAddCG(from);
            float elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                cgFrom.alpha = 1f - (elapsed / (duration * 0.5f));
                yield return null;
            }

            from.SetActive(false);
            cgFrom.alpha = 1f;

            // Fade in
            to.SetActive(true);
            CanvasGroup cgTo = GetOrAddCG(to);
            cgTo.alpha = 0f;
            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                cgTo.alpha = elapsed / (duration * 0.5f);
                yield return null;
            }
            cgTo.alpha = 1f;
        }

        private IEnumerator SlideTransition(GameObject from, GameObject to,
            Vector2 direction, float duration)
        {
            RectTransform rtFrom = from.GetComponent<RectTransform>();
            RectTransform rtTo = to.GetComponent<RectTransform>();

            to.SetActive(true);
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 fromStart = Vector2.zero;
            Vector2 fromEnd = direction * screenSize.x;
            Vector2 toStart = -direction * screenSize.x;
            Vector2 toEnd = Vector2.zero;

            if (rtTo != null) rtTo.anchoredPosition = toStart;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (rtFrom != null)
                    rtFrom.anchoredPosition = Vector2.Lerp(fromStart, fromEnd, t);
                if (rtTo != null)
                    rtTo.anchoredPosition = Vector2.Lerp(toStart, toEnd, t);

                yield return null;
            }

            from.SetActive(false);
            if (rtFrom != null) rtFrom.anchoredPosition = Vector2.zero;
            if (rtTo != null) rtTo.anchoredPosition = Vector2.zero;
        }

        private CanvasGroup GetOrAddCG(GameObject obj)
        {
            var cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.AddComponent<CanvasGroup>();
            return cg;
        }

        public bool IsTransitioning => _isTransitioning;
    }
}
