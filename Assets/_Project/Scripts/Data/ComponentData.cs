// ============================================================
// F1 Career Manager — ComponentData.cs
// Modelo de datos de un componente R&D
// ============================================================

using System;

namespace F1CareerManager.Data
{
    [Serializable]
    public class ComponentData
    {
        // ── Identidad ────────────────────────────────────────
        public string id;                      // ID único
        public string name;                    // "Alerón delantero V2"
        public string description;             // Descripción del componente
        public string area;                    // "Aerodynamics", "Engine", etc.
        public string specificPart;            // "FrontWing", "ICE", "Brakes", etc.

        // ── Rendimiento ──────────────────────────────────────
        public int expectedPerformance;        // Lo que el equipo CREE que dará (0-100)
        public int actualPerformance;          // Lo que REALMENTE da (calculado por IA)
        public int performanceGain;            // Diferencia vs componente anterior
        public int reliability;                // 0-100 (bajo = riesgo de DNF)

        // ── Desarrollo ───────────────────────────────────────
        public float developmentCost;          // Costo en millones
        public int developmentWeeks;           // Semanas para desarrollar
        public int developmentProgress;        // 0-100% progreso actual
        public string status;                  // "InDevelopment", "Available", "Installed", "Banned"

        // ── Legalidad ────────────────────────────────────────
        public string legality;                // "Legal", "GreySubtle", "GreyAggressive", "Illegal"
        public bool hasBeenInvestigated;       // Si la FIA ya lo investigó
        public bool isBanned;                  // Si fue prohibido

        // ── Resultado de instalación ─────────────────────────
        public string installResult;           // "BetterThanExpected", "AsExpected", etc.
        public bool hasBeenInstalled;          // Si ya se instaló

        // ── Mercado ──────────────────────────────────────────
        public float marketPrice;              // Precio de venta/compra en millones
        public bool isForSale;                 // Si está a la venta
        public string ownerTeamId;             // Equipo que lo desarrolló

        /// <summary>
        /// Calcula el resultado real al instalar (basado en probabilidades)
        /// staffBonus: bonus del Director Técnico / Jefe del área
        /// </summary>
        public string SimulateInstallation(float staffBonus, System.Random rng)
        {
            // Probabilidades base del GDD, modificadas por staff
            float betterChance = Core.Constants.RND_RESULT_BETTER + staffBonus;
            float expectedChance = Core.Constants.RND_RESULT_EXPECTED;
            float worseChance = Core.Constants.RND_RESULT_WORSE - (staffBonus * 0.5f);
            float failChance = Core.Constants.RND_RESULT_FAIL - (staffBonus * 0.3f);

            // Normalizar para que sumen 1.0
            float total = betterChance + expectedChance + worseChance + failChance;
            betterChance /= total;
            expectedChance /= total;
            worseChance /= total;
            failChance /= total;

            float roll = (float)rng.NextDouble();

            if (roll < betterChance)
            {
                actualPerformance = expectedPerformance + rng.Next(5, 15);
                installResult = "BetterThanExpected";
            }
            else if (roll < betterChance + expectedChance)
            {
                actualPerformance = expectedPerformance + rng.Next(-2, 3);
                installResult = "AsExpected";
            }
            else if (roll < betterChance + expectedChance + worseChance)
            {
                actualPerformance = expectedPerformance - rng.Next(5, 15);
                installResult = "WorseThanExpected";
            }
            else
            {
                actualPerformance = expectedPerformance - rng.Next(15, 30);
                reliability -= rng.Next(10, 25);
                installResult = "Failed";
            }

            // Asegurar que no baje de 0
            if (actualPerformance < 0) actualPerformance = 0;
            if (reliability < 0) reliability = 0;

            hasBeenInstalled = true;
            status = "Installed";

            return installResult;
        }

        /// <summary>
        /// Calcula la probabilidad de detección por la FIA
        /// </summary>
        public float GetDetectionChance()
        {
            switch (legality)
            {
                case "Illegal": return Core.Constants.FIA_DETECT_ILLEGAL;
                case "GreyAggressive": return Core.Constants.FIA_DETECT_GREY_AGGRESSIVE;
                case "GreySubtle": return Core.Constants.FIA_DETECT_GREY_SUBTLE;
                default: return 0f; // Legal, no se detecta
            }
        }
    }
}
