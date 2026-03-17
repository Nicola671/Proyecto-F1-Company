// ============================================================
// F1 Career Manager — NegotiationSystem.cs
// Sistema de negociación de contratos — máx 3 rondas
// ============================================================
// DEPENDENCIAS: PilotData.cs, TeamData.cs, BudgetManager.cs
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Data;
using F1CareerManager.AI.EconomyAI;

namespace F1CareerManager.Market
{
    /// <summary>Estado de una negociación en curso</summary>
    public class NegotiationState
    {
        public string NegotiationId;
        public string PilotId;
        public string PilotName;
        public string BuyerTeamId;
        public string OfferedRole;         // "First" o "Second"
        public int OfferedContractYears;
        public int CurrentRound;           // 1-3
        public int MaxRounds;              // 3
        public bool IsComplete;
        public bool WasAccepted;
        public float PilotExpectation;     // Salario que espera
        public float FinalSalary;
        public float FinalReleaseClause;
        public float FinalVictoryBonus;
        public float FinalChampionBonus;
        public bool IsOutOfWindow;
        public string RejectionReason;
        public List<string> NegotiationLog;
    }

    /// <summary>Resultado de una ronda de negociación</summary>
    public class NegotiationResult
    {
        public int Round;
        public bool Accepted;
        public string PilotResponse;    // Texto de la respuesta
        public float CounterOffer;      // Contraoferta del piloto (0 si accepted)
        public string CounterDemand;    // Demanda adicional
        public bool FinalRejection;     // Si ya no quiere negociar más
    }

    /// <summary>
    /// Sistema de negociación con pilotos.
    /// El piloto evalúa la oferta según ego, lealtad, nivel del equipo,
    /// ofertas competidoras, y salario vs expectativa.
    /// Máximo 3 rondas de negociación.
    /// </summary>
    public class NegotiationSystem
    {
        // ── Datos ────────────────────────────────────────────
        private BudgetManager _budgetManager;
        private Random _rng;

        // ── Constantes ───────────────────────────────────────
        private const int MAX_ROUNDS = 3;
        private const float EGO_SALARY_MULTIPLIER = 0.005f;  // Ego×0.5% extra
        private const float ROLE_FIRST_EGO_THRESHOLD = 75;   // Ego > 75 exige #1

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public NegotiationSystem(BudgetManager budgetManager, Random rng = null)
        {
            _budgetManager = budgetManager;
            _rng = rng ?? new Random();
        }

        // ══════════════════════════════════════════════════════
        // INICIO DE NEGOCIACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Inicia una nueva negociación con un piloto
        /// </summary>
        public NegotiationState StartNegotiation(PilotData pilot,
            TeamData buyerTeam, string offeredRole, bool outOfWindow)
        {
            // Calcular expectativa salarial del piloto
            float expectation = CalculateSalaryExpectation(pilot, buyerTeam);

            // Penalización fuera de ventana
            if (outOfWindow)
                expectation *= 1.25f;

            // Si ego > 75 y le ofrecen ser #2, rechaza de entrada
            if (pilot.ego > ROLE_FIRST_EGO_THRESHOLD && offeredRole == "Second")
            {
                return new NegotiationState
                {
                    NegotiationId = $"neg_{_rng.Next(100000)}",
                    PilotId = pilot.id,
                    PilotName = $"{pilot.firstName} {pilot.lastName}",
                    BuyerTeamId = buyerTeam.id,
                    OfferedRole = offeredRole,
                    OfferedContractYears = 0,
                    CurrentRound = 0,
                    MaxRounds = MAX_ROUNDS,
                    IsComplete = true,
                    WasAccepted = false,
                    PilotExpectation = expectation,
                    FinalSalary = 0,
                    IsOutOfWindow = outOfWindow,
                    RejectionReason = $"{pilot.lastName} exige ser piloto #1",
                    NegotiationLog = new List<string>
                    {
                        $"{pilot.lastName}: \"No acepto ser segundo piloto. Merezco el #1.\""
                    }
                };
            }

            return new NegotiationState
            {
                NegotiationId = $"neg_{_rng.Next(100000)}",
                PilotId = pilot.id,
                PilotName = $"{pilot.firstName} {pilot.lastName}",
                BuyerTeamId = buyerTeam.id,
                OfferedRole = offeredRole,
                OfferedContractYears = 2,
                CurrentRound = 0,
                MaxRounds = MAX_ROUNDS,
                IsComplete = false,
                WasAccepted = false,
                PilotExpectation = expectation,
                FinalSalary = 0,
                IsOutOfWindow = outOfWindow,
                RejectionReason = "",
                NegotiationLog = new List<string>
                {
                    $"Negociación abierta con {pilot.firstName} {pilot.lastName}.",
                    $"Expectativa salarial estimada: ${expectation:F1}M/año."
                }
            };
        }

