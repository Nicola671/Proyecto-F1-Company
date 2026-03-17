// ============================================================
// F1 Career Manager — MarketWindow.cs
// Sistema de ventana de mercado (Fechas de apertura/cierre)
// ============================================================

using System;
using UnityEngine;
using F1CareerManager.Core;

namespace F1CareerManager.Market
{
    public class MarketWindow : MonoBehaviour
    {
        public static MarketWindow Instance { get; private set; }

        public enum WindowStatus { Closed, SoftOpen, Open }

        // ── Estado ───────────────────────────────────────────
        private WindowStatus currentStatus = WindowStatus.Closed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Actualiza el estado de la ventana según la semana de la temporada
        /// </summary>
        public void UpdateMarketStatus(int currentWeek)
        {
            // Abierto de semana 15 a 45 (Silly Season y fin de año)
            if (currentWeek >= 15 && currentWeek <= 25)
                currentStatus = WindowStatus.SoftOpen; // Solo rumores
            else if (currentWeek > 25 && currentWeek <= 45)
                currentStatus = WindowStatus.Open; // Contrataciones permitidas
            else
                currentStatus = WindowStatus.Closed;

            Debug.Log($"[Market] Estado de ventana: {currentStatus}");
        }

        public bool IsTradingAllowed() => currentStatus == WindowStatus.Open;
        public WindowStatus GetCurrentStatus() => currentStatus;
    }
}
