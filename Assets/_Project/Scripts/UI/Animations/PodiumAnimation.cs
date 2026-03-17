// ============================================================
// F1 Career Manager — PodiumAnimation.cs
// Secuencia animada del podio post-carrera
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace F1CareerManager.UI.Animations
{
    /// <summary>
    /// Secuencia: P3 entra primero, luego P2, luego P1.
    /// Sprites pixel art suben a sus posiciones.
    /// Confetti de colores del equipo ganador.
    /// </summary>
    public class PodiumAnimation : MonoBehaviour
    {
        [Header("Posiciones")]
        [SerializeField] private RectTransform _pos1;
        [SerializeField] private RectTransform _pos2;
        [SerializeField] private RectTransform _pos3;

        [Header("Sprites")]
        [SerializeField] private Image _sprite1;
        [SerializeField] private Image _sprite2;
        [SerializeField] private Image _sprite3;

        [Header("Confetti")]
        [SerializeField] private Transform _confettiContainer;
        [SerializeField] private int _confettiCount = 30;

        private Color _winnerTeamColor = UITheme.AccentGold;

        public void Play(string winnerTeamId)
        {
            _winnerTeamColor = UITheme.GetTeamColor(winnerTeamId);
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            // Ocultar todo
            SetAlpha(_pos1, 0f); SetAlpha(_pos2, 0f); SetAlpha(_pos3, 0f);

            yield return new WaitForSeconds(0.5f);

            // P3 entra desde abajo
            yield return AnimateEntry(_pos3, 80f, UITheme.ANIM_NORMAL);
            yield return new WaitForSeconds(0.3f);

            // P2 entra
            yield return AnimateEntry(_pos2, 120f, UITheme.ANIM_NORMAL);
            yield return new WaitForSeconds(0.3f);

            // P1 entra con más altura
            yield return AnimateEntry(_pos1, 160f, UITheme.ANIM_SLOW);

            // Confetti
            yield return SpawnConfetti();
        }

        private IEnumerator AnimateEntry(RectTransform rt, float height,
            float duration)
        {
            if (rt == null) yield break;

            CanvasGroup cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();

            Vector2 start = rt.anchoredPosition - new Vector2(0, height);
            Vector2 end = rt.anchoredPosition;
            rt.anchoredPosition = start;
            cg.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                rt.anchoredPosition = Vector2.Lerp(start, end, t);
                cg.alpha = t;
                yield return null;
            }

            rt.anchoredPosition = end;
            cg.alpha = 1f;
        }

        private IEnumerator SpawnConfetti()
        {
            if (_confettiContainer == null) yield break;

            Color secondary = UITheme.GetTeamSecondaryColor("");
            System.Random rng = new System.Random();

            for (int i = 0; i < _confettiCount; i++)
            {
                GameObject piece = new GameObject("Confetti",
                    typeof(RectTransform), typeof(Image));
                piece.transform.SetParent(_confettiContainer, false);

                Image img = piece.GetComponent<Image>();
                img.color = rng.Next(2) == 0 ? _winnerTeamColor : UITheme.AccentGold;

                RectTransform rt = piece.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(
                    rng.Next(3, 8), rng.Next(3, 8));
                rt.anchoredPosition = new Vector2(
                    rng.Next(-200, 200), 300);

                StartCoroutine(FallConfetti(rt, rng));

                if (i % 5 == 0)
                    yield return new WaitForSeconds(0.05f);
            }
        }

        private IEnumerator FallConfetti(RectTransform rt, System.Random rng)
        {
            float speed = rng.Next(100, 300);
            float sway = rng.Next(20, 60);
            float elapsed = 0f;
            float lifetime = rng.Next(15, 30) / 10f;
            Vector2 start = rt.anchoredPosition;

            while (elapsed < lifetime && rt != null)
            {
                elapsed += Time.deltaTime;
                float x = start.x + Mathf.Sin(elapsed * 3f) * sway;
                float y = start.y - speed * elapsed;
                rt.anchoredPosition = new Vector2(x, y);
                rt.localRotation = Quaternion.Euler(0, 0, elapsed * 180f);
                yield return null;
            }

            if (rt != null) Destroy(rt.gameObject);
        }

        private void SetAlpha(RectTransform rt, float alpha)
        {
            if (rt == null) return;
            CanvasGroup cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = alpha;
        }
    }
}
