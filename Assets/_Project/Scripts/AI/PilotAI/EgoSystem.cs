// ============================================================
// F1 Career Manager — EgoSystem.cs
// Gestión del ego y exigencias salariales de los pilotos top
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using F1CareerManager.Data;
using F1CareerManager.Core;

namespace F1CareerManager.AI.PilotAI
{
    public class EgoSystem : MonoBehaviour
    {
        public static EgoSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Calcula el factor de ego de un piloto (0-100)
        /// </summary>
        public int CalculatePilotEgo(PilotData pilot)
        {
            // El ego sube con: Stars, Títulos, Edad/Experiencia y victorias recientes
            float ego = (pilot.stars * 15f) + (pilot.age * 0.5f);
            
            // Si es campeón mundial (Simulado por ahora con Stars)
            if (pilot.stars >= 5) ego += 20;

            return (int)Mathf.Clamp(ego, 0, 100);
        }

        /// <summary>
        /// Determina la exigencia salarial mínima del piloto basado en su ego
        /// </summary>
        public long GetMinSalaryExigency(PilotData pilot)
        {
            int ego = CalculatePilotEgo(pilot);
            long baseSal = (long)(ego * 500000); // Ego 100 = $50M anual base exigido

            return baseSal;
        }

        /// <summary>
        /// Determina si el piloto aceptará ser "Piloto 2"
        /// </summary>
        public bool AcceptSecondDriverRole(PilotData pilot)
        {
            int ego = CalculatePilotEgo(pilot);
            // Si el ego es mayor a 75, nunca aceptará ser el segundo piloto
            return ego < 75;
        }
    }
}
