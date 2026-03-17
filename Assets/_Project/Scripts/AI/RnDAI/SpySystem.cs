// ============================================================
// F1 Career Manager — SpySystem.cs
// Sistema de espionaje industrial entre equipos
// Permite conocer componentes y presupuestos de rivales.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.AI.RnDAI
{
    public class SpySystem : MonoBehaviour
    {
        public static SpySystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private long spyCost = 500000; // $500k por intento
        [SerializeField] private int baseSuccessChance = 65;

        // ── Estado ───────────────────────────────────────────
        private Dictionary<string, SpyData> intelMap = new Dictionary<string, SpyData>();

        [Serializable]
        public class SpyData
        {
            public string teamId;
            public int intelLevel; // 0-100: determina qué stats se ven
            public bool budgetKnown;
            public List<string> knownComponents;
            public int lastWeekUpdated;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
            Debug.Log("[SpySystem] ✅ Inicializado");
        }

        // ══════════════════════════════════════════════════════
        // ACCIONES DE ESPIONAJE
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Intenta obtener información de un equipo rival
        /// </summary>
        public bool PerformEspionage(string targetTeamId)
        {
            // 1. Cobrar costo de espionaje vía BudgetManager
            // 2. Simular éxito basado en personal (CommsDirector influye)
            
            bool success = UnityEngine.Random.Range(0, 100) < baseSuccessChance;
            
            if (success)
            {
                if (!intelMap.ContainsKey(targetTeamId)) intelMap[targetTeamId] = new SpyData { teamId = targetTeamId };
                
                var data = intelMap[targetTeamId];
                data.intelLevel = Mathf.Min(data.intelLevel + 25, 100);
                data.budgetKnown = data.intelLevel >= 50;
                data.lastWeekUpdated = 1; // dummy de semana
                
                Debug.Log($"[SpySystem] ¡Éxito! Intel de {targetTeamId} subió a {data.intelLevel}%");
            }
            else
            {
                // Riesgo de ser descubierto y multado por la FIA
                if (UnityEngine.Random.Range(0, 100) < 15)
                {
                    Debug.LogWarning("[SpySystem] ⚠️ ¡DESCUBIERTOS! La FIA ha sido notificada.");
                    // SanctionSystem.Instance.ApplyFine(5000000, "Espionaje industrial");
                }
            }
            
            return success;
        }

        // ══════════════════════════════════════════════════════
        // ACCESO A DATOS (PROTEGIDO)
        // ══════════════════════════════════════════════════════

        public bool IsBudgetKnown(string teamId)
        {
            return intelMap.ContainsKey(teamId) && intelMap[teamId].budgetKnown;
        }

        public int GetIntelLevel(string teamId)
        {
            return intelMap.ContainsKey(teamId) ? intelMap[teamId].intelLevel : 0;
        }
    }
}
