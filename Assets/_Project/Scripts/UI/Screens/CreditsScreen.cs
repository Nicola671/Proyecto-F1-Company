// ============================================================
// F1 Career Manager — CreditsScreen.cs
// Pantalla de créditos con auto-scrolling
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace F1CareerManager.UI.Screens
{
    public class CreditsScreen : MonoBehaviour
    {
        [Header("Scroll Configuration")]
        [SerializeField] private RectTransform scrollContent;
        [SerializeField] private float scrollSpeed = 30f;
        [SerializeField] private float startDelay = 1f;

        [Header("UI Reference")]
        [SerializeField] private Button backButton;
        [SerializeField] private Text creditsText;

        // ── Estado ───────────────────────────────────────────
        private bool isScrolling = false;
        private Vector2 initialPos;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackPressed);
            if (scrollContent != null) initialPos = scrollContent.anchoredPosition;
        }

        private void OnEnable()
        {
            ResetScroll();
            StartCoroutine(ScrollCredits());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            isScrolling = false;
        }

        private void ResetScroll()
        {
            if (scrollContent != null)
                scrollContent.anchoredPosition = initialPos;
        }

        private IEnumerator ScrollCredits()
        {
            yield return new WaitForSeconds(startDelay);
            isScrolling = true;

            float contentHeight = scrollContent != null ? scrollContent.rect.height : 1000f;
            float targetY = contentHeight + 500f; // Un margen extra

            while (isScrolling && scrollContent.anchoredPosition.y < targetY)
            {
                scrollContent.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
                yield return null;
            }

            // Al terminar el scroll, esperar un poco y volver?
            yield return new WaitForSeconds(2f);
            // Si no se tocó volver, podrías volver al menú principal solo
            // OnBackPressed();
        }

        private void OnBackPressed()
        {
            isScrolling = false;
            // UIManager.Instance.NavigateTo("MainMenu");
        }

        // ══════════════════════════════════════════════════════
        // TEXTO DE CRÉDITOS
        // ══════════════════════════════════════════════════════

        // Podrías cargarlo desde un archivo externo .txt si quisieras
        [ContextMenu("Set Default Credits")]
        public void SetDefaultCreditsText()
        {
            if (creditsText == null) return;

            creditsText.text = @"
<b>F1 CAREER MANAGER</b>

DIRECTOR DEL PROYECTO
<color=#00bcd4>NICO</color>

PROGRAMACIÓN CORE & IA
Antigravity (Claude Opus)

DISEÑO DE JUEGO & GDD
NICO & Antigravity

SISTEMA DE EVENTOS
F1CM Team

MÚSICA & AUDIO
Pixel Music FX

DISEÑO GRÁFICO (PIXEL ART)
F1CM Design Crew

DATOS DE TEMPORADA 2025
FIA Official Data (Simulado)

AGRADECIMIENTOS ESPECIALES
A todos los fanáticos de la F1

DESARROLLADO CON UNITY 2022.3 LTS

© 2026 PROYECTO F1 COMPANY
";
        }
    }
}
