// ============================================================
// F1 Career Manager — NotificationToast.cs
// Toast de notificación — esquina superior, auto-desaparece
// ============================================================
// PREFAB: Toast_Prefab (Panel con Text + Icon)
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace F1CareerManager.UI.Components
{
    /// <summary>Tipo de toast</summary>
    public enum ToastType { Info, Warning, Success, Error }

    /// <summary>
    /// Muestra toasts en la esquina superior. Auto-desaparecen en 3s.
    /// Cola de notificaciones si llegan varias juntas.
    /// </summary>
    public class NotificationToast : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static NotificationToast Instance { get; private set; }

        // ── Referencias ──────────────────────────────────────
        [Header("Toast Template")]
        [SerializeField] private GameObject _toastPrefab;
        [SerializeField] private Transform _toastContainer;

        // ── Cola ─────────────────────────────────────────────
        private Queue<ToastData> _queue = new Queue<ToastData>();
        private int _activeToasts = 0;
        private const int MAX_VISIBLE = 3;

        private void Awake()
        {
            Instance = this;
        }

        // ══════════════════════════════════════════════════════
        // API PÚBLICA
        // ══════════════════════════════════════════════════════

        /// <summary>Muestra un toast informativo</summary>
        public static void ShowInfo(string message) =>
            Instance?.Enqueue(message, ToastType.Info);

        /// <summary>Muestra un toast de warning</summary>
        public static void ShowWarning(string message) =>
            Instance?.Enqueue(message, ToastType.Warning);

        /// <summary>Muestra un toast de éxito</summary>
        public static void ShowSuccess(string message) =>
            Instance?.Enqueue(message, ToastType.Success);

        /// <summary>Muestra un toast de error</summary>
        public static void ShowError(string message) =>
            Instance?.Enqueue(message, ToastType.Error);

        // ══════════════════════════════════════════════════════
        // GESTIÓN DE COLA
        // ══════════════════════════════════════════════════════

        private void Enqueue(string message, ToastType type)
        {
            _queue.Enqueue(new ToastData { Message = message, Type = type });
            TryShowNext();
        }

        private void TryShowNext()
        {
            if (_activeToasts >= MAX_VISIBLE || _queue.Count == 0) return;

            var data = _queue.Dequeue();
            _activeToasts++;
            StartCoroutine(ShowToastCoroutine(data));
        }

        private IEnumerator ShowToastCoroutine(ToastData data)
        {
            // Crear toast
            GameObject toastObj = null;
            if (_toastPrefab != null && _toastContainer != null)
            {
                toastObj = Instantiate(_toastPrefab, _toastContainer);
            }
            else
            {
                // Crear toast por código si no hay prefab
                toastObj = CreateToastUI(data);
            }

            if (toastObj == null)
            {
                _activeToasts--;
                TryShowNext();
                yield break;
            }

            // Configurar visual
            ConfigureToast(toastObj, data);

            // Animación de entrada (slide desde derecha)
            CanvasGroup cg = toastObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = toastObj.AddComponent<CanvasGroup>();

            RectTransform rt = toastObj.GetComponent<RectTransform>();
            Vector2 startPos = rt.anchoredPosition + new Vector2(300f, 0f);
            Vector2 endPos = rt.anchoredPosition;

            float elapsed = 0f;
            while (elapsed < UITheme.ANIM_NORMAL)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / UITheme.ANIM_NORMAL);
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                cg.alpha = t;
                yield return null;
            }

            // Esperar duración del toast
            yield return new WaitForSeconds(UITheme.TOAST_DURATION);

            // Animación de salida (fade out)
            elapsed = 0f;
            while (elapsed < UITheme.ANIM_FAST)
            {
                elapsed += Time.deltaTime;
                float t = 1f - (elapsed / UITheme.ANIM_FAST);
                cg.alpha = t;
                yield return null;
            }

            Destroy(toastObj);
            _activeToasts--;
            TryShowNext();
        }

        // ══════════════════════════════════════════════════════
        // CREACIÓN POR CÓDIGO (fallback sin prefab)
        // ══════════════════════════════════════════════════════

        private GameObject CreateToastUI(ToastData data)
        {
            GameObject toast = new GameObject("Toast", typeof(RectTransform));
            toast.transform.SetParent(_toastContainer ?? transform, false);

            RectTransform rt = toast.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(280f, 50f);
            rt.anchoredPosition = new Vector2(-UITheme.PADDING_MD, -UITheme.PADDING_MD
                - (_activeToasts - 1) * 58f);

            // Background
            Image bg = toast.AddComponent<Image>();
            bg.color = UITheme.BackgroundCard;

            // Barra de color lateral
            GameObject colorBar = new GameObject("ColorBar", typeof(RectTransform));
            colorBar.transform.SetParent(toast.transform, false);
            Image barImg = colorBar.AddComponent<Image>();
            barImg.color = GetToastColor(data.Type);
            RectTransform barRt = colorBar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.sizeDelta = new Vector2(4f, 0f);
            barRt.anchoredPosition = Vector2.zero;

            // Texto
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(toast.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.text = data.Message;
            text.color = UITheme.TextPrimary;
            text.fontSize = UITheme.FONT_SIZE_SM;
            text.alignment = TextAnchor.MiddleLeft;
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(UITheme.PADDING_MD, UITheme.PADDING_XS);
            textRt.offsetMax = new Vector2(-UITheme.PADDING_XS, -UITheme.PADDING_XS);

            return toast;
        }

        private void ConfigureToast(GameObject toast, ToastData data)
        {
            // Buscar texto en hijos
            Text text = toast.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = data.Message;
                text.color = UITheme.TextPrimary;
            }

            // Buscar barra de color
            Transform bar = toast.transform.Find("ColorBar");
            if (bar != null)
            {
                Image barImg = bar.GetComponent<Image>();
                if (barImg != null)
                    barImg.color = GetToastColor(data.Type);
            }
        }

        private Color GetToastColor(ToastType type)
        {
            switch (type)
            {
                case ToastType.Info: return UITheme.AccentTertiary;
                case ToastType.Warning: return UITheme.TextWarning;
                case ToastType.Success: return UITheme.TextPositive;
                case ToastType.Error: return UITheme.TextNegative;
                default: return UITheme.TextSecondary;
            }
        }

        private class ToastData
        {
            public string Message;
            public ToastType Type;
        }
    }
}
