// ============================================================
// F1 Career Manager — SettingsScreen.cs
// Pantalla de configuración: volumen, idioma, modo oscuro/claro
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using F1CareerManager.Core;

namespace F1CareerManager.UI.Screens
{
    public class SettingsScreen : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text musicVolumeValue;
        [SerializeField] private Text sfxVolumeValue;
        [SerializeField] private Toggle muteToggle;

        [Header("Language & Localization")]
        [SerializeField] private Dropdown languageDropdown;

        [Header("Display Settings")]
        [SerializeField] private Toggle darkModeToggle;
        [SerializeField] private Toggle notificationsToggle;
        [SerializeField] private Text themeLabel;

        [Header("User Account / Save")]
        [SerializeField] private Button changeNameButton;
        [SerializeField] private Button resetProgressButton;
        [SerializeField] private Text appVersionText;

        [Header("Actions")]
        [SerializeField] private Button backToMainMenuButton;
        [SerializeField] private Button creditsButton;

        // ── Estado ───────────────────────────────────────────
        private string currentLanguage = "ES";
        private bool isDarkMode = true;

        private void Awake()
        {
            SetupListeners();
            LoadSettings();
            
            if (appVersionText != null) appVersionText.text = $"v{Application.version}";
        }

        private void SetupListeners()
        {
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            if (muteToggle != null) muteToggle.onValueChanged.AddListener(OnMuteChanged);
            if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            if (darkModeToggle != null) darkModeToggle.onValueChanged.AddListener(OnDarkModeChanged);
            if (notificationsToggle != null) notificationsToggle.onValueChanged.AddListener(OnNotificationsChanged);
            
            if (backToMainMenuButton != null) backToMainMenuButton.onClick.AddListener(OnBackPressed);
            if (creditsButton != null) creditsButton.onClick.AddListener(OnCreditsPressed);
            if (resetProgressButton != null) resetProgressButton.onClick.AddListener(OnResetProgressPressed);
        }

        private void LoadSettings()
        {
            // Reproducir volumen guardado
            float music = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            float sfx = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
            bool mute = PlayerPrefs.GetInt("IsMuted", 0) == 1;
            int lang = PlayerPrefs.GetInt("LanguageIndex", 0);
            bool dark = PlayerPrefs.GetInt("IsDarkMode", 1) == 1;
            bool notify = PlayerPrefs.GetInt("NotificationsEnabled", 1) == 1;

            if (musicVolumeSlider != null) musicVolumeSlider.value = music;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfx;
            if (muteToggle != null) muteToggle.isOn = mute;
            if (languageDropdown != null) languageDropdown.value = lang;
            if (darkModeToggle != null) darkModeToggle.isOn = dark;
            if (notificationsToggle != null) notificationsToggle.isOn = notify;

            UpdateVolumeLabels(music, sfx);
            ApplyTheme(dark);
        }

        // ══════════════════════════════════════════════════════
        // HANDLERS
        // ══════════════════════════════════════════════════════

        private void OnMusicVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat("MusicVolume", val);
            UpdateVolumeLabels(val, sfxVolumeSlider != null ? sfxVolumeSlider.value : 0.8f);
            // AudioManager.Instance.SetMusicVolume(val);
        }

        private void OnSfxVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat("SfxVolume", val);
            UpdateVolumeLabels(musicVolumeSlider != null ? musicVolumeSlider.value : 0.7f, val);
            // AudioManager.Instance.SetSfxVolume(val);
        }

        private void OnMuteChanged(bool val)
        {
            PlayerPrefs.SetInt("IsMuted", val ? 1 : 0);
            // AudioManager.Instance.SetMute(val);
        }

        private void OnLanguageChanged(int index)
        {
            PlayerPrefs.SetInt("LanguageIndex", index);
            string langCode = index == 0 ? "ES" : "EN";
            currentLanguage = langCode;
            // LocalizationManager.Instance.SetLanguage(langCode);
            Debug.Log($"[Settings] Idioma cambiado a {langCode}");
        }

        private void OnDarkModeChanged(bool val)
        {
            PlayerPrefs.SetInt("IsDarkMode", val ? 1 : 0);
            ApplyTheme(val);
            Debug.Log($"[Settings] Tema cambiado a {(val ? "Oscuro" : "Claro")}");
        }

        private void OnNotificationsChanged(bool val)
        {
            PlayerPrefs.SetInt("NotificationsEnabled", val ? 1 : 0);
            NotificationSystem.Instance?.SetNotificationsEnabled(val);
            Debug.Log($"[Settings] Notificaciones push: {(val ? "Activadas" : "Desactivadas")}");
        }

        private void ApplyTheme(bool dark)
        {
            isDarkMode = dark;
            if (themeLabel != null) themeLabel.text = dark ? "Modo Oscuro" : "Modo Claro";
            // En una app real, aquí se acceden a los assets de color y se actualizan los materiales
        }

        private void UpdateVolumeLabels(float music, float sfx)
        {
            if (musicVolumeValue != null) musicVolumeValue.text = $"{Mathf.RoundToInt(music * 100)}%";
            if (sfxVolumeValue != null) sfxVolumeValue.text = $"{Mathf.RoundToInt(sfx * 100)}%";
        }

        // ══════════════════════════════════════════════════════
        // ACCIONES
        // ══════════════════════════════════════════════════════

        private void OnBackPressed()
        {
            PlayerPrefs.Save();
            // UIManager.Instance.NavigateTo("MainMenu");
        }

        private void OnCreditsPressed()
        {
            // UIManager.Instance.NavigateTo("Credits");
        }

        private void OnResetProgressPressed()
        {
            // Popup de confirmación
            Debug.Log("[Settings] Reset progress solicitado");
        }
    }
}
