// ============================================================
// F1 Career Manager — ComponentEvaluator.cs
// Evaluación e instalación de componentes R&D — IA 2
// ============================================================
// DEPENDENCIAS: EventBus.cs, ComponentData.cs, TeamData.cs,
//               StaffData.cs, BudgetManager.cs, Constants.cs
// EVENTOS QUE DISPARA: OnComponentInstalled
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;
using F1CareerManager.AI.EconomyAI;

namespace F1CareerManager.AI.RnDAI
{
    /// <summary>
    /// Evalúa e instala componentes R&D en los autos.
    /// Simula resultado de instalación con probabilidades del GDD:
    /// Mejor 20%, Esperado 45%, Peor 25%, Falla 10%.
    /// Gestiona componentes ilegales y zona gris.
    /// </summary>
    public class ComponentEvaluator
    {
        // ── Datos ────────────────────────────────────────────
        private EventBus _eventBus;
        private Random _rng;

        // ── Bonus por legalidad ──────────────────────────────
        private const float ILLEGAL_PERF_MIN = 0.15f;     // +15% mínimo
        private const float ILLEGAL_PERF_MAX = 0.30f;     // +30% máximo
        private const float GREY_PERF_MIN = 0.08f;        // +8% mínimo
        private const float GREY_PERF_MAX = 0.15f;        // +15% máximo

        // ── Penalizaciones por fallo ─────────────────────────
        private const int FAIL_RELIABILITY_MIN = 10;       // -10 fiabilidad mínimo
        private const int FAIL_RELIABILITY_MAX = 25;       // -25 fiabilidad máximo

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public ComponentEvaluator(Random rng = null)
        {
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
        }

        // ══════════════════════════════════════════════════════
        // INSTALACIÓN DE COMPONENTES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Instala un componente en el auto de un equipo.
        /// Simula el resultado y aplica efectos al auto.
        /// </summary>
        /// <param name="component">Componente a instalar</param>
        /// <param name="team">Equipo donde se instala</param>
        /// <param name="staff">Lista de staff del equipo (para calcular bonus)</param>
        /// <returns>Resultado de la instalación</returns>
        public InstallationResult InstallComponent(ComponentData component,
            TeamData team, List<StaffData> staff)
        {
            // Calcular bonus del staff relevante
            float staffBonus = GetRelevantStaffBonus(component.area, staff, team.id);

            // Simular la instalación (método ya existente en ComponentData)
            string result = component.SimulateInstallation(staffBonus, _rng);

            // Aplicar bonus por legalidad si es ilegal/zona gris
            int legalityBonus = CalculateLegalityBonus(component);
            component.actualPerformance += legalityBonus;

            // Aplicar cambios al auto
            ApplyComponentToTeam(component, team, result);

            // Construir resultado detallado
            var installResult = new InstallationResult
            {
                ComponentId = component.id,
                ComponentName = component.name,
                Area = component.area,
                Result = result,
                ExpectedPerformance = component.expectedPerformance,
                ActualPerformance = component.actualPerformance,
                PerformanceDelta = component.actualPerformance - component.expectedPerformance,
                ReliabilityChange = result == "Failed"
                    ? -_rng.Next(FAIL_RELIABILITY_MIN, FAIL_RELIABILITY_MAX + 1)
                    : 0,
                Legality = component.legality,
                LegalityBonusApplied = legalityBonus
            };

            // Si hubo fallo, reducir fiabilidad del área
            if (result == "Failed")
            {
                ApplyReliabilityPenalty(team, component.area,
                    Math.Abs(installResult.ReliabilityChange));
            }

            // Disparar evento
            _eventBus.FireComponentInstalled(new EventBus.ComponentInstalledArgs
            {
                ComponentId = component.id,
                ComponentName = component.name,
                TeamId = team.id,
                Area = component.area,
                InstallResult = result,
                ExpectedPerformance = component.expectedPerformance,
                ActualPerformance = component.actualPerformance,
                Legality = component.legality
            });

            return installResult;
        }

        // ══════════════════════════════════════════════════════
        // APLICACIÓN AL AUTO
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Aplica los cambios del componente a los stats del auto
        /// </summary>
        private void ApplyComponentToTeam(ComponentData component,
            TeamData team, string result)
        {
            // Calcular ganancia real (puede ser negativa si salió mal)
            int gain = component.actualPerformance - component.expectedPerformance
                       + component.performanceGain;

            // Normalizar: una ganancia de componente de ~10 = +1 punto en el stat
            float statChange = gain * 0.1f;

            switch (component.area)
            {
                case "Aerodynamics":
                    team.aeroRating = Clamp(team.aeroRating + (int)Math.Round(statChange),
                        0, 100);
                    break;
                case "Engine":
                    team.engineRating = Clamp(team.engineRating + (int)Math.Round(statChange),
                        0, 100);
                    break;
                case "Chassis":
                    team.chassisRating = Clamp(team.chassisRating + (int)Math.Round(statChange),
                        0, 100);
                    break;
                case "Reliability":
                    team.reliabilityRating = Clamp(
                        team.reliabilityRating + (int)Math.Round(statChange), 0, 100);
                    break;
            }

            // Recalcular rendimiento general
            team.CalculateCarPerformance();
        }

