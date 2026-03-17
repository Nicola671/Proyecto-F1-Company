// ============================================================
// F1 Career Manager — StaffData.cs
// Modelo de datos de un miembro del staff
// ============================================================

using System;

namespace F1CareerManager.Data
{
    [Serializable]
    public class StaffData
    {
        // ── Identidad ────────────────────────────────────────
        public string id;                      // ID único
        public string firstName;               // Nombre
        public string lastName;                // Apellido
        public string nationality;             // Nacionalidad
        public string countryCode;             // Código de país
        public int age;                        // Edad actual

        // ── Rol y capacidad ──────────────────────────────────
        public string role;                    // "TechnicalDirector", "AeroChief", etc.
        public int stars;                      // 1-5 ⭐ nivel técnico
        public int skillLevel;                 // 0-100 nivel preciso
        public string secondarySpecialty;      // Área secundaria donde aporta

        // ── Personalidad ─────────────────────────────────────
        public int loyalty;                    // 0-100
        public int ego;                        // 0-100
        public int motivation;                 // 0-100 (baja con burnout)
        public int pilotRelation;              // -100 a +100 (relación con pilotos)

        // ── Contrato ─────────────────────────────────────────
        public string teamId;                  // ID del equipo actual
        public int contractYearsLeft;          // Años restantes
        public float salary;                   // Salario anual en millones
        public float releaseClause;            // Cláusula de salida

        // ── Estado ───────────────────────────────────────────
        public bool isBurnedOut;               // Si tiene burnout activo
        public int burnoutRecoveryWeeks;       // Semanas hasta recuperarse
        public bool isAvailable;               // Si está disponible para fichaje
        public bool isRetired;                 // Si se retiró

        // ── Efecto en el equipo ──────────────────────────────
        // Estos se calculan según el rol y el nivel

        /// <summary>
        /// Obtiene el bonus que da al equipo según su rol y nivel
        /// </summary>
        public float GetRoleBonus()
        {
            float baseBonus = skillLevel / 100f;

            switch (role)
            {
                case "TechnicalDirector":
                    return baseBonus * 0.15f;        // +hasta 15% velocidad R&D
                case "AeroChief":
                    return baseBonus * 0.12f;        // +hasta 12% rendimiento aero
                case "EngineChief":
                    return baseBonus * 0.10f;        // +hasta 10% fiabilidad motor
                case "RaceEngineer":
                    return baseBonus * 0.10f;        // +hasta 10% estrategia
                case "DataAnalyst":
                    return baseBonus * 0.08f;        // +hasta 8% lectura de setup
                case "TeamDoctor":
                    return baseBonus * 0.35f;        // +hasta 35% velocidad recuperación
                case "CommsChief":
                    return baseBonus * 0.30f;        // -hasta 30% rumores negativos
                case "AcademyDirector":
                    return baseBonus * 0.20f;        // +hasta 20% desarrollo juniors
                case "FinanceDirector":
                    return baseBonus * 0.05f;        // +hasta 5% ingresos sponsors
                case "Spy":
                    return baseBonus * 0.25f;        // +hasta 25% info rival
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Calcula las estrellas según el nivel de habilidad
        /// </summary>
        public int CalculateStars()
        {
            if (skillLevel >= 90) stars = 5;
            else if (skillLevel >= 75) stars = 4;
            else if (skillLevel >= 55) stars = 3;
            else if (skillLevel >= 35) stars = 2;
            else stars = 1;

            return stars;
        }
    }
}
