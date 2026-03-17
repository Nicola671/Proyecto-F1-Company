// ============================================================
// F1 Career Manager — QualifyingSimulator.cs
// Simulación de sesión de Clasificación (Sábado)
// Genera grill de salida para el Gran Premio.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.Simulation
{
    public class QualifyingSimulator : MonoBehaviour
    {
        public static QualifyingSimulator Instance { get; private set; }

        [Serializable]
        public class QualyResult
        {
            public string pilotId;
            public int round; // Q1, Q2, Q3
            public float lapTimeSeconds;
            public int position;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
            Debug.Log("[QualifyingSimulator] ✅ Inicializado");
        }

        // ══════════════════════════════════════════════════════
        // SIMULACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Simula la pole position para un circuito y lista de pilotos
        /// </summary>
        public List<QualyResult> RunQualifying(string circuitId, List<PilotData> pilots, float circuitLengthKm, float avgLapTime)
        {
            Debug.Log($"[Qualifying] Sesión iniciada en {circuitId}...");
            var results = new List<QualyResult>();

            foreach (var p in pilots)
            {
                // Un Qualy-lap depende de Speed (70%) + Aero (20%) + Engine (10%)
                float pilotFactor = p.stats.speed;
                float carFactor = (p.teamId != null) ? 90f : 50f; // Stats base del auto
                
                float variability = (100f - p.stats.consistency) * 0.05f;
                float noise = UnityEngine.Random.Range(-variability, variability);
                
                // Tiempo base ajustado por habilidad y auto
                float factorTotal = (pilotFactor * 0.6f) + (carFactor * 0.4f) + noise;
                float performanceImpact = (factorTotal - 50f) * 0.02f; // -1s a +1s aprox
                
                results.Add(new QualyResult
                {
                    pilotId = p.id,
                    lapTimeSeconds = avgLapTime - performanceImpact,
                    position = 0 // Se rellena al ordenar
                });
            }

            // Ordenar por tiempo y asignar POS
            results = results.OrderBy(r => r.lapTimeSeconds).ToList();
            for (int i = 0; i < results.Count; i++) results[i].position = i + 1;

            Debug.Log($"[Qualifying] ¡POLE POSITION lograda por {results[0].pilotId}!");
            return results;
        }
    }
}
