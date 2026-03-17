// ============================================================
// F1 Career Manager — FormCalculator.cs
// Calculadora de estado de forma (Form) de pilotos y equipos
// Afecta directamente al rendimiento en simulación de carrera.
// ============================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using F1CareerManager.Data;
using F1CareerManager.Core;

namespace F1CareerManager.Core
{
    /// <summary>
    /// Utilidad estática para calcular la forma (0-100) basada en:
    /// - Últimos 5 resultados de carrera
    /// - Ánimo actual (Mood)
    /// - Fiabilidad de componentes
    /// </summary>
    public static class FormCalculator
    {
        private static Dictionary<string, List<int>> pilotHistory = new Dictionary<string, List<int>>();

        /// <summary>
        /// Agrega un nuevo resultado de carrera al historial del piloto (1-20)
        /// </summary>
        public static void RecordPilotResult(string pilotId, int position)
        {
            if (!pilotHistory.ContainsKey(pilotId)) pilotHistory[pilotId] = new List<int>();
            
            pilotHistory[pilotId].Insert(0, position);
            // Mantener solo los últimos 5 resultados
            if (pilotHistory[pilotId].Count > 5) pilotHistory[pilotId].RemoveAt(5);
        }

        /// <summary>
        /// Calcula la forma actual del piloto (0-100)
        /// </summary>
        public static int GetCurrentPilotForm(string pilotId, int moodValue)
        {
            float form = 50f; // Base Neutral

            // 1. Historial Reciente (60% peso)
            if (pilotHistory.ContainsKey(pilotId) && pilotHistory[pilotId].Count > 0)
            {
                float avgPos = (float)pilotHistory[pilotId].Average();
                // Si el promedio es 1 → +30 puntos. Si es 20 → -30 puntos.
                float resultsBonus = (10.5f - avgPos) * 3f;
                form += resultsBonus;
            }

            // 2. Mood (40% peso)
            // moodValue (0-100) → Bonus (-20 a +20)
            float moodBonus = (moodValue - 50f) * 0.4f;
            form += moodBonus;

            // 3. Fatiga o Lesiones (Afectan negativamente)
            // if (InjuryManager.Instance.HasActiveInjury(pilotId)) form -= 25;

            return (int)Mathf.Clamp(form, 0, 100);
        }

        /// <summary>
        /// Calcula la forma actual del equipo basada en fiabilidad
        /// </summary>
        public static int GetTeamReliabilityForm(TeamData team)
        {
            // La fiabilidad decae con el uso si no hay mantenimiento/R&D
            float reliability = team.stats.reliability;
            return (int)Mathf.Clamp(reliability, 0, 100);
        }

        public static void ResetManual()
        {
            pilotHistory.Clear();
        }
    }
}
