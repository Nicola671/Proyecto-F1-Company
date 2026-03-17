// ============================================================
// F1 Career Manager — CircuitData.cs
// Modelo de datos de un circuito
// ============================================================

using System;
using System.Collections.Generic;

namespace F1CareerManager.Data
{
    [Serializable]
    public class CircuitData
    {
        // ── Identidad ────────────────────────────────────────
        public string id;                      // "monza", "monaco", etc.
        public string name;                    // "Autodromo Nazionale Monza"
        public string shortName;               // "Monza"
        public string city;                    // "Monza"
        public string country;                 // "Italy"
        public string countryCode;             // "IT"
        public int roundNumber;                // Número de ronda en el calendario (1-24)
        public string spriteId;                // Para cargar sprite del circuito

        // ── Características técnicas ─────────────────────────
        public int totalLaps;                  // Número de vueltas de la carrera
        public float lapDistanceKm;            // Distancia de una vuelta en km
        public string circuitType;             // "HighSpeed", "Street", "Technical", etc.
        public int drsZones;                   // Número de zonas DRS
        public bool isNightRace;               // Si se corre de noche

        // ── Qué favorece / perjudica ─────────────────────────
        public List<string> favors;            // ["potencia_motor", "velocidad_punta"]
        public List<string> hinders;           // ["carga_aerodinamica"]

        // ── Probabilidades de eventos ────────────────────────
        public float rainChance;               // 0.0 - 1.0
        public float safetyCarChance;          // 0.0 - 1.0
        public string tireDegradation;         // "VeryLow" a "VeryHigh"

        // ── Dificultad ───────────────────────────────────────
        public int overtakingDifficulty;       // 0-100 (0=fácil, 100=imposible)
        public int setupDifficulty;            // 0-100 (qué tan difícil es clavar el setup)
        public string specialCharacteristic;   // "Muro de los Campeones", etc.

        // ── Clima base ───────────────────────────────────────
        public float baseTemperature;          // Temperatura promedio en °C
        public float humidityFactor;           // 0.0 - 1.0 (afecta neumáticos)

        /// <summary>
        /// Calcula el modificador de rendimiento del auto según el tipo de circuito
        /// y las fortalezas del auto
        /// </summary>
        public float GetCarModifier(TeamData team)
        {
            float modifier = 0f;

            foreach (var favor in favors)
            {
                switch (favor)
                {
                    case "potencia_motor":
                    case "velocidad_punta":
                        modifier += team.engineRating * 0.01f;
                        break;
                    case "carga_aerodinamica":
                    case "downforce":
                        modifier += team.aeroRating * 0.01f;
                        break;
                    case "traccion":
                    case "estabilidad":
                        modifier += team.chassisRating * 0.01f;
                        break;
                    case "fiabilidad":
                        modifier += team.reliabilityRating * 0.01f;
                        break;
                }
            }

            foreach (var hinder in hinders)
            {
                switch (hinder)
                {
                    case "potencia_motor":
                    case "velocidad_punta":
                        modifier -= team.engineRating * 0.005f;
                        break;
                    case "carga_aerodinamica":
                    case "downforce":
                        modifier -= team.aeroRating * 0.005f;
                        break;
                }
            }

            return modifier;
        }
    }
}