        // ══════════════════════════════════════════════════════
        // HACER OFERTA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Hace una oferta al piloto. Retorna su respuesta.
        /// </summary>
        public NegotiationResult MakeOffer(NegotiationState state,
            float salary, float releaseClause,
            float victoryBonus, float championBonus)
        {
            if (state.IsComplete) return null;

            state.CurrentRound++;
            var result = new NegotiationResult { Round = state.CurrentRound };

            // Verificar presupuesto
            // (no bloqueamos, pero advertimos)

            // Evaluar oferta
            float satisfactionScore = EvaluateOffer(state, salary,
                releaseClause, victoryBonus, championBonus);

            state.NegotiationLog.Add(
                $"Ronda {state.CurrentRound}: Oferta ${salary:F1}M/año, " +
                $"cláusula ${releaseClause:F1}M, bonus victoria ${victoryBonus:F1}M");

            if (satisfactionScore >= 0.80f)
            {
                // ¡Acepta!
                result.Accepted = true;
                result.PilotResponse = GenerateAcceptResponse(state);
                result.FinalRejection = false;

                state.IsComplete = true;
                state.WasAccepted = true;
                state.FinalSalary = salary;
                state.FinalReleaseClause = releaseClause;
                state.FinalVictoryBonus = victoryBonus;
                state.FinalChampionBonus = championBonus;
                state.NegotiationLog.Add("✅ ¡Oferta aceptada!");
            }
            else if (state.CurrentRound >= MAX_ROUNDS)
            {
                // Última ronda y no acepta → rechazo final
                result.Accepted = false;
                result.FinalRejection = true;
                result.PilotResponse = GenerateRejectResponse(state, satisfactionScore);

                state.IsComplete = true;
                state.WasAccepted = false;
                state.RejectionReason = "Negociación agotada (3 rondas)";
                state.NegotiationLog.Add("❌ Negociación fallida.");
            }
            else
            {
                // Contraoferta
                result.Accepted = false;
                result.FinalRejection = false;

                float counterSalary = state.PilotExpectation *
                    (0.9f + (float)_rng.NextDouble() * 0.2f);
                result.CounterOffer = counterSalary;

                // Demandas adicionales según personalidad
                result.CounterDemand = GenerateCounterDemand(state);
                result.PilotResponse = GenerateCounterResponse(state, counterSalary);

                state.NegotiationLog.Add(
                    $"Contraoferta: ${counterSalary:F1}M/año. {result.CounterDemand}");
            }

            return result;
        }

        // ══════════════════════════════════════════════════════
        // EVALUACIÓN DE OFERTA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Evalúa la satisfacción del piloto con la oferta (0-1).
        /// ≥0.80 = acepta.
        /// </summary>
        private float EvaluateOffer(NegotiationState state,
            float salary, float releaseClause,
            float victoryBonus, float championBonus)
        {
            float score = 0f;

            // Factor salario (40% del peso)
            float salaryRatio = salary / state.PilotExpectation;
            score += Math.Min(salaryRatio, 1.3f) * 0.40f;

            // Factor cláusula de salida (10%) — baja = mejor para piloto
            float clauseRatio = releaseClause > 0
                ? Math.Min(1f, state.PilotExpectation * 3f / releaseClause)
                : 1f;
            score += clauseRatio * 0.10f;

            // Factor bonuses (10%)
            float bonusScore = (victoryBonus + championBonus * 0.5f) * 0.5f;
            score += Math.Min(bonusScore / state.PilotExpectation, 1f) * 0.10f;

            // Factor ronda (10%) — más rondas = más presión para aceptar
            score += (state.CurrentRound / (float)MAX_ROUNDS) * 0.10f;

            // Factor equipo (15%) — se evalúa externamente como bonus
            // Equipos top son más atractivos
            score += 0.10f; // Base

            // Factor rol (15%) — rol #1 siempre es más atractivo
            if (state.OfferedRole == "First")
                score += 0.15f;
            else
                score += 0.05f;

            // Variación aleatoria (±5%)
            score += ((float)_rng.NextDouble() * 0.10f - 0.05f);

            return Math.Max(0f, Math.Min(1f, score));
        }

