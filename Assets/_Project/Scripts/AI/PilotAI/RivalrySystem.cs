// ============================================================
// F1 Career Manager — RivalrySystem.cs
// Gestión de rivalidades entre pilotos (compañeros y rivales)
// Afecta humor, eventos de Prensa y comportamiento en GP.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using F1CareerManager.Core;
using F1CareerManager.AI.PilotAI;

namespace F1CareerManager.AI.PilotAI
{
    public class RivalrySystem : MonoBehaviour
    {
        public static RivalrySystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int conflictThreshold = -50;
        [SerializeField] private int highTensionThreshold = -25;
        [SerializeField] private int bromanceThreshold = 50;

        // ── Estado ───────────────────────────────────────────
        private Dictionary<string, int> relationshipMatrix = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
            Debug.Log("[RivalrySystem] ✅ Inicializado");
        }

        // ══════════════════════════════════════════════════════
        // GESTIÓN DE RELACIONES
        // ══════════════════════════════════════════════════════

        private string GetKey(string p1, string p2)
        {
            var list = new List<string> { p1, p2 };
            list.Sort();
            return $"{list[0]}_{list[1]}";
        }

        public void ModifyRelationship(string p1, string p2, int amount)
        {
            string key = GetKey(p1, p2);
            if (!relationshipMatrix.ContainsKey(key)) relationshipMatrix[key] = 0;
            
            relationshipMatrix[key] = Mathf.Clamp(relationshipMatrix[key] + amount, -100, 100);
            Debug.Log($"[RivalrySystem] Relación {p1} / {p2} cambió {amount}: {relationshipMatrix[key]}");

            CheckRivalryEvents(p1, p2, relationshipMatrix[key]);
        }

        private void CheckRivalryEvents(string p1, string p2, int level)
        {
            if (level <= conflictThreshold)
            {
                // Disparar evento de conflicto abierto para NewsGenerator / EventBus
                Debug.Log($"[RivalrySystem] ⚠️ CONFLICTO ABIERTO entre {p1} y {p2}");
                // MoodSystem.Instance?.ModifyMood(p1, -10, "Conflict with rival");
                // MoodSystem.Instance?.ModifyMood(p2, -10, "Conflict with rival");
            }
            else if (level >= bromanceThreshold)
            {
                // Disparar evento de bromance (buena relación)
                Debug.Log($"[RivalrySystem] 🔥 BROMANCE detectado entre {p1} y {p2}");
            }
        }

        // ══════════════════════════════════════════════════════
        // IMPACTO EN CARRERA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Determina la bonus/penalidad de defensa si el atacante es un rival directo
        /// </summary>
        public int GetRivalryDefenseBonus(string defenderId, string attackerId)
        {
            string key = GetKey(defenderId, attackerId);
            if (!relationshipMatrix.ContainsKey(key)) return 0;

            int level = relationshipMatrix[key];
            if (level <= conflictThreshold) return 15; // Defiende como un león contra su rival
            if (level >= bromanceThreshold) return -5; // Deja pasar más fácil a su amigo
            return 0;
        }

        /// <summary>
        /// Probabilidad de que ambos pilotos choquen si están peleando posición
        /// </summary>
        public float GetCrashRiskMultiplier(string p1, string p2)
        {
            string key = GetKey(p1, p2);
            if (!relationshipMatrix.ContainsKey(key)) return 1.0f;

            int level = relationshipMatrix[key];
            if (level <= conflictThreshold) return 2.5f; // Mucha tensión, riesgo alto
            if (level >= bromanceThreshold) return 0.5f; // Buena relación, riesgo bajo
            return 1.0f;
        }

        // ══════════════════════════════════════════════════════
        // API PÚBLICA
        // ══════════════════════════════════════════════════════

        public int GetRelationship(string p1, string p2)
        {
            string key = GetKey(p1, p2);
            return relationshipMatrix.ContainsKey(key) ? relationshipMatrix[key] : 0;
        }

        public string GetRelationshipStatus(string p1, string p2)
        {
            int level = GetRelationship(p1, p2);
            if (level <= conflictThreshold) return "Enemistad";
            if (level <= highTensionThreshold) return "Tensión";
            if (level >= bromanceThreshold) return "Amistad";
            return "Neutral";
        }
    }
}
