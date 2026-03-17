// ============================================================
// F1 Career Manager — ContractData.cs
// Modelo de datos de contrato
// ============================================================

using System;

namespace F1CareerManager.Data
{
    [Serializable]
    public class ContractData
    {
        public string id;                      // ID único
        public string pilotId;                 // ID del piloto
        public string teamId;                  // ID del equipo
        public string pilotRole;               // "First", "Second", "Reserve", "Junior"

        // ── Duración ─────────────────────────────────────────
        public int totalYears;                 // Duración total del contrato
        public int yearsRemaining;             // Años restantes
        public int seasonSigned;               // Temporada en que se firmó
        public int seasonExpires;              // Temporada en que expira

        // ── Financiero ───────────────────────────────────────
        public float annualSalary;             // Salario anual en millones
        public float releaseClause;            // Cláusula de salida en millones
        public float signingBonus;             // Bonus por firmar

        // ── Bonos por rendimiento ────────────────────────────
        public float winBonus;                 // Bonus por cada victoria
        public float podiumBonus;              // Bonus por cada podio
        public float championshipBonus;        // Bonus si gana el campeonato

        // ── Cláusulas especiales ─────────────────────────────
        public bool hasExtensionOption;        // El equipo puede renovar 1 año automáticamente
        public bool hasAntiRivalClause;        // Prohibición de ir a rivales directos
        public bool guaranteedFirstDriver;     // Si se prometió ser #1
        public string antiRivalTeamId;         // Equipo rival específico si aplica

        // ── Estado ───────────────────────────────────────────
        public bool isActive;                  // Si el contrato está activo
        public bool wasExtended;               // Si se usó la opción de extensión
        public bool wasBroken;                 // Si se rompió el contrato

        /// <summary>
        /// Calcula el costo total del contrato (sin bonos de rendimiento)
        /// </summary>
        public float GetTotalContractValue()
        {
            return (annualSalary * totalYears) + signingBonus;
        }

        /// <summary>
        /// Calcula el costo de romper el contrato
        /// </summary>
        public float GetBreakCost()
        {
            return releaseClause + (annualSalary * yearsRemaining * 0.5f);
        }
    }
}
