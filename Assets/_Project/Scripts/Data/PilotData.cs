// ============================================================
// F1 Career Manager — PilotData.cs
// Modelo de datos completo de un piloto
// ============================================================

using System;

namespace F1CareerManager.Data
{
    [Serializable]
    public class PilotData
    {
        // ── Identidad ────────────────────────────────────────
        public string id;                  // ID único (ej: "VER_01")
        public string firstName;           // "Max"
        public string lastName;            // "Verstappen"
        public string nationality;         // "Dutch"
        public string countryCode;         // "NL"
        public string shortName;           // "VER" (3 letras)
        public int number;                 // 1
        public int age;                    // 27
        public string spriteId;            // ID para cargar el sprite
        public bool isRegen;               // false para reales, true para generados

        // ── Estrellas y rating visible ───────────────────────
        public int stars;                  // 1-5 ⭐
        public int overallRating;          // 0-100 (promedio ponderado de stats)

        // ── Stats principales (0-100) ────────────────────────
        public int speed;                  // Velocidad pura
        public int consistency;            // Consistencia
        public int rainSkill;              // Habilidad bajo lluvia
        public int startSkill;             // Desempeño en salidas
        public int defense;                // Capacidad defensiva
        public int attack;                 // Capacidad de adelantamiento
        public int tireManagement;         // Gestión de neumáticos
        public int fuelManagement;         // Gestión de combustible
        public int concentration;          // Concentración / menos errores
        public int adaptability;           // Adaptación a nuevo auto

        // ── Atributos ocultos (NO visibles al jugador) ───────
        public int potential;              // Techo máximo (0-100)
        public float growthRate;           // Velocidad de crecimiento (0.5 - 2.0)
        public int peakAge;                // Edad pico (25-33)
        public float declineRate;          // Velocidad de declive post-pico (0.5 - 2.0)
        public string potentialLabel;      // "Excepcional", "Prometedor", etc.

        // ── Estado emocional ─────────────────────────────────
        public string mood;                // "Happy", "Neutral", "Upset", "Furious", "WantsOut"
        public int moodValue;              // -100 a +100 (interno, determina el mood)
        public int ego;                    // 0-100
        public int loyalty;               // 0-100
        public int aggression;             // 0-100 (nivel de agresividad)
        public int formCurrent;            // Forma actual (fluctúa, 60-100)
        public int teammateRelation;       // -100 a +100
        public int pressRelation;          // 0-100 (relación con la prensa)

        // ── Contrato actual ──────────────────────────────────
        public string currentTeamId;       // ID del equipo actual
        public int contractYearsLeft;      // Años restantes
        public float salary;               // Salario anual en millones
        public float releaseClause;        // Cláusula de salida en millones
        public string role;                // "First", "Second", "Reserve", "Junior"

        // ── Historial ────────────────────────────────────────
        public int totalRaces;
        public int totalWins;
        public int totalPodiums;
        public int totalPoles;
        public int totalPoints;
        public int totalChampionships;
        public int seasonsInF1;
        public int bestFinish;             // Mejor posición lograda en una carrera

        // ── Categoría (para regens) ─────────────────────────
        public string currentCategory;     // "F1", "F2", "F3"

        // ── Estado físico ────────────────────────────────────
        public bool isInjured;
        public int racesUntilRecovery;     // 0 si no está lesionado
        public string injurySeverity;      // "Light", "Moderate", "Severe"

        // ── Mercado ──────────────────────────────────────────
        public float marketValue;          // Valor de mercado estimado en millones
        public bool isAvailable;           // Si está disponible para transferencia
        public bool isRetired;             // Si se retiró

        /// <summary>
        /// Calcula el overall rating basado en los stats principales
        /// </summary>
        public int CalculateOverall()
        {
            // Ponderación: velocidad y consistencia pesan más
            float weighted =
                speed * 0.18f +
                consistency * 0.14f +
                rainSkill * 0.08f +
                startSkill * 0.08f +
                defense * 0.10f +
                attack * 0.10f +
                tireManagement * 0.10f +
                fuelManagement * 0.06f +
                concentration * 0.08f +
                adaptability * 0.08f;

            overallRating = (int)Math.Round(weighted);
            return overallRating;
        }

        /// <summary>
        /// Calcula las estrellas según el overall rating
        /// </summary>
        public int CalculateStars()
        {
            if (overallRating >= 90) stars = 5;
            else if (overallRating >= 78) stars = 4;
            else if (overallRating >= 65) stars = 3;
            else if (overallRating >= 50) stars = 2;
            else stars = 1;

            return stars;
        }

        /// <summary>
        /// Obtiene el modificador de rendimiento según el humor
        /// </summary>
        public float GetMoodModifier()
        {
            switch (mood)
            {
                case "Happy": return Core.Constants.MOOD_HAPPY_BONUS;
                case "Upset": return Core.Constants.MOOD_UPSET_PENALTY;
                case "Furious": return Core.Constants.MOOD_FURIOUS_PENALTY;
                case "WantsOut": return Core.Constants.MOOD_LEAVING_PENALTY;
                default: return 0f; // Neutral
            }
        }

        /// <summary>
        /// Determina el label de potencial visible al jugador
        /// </summary>
        public string GetPotentialLabel()
        {
            if (potential >= 90) return "Talento generacional";
            if (potential >= 80) return "Excepcional";
            if (potential >= 70) return "Muy prometedor";
            if (potential >= 55) return "Prometedor";
            if (potential >= 40) return "Decente";
            return "Inconsistente";
        }
    }
}
