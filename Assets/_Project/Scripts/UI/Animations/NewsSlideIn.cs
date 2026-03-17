// ============================================================
// F1 Career Manager — NewsSlideIn.cs
// Animación de entrada de tarjetas de noticias
// ============================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace F1CareerManager.UI.Animations
{
    /// <summary>
    /// Las tarjetas de news entran desde el costado.
    /// Stagger: cada tarjeta con 0.1s de delay.
    /// Urgentes tienen shake animation adicional.
    /// </summary>
    public class NewsSlideIn : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private float _slideDistance = 400f;
        [SerializeField] private float _staggerDelay = 0.1f;
        [SerializeField] private float _slideDuration = 0.25f;
        [SerializeField] private float _shakeIntensity = 8f;
        [SerializeField] private int _shakeCount = 3;

        /// <summary>
        /// Anima una lista de items con stagger.
        /// </summary>
        public void AnimateItems(List<RectTransform> items,
            List<bool> isUrgent = null)
        {
            StartCoroutine(AnimateSequence(items, isUrgent));
        }

        /// <summary>
        /// Anima un solo item.
        /// </summary>
        public void AnimateSingle(RectTransform item, bool urgent,
            float delay = 0f)
        {
            StartCoroutine(AnimateSingleItem(item, urgent, delay));
        }

        private IEnumerator AnimateSequence(List<RectTransform> items,
            List<bool> isUrgent)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;

                bool urgent = isUrgent != null && i < isUrgent.Count
                    && isUrgent[i];

                StartCoroutine(AnimateSingleItem(
                    items[i], urgent, i * _staggerDelay));
            }
            yield return null;
        }

        private IEnumerator AnimateSingleItem(RectTransform rt,
            bool isUrgent, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            CanvasGroup cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();

            // Posición inicial
            Vector2 endPos = rt.anchoredPosition;
            Vector2 startPos = endPos + new Vector2(_slideDistance, 0f);
            rt.anchoredPosition = startPos;
            cg.alpha = 0f;

            // Slide in
            float elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _slideDuration);
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                cg.alpha = t;
                yield return null;
            }

            rt.anchoredPosition = endPos;
            cg.alpha = 1f;

            // Shake para urgentes
            if (isUrgent)
            {
                yield return new WaitForSeconds(0.05f);
                yield return ShakeAnimation(rt);
            }
        }

        private IEnumerator ShakeAnimation(RectTransform rt)
        {
            Vector2 original = rt.anchoredPosition;

            for (int i = 0; i < _shakeCount; i++)
            {
                float intensity = _shakeIntensity * (1f - (float)i / _shakeCount);
                rt.anchoredPosition = original + new Vector2(intensity, 0f);
                yield return new WaitForSeconds(0.04f);
                rt.anchoredPosition = original - new Vector2(intensity, 0f);
                yield return new WaitForSeconds(0.04f);
            }

            rt.anchoredPosition = original;
        }
    }
}
