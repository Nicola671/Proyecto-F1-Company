// ============================================================
// F1 Career Manager — LapSimulator.cs
// Cálculo detallado de tiempos de vuelta (micro-simulación)
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using F1CareerManager.Data;
using F1CareerManager.Core;
using F1CareerManager.Utils;

namespace F1CareerManager.Simulation
{
    public class LapSimulator : MonoBehaviour
    {
        public static LapSimulator Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Calcula el tiempo de vuelta exacto para un piloto basado en múltiples factores
        /// </summary>
        public float CalculateLapTime(PilotData pilot, TeamData team, CircuitData circuit, float setupBonus, float wearFactor)
        {
            // Tiempo Base del Circuito (Simulado: 1:30.000 = 90.0s)
            float baseTime = 90.0f;
            
            // Factor Piloto (Velocidad, Consistencia, Ánimo)
            float pilotSkill = pilot.stats.speed + (pilot.stats.consistency * 0.2f);
            float pilotMood = pilot.mood * 0.1f;
            float pilotTotal = pilotSkill + pilotMood;

            // Factor Coche (Aero, Motor, Chasis)
            float carPower = team.stats.aero + team.stats.engine + team.stats.chassis;
            
            // Factor Circuito (Favorece/Penaliza)
            float circuitBonus = 0f;
            foreach (var fav in circuit.favors)
            {
                if (fav == "engine_power") circuitBonus += team.stats.engine * 0.05f;
                if (fav == "tire_management") circuitBonus += pilot.stats.consistency * 0.05f;
            }

            // Cálculo Final de Tiempo (Segundos)
            float performanceIndex = (pilotTotal * 0.4f) + (carPower * 0.4f) + circuitBonus;
            float timeReduction = performanceIndex / 100f; // Aprox 1.5s a 3.0s de diferencia

            float lapTime = baseTime - timeReduction - setupBonus;
            
            // Impacto del Desgaste (Aumento de tiempo por vuelta)
            lapTime += wearFactor * 0.1f;

            // Variabilidad aleatoria (Tráfico, Errores pequeños)
            float error = MathUtils.GaussianRandom(0.05f, 0.02f);
            
            return lapTime + error;
        }
    }
}
