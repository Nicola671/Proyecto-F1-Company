// ============================================================
// F1 Career Manager — PracticeSimulator.cs
// Simulación de sesiones de Prácticas Libres (Viernes)
// Genera bonificaciones de Setup y conocimiento de pista.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.Simulation
{
    public class PracticeSimulator : MonoBehaviour
    {
        public static PracticeSimulator Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
            Debug.Log("[PracticeSimulator] ✅ Inicializado");
        }

        // ══════════════════════════════════════════════════════
        // SIMULACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Simula las 3 sesiones de prácticas y otorga bonos de Setup
        /// </summary>
        public Dictionary<string, float> SimulatePractice(string circuitId, List<PilotData> pilots)
        {
            var setupBonuses = new Dictionary<string, float>();
            Debug.Log($"[Practice] Simulando sesiones en {circuitId}...");

            foreach (var p in pilots)
            {
                // El bono de setup depende de la Experiencia del piloto y del staff
                float setupPerf = (p.stats.consistency * 0.6f) + (UnityEngine.Random.Range(5, 15));
                float bonus = Mathf.Clamp(setupPerf / 100f, 0.05f, 0.15f); // 5% a 15% de mejora
                
                setupBonuses[p.id] = bonus;
                Debug.Log($"[Practice] {p.fullName} setup bonus: +{bonus*100:F1}%");
            }

            return setupBonuses;
        }

        /// <summary>
        /// Evalúa el desgaste de neumáticos durante el long-run
        /// </summary>
        public float EstimateRacePace()
        {
            return UnityEngine.Random.Range(85f, 98f);
        }
    }
}