        /// <summary>
        /// Reduce la fiabilidad de un área específica por fallo
        /// </summary>
        private void ApplyReliabilityPenalty(TeamData team, string area, int amount)
        {
            // Un fallo en cualquier área afecta la fiabilidad general
            team.reliabilityRating = Clamp(team.reliabilityRating - amount, 0, 100);
            team.CalculateCarPerformance();
        }

        // ══════════════════════════════════════════════════════
        // LEGALIDAD
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Calcula el bonus de rendimiento por usar componentes ilegales/zona gris
        /// </summary>
        private int CalculateLegalityBonus(ComponentData component)
        {
            switch (component.legality)
            {
                case "Illegal":
                    float illegalBonus = ILLEGAL_PERF_MIN +
                        (float)_rng.NextDouble() * (ILLEGAL_PERF_MAX - ILLEGAL_PERF_MIN);
                    return (int)(component.expectedPerformance * illegalBonus);

                case "GreyAggressive":
                    float greyAggBonus = GREY_PERF_MIN +
                        (float)_rng.NextDouble() * (GREY_PERF_MAX - GREY_PERF_MIN);
                    return (int)(component.expectedPerformance * greyAggBonus);

                case "GreySubtle":
                    float greySubBonus = GREY_PERF_MIN * 0.8f +
                        (float)_rng.NextDouble() * (GREY_PERF_MAX * 0.6f);
                    return (int)(component.expectedPerformance * greySubBonus);

                default:
                    return 0; // Legal, sin bonus extra
            }
        }

        // ══════════════════════════════════════════════════════
        // VENTA DE COMPONENTES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Vende un componente a un equipo rival.
        /// Genera ingreso para el vendedor pero fortalece al comprador.
        /// </summary>
        public float SellComponentToRival(ComponentData component,
            TeamData seller, TeamData buyer,
            BudgetManager budgetManager)
        {
            float price = component.marketPrice;

            // El comprador paga
            budgetManager.AddExpense(buyer, price,
                $"Compra componente {component.name} de {seller.shortName}");

            // El vendedor recibe
            budgetManager.AddIncome(seller, price,
                $"Venta componente {component.name} a {buyer.shortName}");

            // El componente pasa al comprador (versión ligeramente inferior)
            component.ownerTeamId = buyer.id;
            component.actualPerformance = (int)(component.actualPerformance * 0.85f);

            return price;
        }

        // ══════════════════════════════════════════════════════
        // STAFF BONUS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene el bonus del staff relevante para un área de componente
        /// </summary>
        private float GetRelevantStaffBonus(string area, List<StaffData> staff,
            string teamId)
        {
            float totalBonus = 0f;

            foreach (var member in staff)
            {
                if (member.teamId != teamId || member.isBurnedOut) continue;

                // Director Técnico siempre aporta
                if (member.role == "TechnicalDirector")
                    totalBonus += member.GetRoleBonus() * 0.5f;

                // Staff específico del área
                bool isRelevant = false;
                switch (area)
                {
                    case "Aerodynamics":
                        isRelevant = member.role == "AeroChief";
                        break;
                    case "Engine":
                        isRelevant = member.role == "EngineChief";
                        break;
                    case "Chassis":
                    case "Reliability":
                        isRelevant = member.role == "RaceEngineer";
                        break;
                }

                if (isRelevant)
                    totalBonus += member.GetRoleBonus();
            }

            return totalBonus;
        }

        // ══════════════════════════════════════════════════════
        // EVALUACIÓN DE COMPONENTE
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Evalúa si vale la pena instalar un componente.
        /// Usado por la IA de equipos rivales para decidir.
        /// </summary>
        public float EvaluateComponentValue(ComponentData component, TeamData team)
        {
            float value = component.expectedPerformance * 0.5f;
            value += component.performanceGain * 2f;

            // Penalizar riesgo de componentes ilegales
            float detectionRisk = component.GetDetectionChance();
            value *= (1f - detectionRisk * 0.5f);

            // Bonus si el equipo es débil en esa área
            int currentLevel = GetTeamStatForArea(team, component.area);
            if (currentLevel < 75)
                value *= 1.3f; // 30% más valioso si el área es débil

            return value;
        }

        /// <summary>
        /// Obtiene el stat del equipo para un área específica
        /// </summary>
        private int GetTeamStatForArea(TeamData team, string area)
        {
            switch (area)
            {
                case "Aerodynamics": return team.aeroRating;
                case "Engine": return team.engineRating;
                case "Chassis": return team.chassisRating;
                case "Reliability": return team.reliabilityRating;
                default: return 70;
            }
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    /// <summary>Resultado detallado de una instalación</summary>
    public class InstallationResult
    {
        public string ComponentId;
        public string ComponentName;
        public string Area;
        public string Result;            // "BetterThanExpected", etc.
        public int ExpectedPerformance;
        public int ActualPerformance;
        public int PerformanceDelta;
        public int ReliabilityChange;
        public string Legality;
        public int LegalityBonusApplied;
    }
}
