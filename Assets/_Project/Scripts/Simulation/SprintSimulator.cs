// ============================================================
// F1 Career Manager — SprintSimulator.cs
// Gestión de fines de semana con formato SPRINT
// Incluye Sprint Qualifying y Sprint Race.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.Simulation
{
    public class SprintSimulator : MonoBehaviour
    {
        public static SprintSimulator Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
            Debug.Log("[SprintSimulator] ✅ Inicializado");
        }

        // ══════════════════════════════════════════════════════
        // SIMULACIÓN SPRINT (1/3 de distancia de carrera)
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Simula la carrera corta del sábado. Otorga puntos (8 al 1).
        /// </summary>
        public List<SprintResult> RunSprintRace(string circuitId, List<PilotData> pilots, int laps)
        {
            Debug.Log($"[Sprint] Iniciando carrera corta ({laps} vueltas) en {circuitId}...");
            var results = new List<SprintResult>();

            // Lógica similar a RaceSimulator pero sin paradas obligatorias
            foreach (var p in pilots)
            {
                float baseSpeed = p.stats.speed;
                float randomness = UnityEngine.Random.Range(-5f, 5f);
                float finalScore = baseSpeed + randomness;

                results.Add(new SprintResult
                {
                    pilotId = p.id,
                    score = finalScore,
                    pointsAwarded = 0
                });
            }

            // Ordenar y asignar puntos (1st=8, 2nd=7... 8th=1)
            results = results.OrderByDescending(r => r.score).ToList();
            for (int i = 0; i < results.Count; i++)
            {
                if (i < 8) results[i].pointsAwarded = 8 - i;
            }

            return results;
        }

        [Serializable]
        public class SprintResult
        {
            public string pilotId;
            public float score;
            public int pointsAwarded;
        }
    }
}
