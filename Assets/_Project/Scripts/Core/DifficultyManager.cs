// ============================================================
// F1 Career Manager — DifficultyManager.cs
// Centraliza el impacto de la dificultad en todos los sistemas.
// Lee difficulty.json y expone los modificadores.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using F1CareerManager.Core;

namespace F1CareerManager.Core
{
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [Serializable]
        public class DifficultyLevel
        {
            public string id;
            public string name;
            public Dictionary<string, float> modifiers = new Dictionary<string, float>();
        }

        // ── Estado ───────────────────────────────────────────
        private string currentDifficultyId = "STANDARD";
        private DifficultyLevel currentLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(string difficultyId)
        {
            currentDifficultyId = difficultyId;
            LoadDifficultySettings();
            Debug.Log($"[DifficultyManager] ✅ Nivel {difficultyId} aplicado.");
        }

        private void LoadDifficultySettings()
        {
            // En una implementación real, esto leería el JSON y llenaría el diccionario
            // Por ahora, simulamos los multiplicadores que los otros mánagers pedirán
            currentLevel = new DifficultyLevel { id = currentDifficultyId };
        }

        // ══════════════════════════════════════════════════════
        // API PARA OTROS SISTEMAS
        // ══════════════════════════════════════════════════════

        public float GetModifier(string key, float defaultValue = 1.0f)
        {
            // Ejemplo de claves: "ai_speed", "income_mult", "fia_strictness", "player_mood_decay"
            return currentDifficultyId switch
            {
                "NARRATIVE" => GetNarrativeMod(key),
                "DEMANDING" => GetDemandingMod(key),
                "LEGEND" => GetLegendMod(key),
                _ => 1.0f // STANDARD
            };
        }

        private float GetNarrativeMod(string key) => key switch {
            "ai_speed" => 0.85f, "income_mult" => 1.5f, "fia_strictness" => 0.5f, _ => 1.0f
        };

        private float GetDemandingMod(string key) => key switch {
            "ai_speed" => 1.10f, "income_mult" => 0.8f, "fia_strictness" => 1.4f, _ => 1.0f
        };

        private float GetLegendMod(string key) => key switch {
            "ai_speed" => 1.25f, "income_mult" => 0.5f, "fia_strictness" => 2.0f, _ => 1.0f
        };
    }
}
