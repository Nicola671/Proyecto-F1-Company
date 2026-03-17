// ============================================================
// F1 Career Manager — PressScreen.cs
// Pantalla de Media Hub: Noticias y Rumores del Paddock
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.UI.Screens
{
    public class PressScreen : MonoBehaviour
    {
        [Header("Media Filters")]
        [SerializeField] private Button newsTabBtn;
        [SerializeField] private Button rumorsTabBtn;
        [SerializeField] private Button interviewsTabBtn;

        [Header("Content Feed")]
        [SerializeField] private Transform feedContainer;
        [SerializeField] private GameObject newsItemPrefab;
        [SerializeField] private GameObject rumorItemPrefab;

        [Header("Trending Topics")]
        [SerializeField] private Text topHeadline;
        [SerializeField] private Text driverPopularityTrend;
        [SerializeField] private Text teamPrestigeTrend;

        // ── Estado ───────────────────────────────────────────
        private string currentTab = "NEWS";

        // ══════════════════════════════════════════════════════
        // NAVEGACIÓN
        // ══════════════════════════════════════════════════════

        public void SwitchTab(string tab)
        {
            currentTab = tab;
            RefreshFeed();
            Debug.Log($"[PressScreen] Filtro: {tab}");
        }

        public void RefreshFeed()
        {
            if (feedContainer == null) return;
            
            // Limpia feed pre-existente
            foreach (Transform t in feedContainer) Destroy(t.gameObject);

            // Poblar según el tab (Dummy de lógica)
            Debug.Log($"[PressScreen] Poblando feed de {currentTab}...");
            
            // Aquí se llamarían a NewsGenerator.Instance.GetLatestNews() o RumorSystem.Instance.GetActiveRumors()
        }

        public void UpdateTrends(string headline, string driverTrend, string teamTrend)
        {
            if (topHeadline != null) topHeadline.text = headline;
            if (driverPopularityTrend != null) driverPopularityTrend.text = $"📈 {driverTrend}";
            if (teamPrestigeTrend != null) teamPrestigeTrend.text = $"📊 {teamTrend}";
        }
    }
}