        /// <summary>
        /// Calcula cuánto espera ganar el piloto
        /// </summary>
        private float CalculateSalaryExpectation(PilotData pilot, TeamData team)
        {
            // Base: salario actual o valor de mercado
            float baseSalary = pilot.salary > 0
                ? pilot.salary : pilot.marketValue * 0.15f;

            // Ego aumenta expectativas
            float egoMultiplier = 1f + (pilot.ego * EGO_SALARY_MULTIPLIER);
            baseSalary *= egoMultiplier;

            // Si equipo comprador es mejor → pide más
            if (team.carPerformance > 85)
                baseSalary *= 1.15f;

            // Si quiere irse del equipo actual → acepta menos
            if (pilot.mood == "WantsOut")
                baseSalary *= 0.80f;
            else if (pilot.mood == "Furious")
                baseSalary *= 0.85f;

            // Lealtad alta al equipo actual → pide más para irse
            if (!string.IsNullOrEmpty(pilot.currentTeamId) &&
                pilot.currentTeamId != team.id)
            {
                float loyaltyPenalty = pilot.loyalty / 100f * 0.20f;
                baseSalary *= (1f + loyaltyPenalty);
            }

            return (float)Math.Round(baseSalary, 1);
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN DE TEXTO
        // ══════════════════════════════════════════════════════

        private string GenerateAcceptResponse(NegotiationState state)
        {
            string[] responses = {
                $"\"Estoy emocionado de unirme al proyecto. ¡Vamos a ganar juntos!\"",
                $"\"La oferta es justa. Acepto y estoy listo para demostrar de qué soy capaz.\"",
                $"\"Confío en la dirección del equipo. ¡Firmemos!\"",
                $"\"Es exactamente lo que buscaba. Manos a la obra.\"",
                $"\"Después de pensarlo, creo que es el movimiento correcto para mi carrera.\""
            };
            return responses[_rng.Next(responses.Length)];
        }

        private string GenerateRejectResponse(NegotiationState state, float score)
        {
            if (score < 0.40f)
                return "\"La oferta está muy lejos de lo que espero. No hay acuerdo.\"";
            if (score < 0.60f)
                return "\"Aprecio el interés, pero no logramos llegar a un número que me convenza.\"";
            return "\"Estuvimos cerca, pero al final no es suficiente. Quizás en el futuro.\"";
        }

        private string GenerateCounterResponse(NegotiationState state, float counter)
        {
            string[] templates = {
                $"\"Interesante, pero necesito al menos ${counter:F1}M para considerar.\"",
                $"\"Vamos bien, pero mi número es ${counter:F1}M. ¿Pueden llegar?\"",
                $"\"No está lejos, pero necesito ${counter:F1}M mínimo.\"",
                $"\"Mi representante dice que ${counter:F1}M es lo justo.\""
            };
            return templates[_rng.Next(templates.Length)];
        }

        private string GenerateCounterDemand(NegotiationState state)
        {
            string[] demands = {
                "Quiere cláusula de salida más baja.",
                "Pide bonus por cada victoria.",
                "Exige acceso prioritario al simulador.",
                "Quiere garantías sobre el desarrollo del auto.",
                "Pide que inviertan más en R&D esta temporada.",
                ""  // Sin demanda adicional
            };
            return demands[_rng.Next(demands.Length)];
        }
    }
}
