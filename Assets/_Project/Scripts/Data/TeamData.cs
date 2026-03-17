// ============================================================
// F1 Career Manager — TeamData.cs
// Modelo de datos completo de un equipo
// ============================================================

using System;
using System.Collections.Generic;

namespace F1CareerManager.Data
{
    [Serializable]
    public class TeamData
    {
        // ── Identidad ────────────────────────────────────────
        public string id;                      // "ferrari", "redbull", etc.
        public string fullName;                // "Scuderia Ferrari"
        public string shortName;               // "Ferrari"
        public string country;                 // "Italy"
        public string countryCode;             // "IT"
        public string teamPrincipal;           // Nombre del jefe (jugador o IA)
        public string primaryColor;            // "#DC0000" (hex)
        public string secondaryColor;          // "#FFF200"
        public string spriteId;                // Para cargar el sprite correcto
        public int foundedYear;                // 1950
        public bool isPlayerControlled;        // true si es el equipo del jugador

        // ── Pilotos ──────────────────────────────────────────
        public string pilot1Id;                // ID del piloto #1
        public string pilot2Id;                // ID del piloto #2
        public string reservePilotId;          // ID del piloto de reserva (puede ser null)
        public List<string> juniorPilotIds;    // IDs de pilotos de academia

        // ── Stats del equipo (0-100) ─────────────────────────
        public int carPerformance;             // Rendimiento general del auto
        public int aeroRating;                 // Nivel aerodinámico
        public int engineRating;               // Nivel del motor
        public int chassisRating;              // Nivel del chasis
        public int reliabilityRating;          // Fiabilidad general
        public int pitStopSpeed;               // Velocidad de pit stops (0-100)

        // ── Staff ────────────────────────────────────────────
        public List<string> staffIds;          // IDs de los miembros del staff

        // ── Economía ─────────────────────────────────────────
        public float budget;                   // Presupuesto actual en millones
        public float baseBudget;               // Presupuesto base al inicio de temporada
        public float totalSalaries;            // Gasto total en salarios
        public float rndBudget;                // Presupuesto dedicado a R&D
        public float revenue;                  // Ingresos actuales de la temporada
        public float expenses;                 // Gastos actuales de la temporada
        public string financialStatus;         // "Thriving", "Healthy", etc.

        // ── Patrocinadores ───────────────────────────────────
        public List<string> sponsorIds;        // IDs de patrocinadores activos
        public float sponsorIncome;            // Ingresos totales por sponsors

        // ── Fábrica / Infraestructura ────────────────────────
        public int factoryLevel;               // 1-5 (afecta velocidad de R&D)
        public int windTunnelLevel;            // 1-5 (afecta calidad aero)
        public int simulatorLevel;             // 1-5 (afecta setup en carrera)

        // ── R&D ──────────────────────────────────────────────
        public List<string> installedComponentIds;     // Componentes actuales
        public List<string> inDevelopmentComponentIds;  // En desarrollo
        public float rndProgress;               // Progreso general de R&D (0-100)
        public float rndSpeedBonus;             // Bonus temporal a velocidad R&D (ej: Eureka)
        public float nextComponentBonus;        // Bonus al próximo componente instalado
        public int rndPausedWeeks;              // Semanas con R&D paralizado (ej: incendio)

        // ── Rendimiento detallado ─────────────────────────────
        public float aeroPerformance;           // Rendimiento aero actual (float detallado)
        public float engineReliability;         // Fiabilidad motor actual (float detallado)

        // ── Penalizaciones temporales ─────────────────────────
        public float pitStopPenalty;            // Segundos extra en pit stops (ej: huelga)
        public int pitStopPenaltyRaces;         // Carreras restantes con penalización

        // ── Reputación y clasificación ───────────────────────
        public int reputation;                 // 0-100 (afecta fichajes y sponsors)
        public int constructorPoints;          // Puntos en el campeonato actual
        public int constructorPosition;        // Posición en el campeonato actual

        // ── Historial ────────────────────────────────────────
        public int totalConstructorTitles;
        public int totalDriverTitles;          // Títulos de pilotos logrados por el equipo
        public int totalWins;
        public int totalPodiums;

        // ── Objetivos de la temporada ────────────────────────
        public string objectiveMinimum;        // "Top 3 constructores"
        public string objectiveStandard;       // "Top 2 constructores"
        public string objectiveElite;          // "Campeonato"
        public int objectiveMinTarget;         // Posición numérica mínima (ej: 3)
        public int objectiveStdTarget;         // Posición numérica estándar (ej: 2)
        public int objectiveEliteTarget;       // Posición numérica élite (ej: 1)

        /// <summary>
        /// Calcula el rendimiento general del auto basado en las áreas
        /// </summary>
        public int CalculateCarPerformance()
        {
            carPerformance = (int)(
                aeroRating * 0.30f +
                engineRating * 0.30f +
                chassisRating * 0.25f +
                reliabilityRating * 0.15f
            );
            return carPerformance;
        }

        /// <summary>
        /// Calcula el gasto total en salarios (pilotos + staff)
        /// </summary>
        public float CalculateTotalSalaries(List<PilotData> pilots, List<StaffData> staff)
        {
            totalSalaries = 0f;
            foreach (var pilot in pilots)
            {
                if (pilot.currentTeamId == id)
                    totalSalaries += pilot.salary;
            }
            foreach (var member in staff)
            {
                if (member.teamId == id)
                    totalSalaries += member.salary;
            }
            return totalSalaries;
        }

        /// <summary>
        /// Evalúa si el equipo está en crisis financiera
        /// </summary>
        public bool IsInCrisis()
        {
            return budget <= Core.Constants.CRISIS_THRESHOLD;
        }

        /// <summary>
        /// Actualiza el estado financiero
        /// </summary>
        public void UpdateFinancialStatus()
        {
            float ratio = budget / baseBudget;
            if (ratio > 0.8f) financialStatus = "Thriving";
            else if (ratio > 0.5f) financialStatus = "Healthy";
            else if (ratio > 0.25f) financialStatus = "Tight";
            else if (ratio > 0f) financialStatus = "Struggling";
            else financialStatus = "Crisis";
        }
    }
}
