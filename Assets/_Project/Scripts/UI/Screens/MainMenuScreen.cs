// ============================================================
// F1 Career Manager — MainMenuScreen.cs
// Pantalla de inicio del juego (New Game, Load, Settings, Credits)
// ============================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using F1CareerManager.Core;

namespace F1CareerManager.UI.Screens
{
    public class MainMenuScreen : MonoBehaviour
    {
        [Header("Menu Buttons")]
        [SerializeField] private Button newGameBtn;
        [SerializeField] private Button loadGameBtn;
        [SerializeField] private Button settingsBtn;
        [SerializeField] private Button creditsBtn;
        [SerializeField] private Button exitBtn;

        [Header("Visual Effects")]
        [SerializeField] private Animation menuAnim;
        [SerializeField] private Image logoMain;
        [SerializeField] private Text versionLabel;

        [Header("Save Slot Preview")]
        [SerializeField] private GameObject savePreviewPanel;
        [SerializeField] private Text saveDetailsText;

        // ── Estado ───────────────────────────────────────────
        private bool isFirstTime = true;

        private void Awake()
        {
            SetupListeners();
            if (versionLabel != null) versionLabel.text = $"Build {Application.version}";
        }

        private void SetupListeners()
        {
            if (newGameBtn != null) newGameBtn.onClick.AddListener(OnNewGamePressed);
            if (loadGameBtn != null) loadGameBtn.onClick.AddListener(OnLoadGamePressed);
            if (settingsBtn != null) settingsBtn.onClick.AddListener(OnSettingsPressed);
            if (creditsBtn != null) creditsBtn.onClick.AddListener(OnCreditsPressed);
            if (exitBtn != null) exitBtn.onClick.AddListener(Application.Quit);
        }

        // ══════════════════════════════════════════════════════
        // HANDLERS
        // ══════════════════════════════════════════════════════

        private void OnNewGamePressed()
        {
            Debug.Log("[MainMenu] Nueva partida iniciada.");
            // UIManager.Instance.NavigateTo("TeamSelection");
        }

        private void OnLoadGamePressed()
        {
            Debug.Log("[MainMenu] Cargando partida...");
            // SaveManager.Instance.LoadGame();
        }

        private void OnSettingsPressed()
        {
            Debug.Log("[MainMenu] Ajustes abiertos.");
            // UIManager.Instance.NavigateTo("Settings");
        }

        private void OnCreditsPressed()
        {
            Debug.Log("[MainMenu] Créditos abiertos.");
            // UIManager.Instance.NavigateTo("Credits");
        }

        // ══════════════════════════════════════════════════════
        // ANIMACIÓN (Simulada)
        // ══════════════════════════════════════════════════════

        public void PlayIntro()
        {
            if (menuAnim != null) menuAnim.Play();
            Debug.Log("[MainMenu] Intro de inicio de sesión.");
        }
    }
}
