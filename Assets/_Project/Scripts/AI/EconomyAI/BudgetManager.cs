// ============================================================
// F1 Career Manager — BudgetManager.cs
// Gestión financiera de equipos — IA 5 (Economía)
// ============================================================
// DEPENDENCIAS: EventBus.cs, TeamData.cs, PilotData.cs,
//               StaffData.cs, Constants.cs
// EVENTOS QUE DISPARA: OnBudgetChanged
// EVENTOS QUE ESCUCHA: OnRaceFinished, OnContractSigned
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.AI.EconomyAI
{
    /// <summary>
    /// Gestiona el presupuesto de todos los equipos.
    /// Controla ingresos (premios, sponsors, merchandise) y gastos
    /// (salarios, logística, infraestructura, R&D).
    /// Detecta crisis financiera y aplica restricciones.
    /// </summary>
    public class BudgetManager
    {
        // ── Datos ────────────────────────────────────────────
        private EventBus _eventBus;
        private Random _rng;

        // ── Constantes de premios por carrera (en millones) ──
        private static readonly float[] RACE_PRIZE_BY_POSITION = new float[]
        {
            2.0f, 1.5f, 1.2f, 1.0f, 0.8f,     // P1-P5
            0.6f, 0.5f, 0.4f, 0.3f, 0.2f,      // P6-P10
            0.15f, 0.12f, 0.10f, 0.10f, 0.10f,  // P11-P15
            0.10f, 0.10f, 0.10f, 0.10f, 0.10f   // P16-P20
        };

        // ── Premios de constructor final (en millones) ───────
        private static readonly float[] CONSTRUCTOR_PRIZE = new float[]
        {
            150f, 120f, 100f, 80f, 60f,   // P1-P5
            45f,  35f,  25f, 15f, 10f     // P6-P10
        };

        // ── Costos fijos por nivel de equipo ─────────────────
        private const float LOGISTICS_BASE = 5f;            // $5M base
        private const float LOGISTICS_PER_FACTORY = 2f;     // +$2M por nivel fábrica
        private const float INFRASTRUCTURE_BASE = 2f;       // $2M base
        private const float INFRASTRUCTURE_PER_LEVEL = 1.5f; // +$1.5M por nivel
        private const float MERCHANDISE_MIN = 1f;           // $1M mínimo
        private const float MERCHANDISE_PER_REP = 0.09f;    // +$0.09M por reputación
        private const float BUDGET_CAP_PENALTY_RATE = 0.5f; // 50% del exceso como multa

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public BudgetManager(Random rng = null)
        {
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();

            // Suscribirse a eventos
            _eventBus.OnRaceFinished += HandleRaceFinished;
        }

        // ══════════════════════════════════════════════════════
        // TRANSACCIONES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Registra un ingreso para un equipo y dispara evento
        /// </summary>
        public void AddIncome(TeamData team, float amount, string reason)
        {
            float previousBudget = team.budget;
            team.budget += amount;
            team.revenue += amount;

            team.UpdateFinancialStatus();

            _eventBus.FireBudgetChanged(new EventBus.BudgetChangedArgs
            {
                TeamId = team.id,
                PreviousBudget = previousBudget,
                NewBudget = team.budget,
                ChangeAmount = amount,
                Reason = $"[INGRESO] {reason}",
                FinancialStatus = team.financialStatus
            });
        }

        /// <summary>
        /// Registra un gasto para un equipo y dispara evento
        /// </summary>
        public void AddExpense(TeamData team, float amount, string reason)
        {
            float previousBudget = team.budget;
            team.budget -= amount;
            team.expenses += amount;

            team.UpdateFinancialStatus();

            _eventBus.FireBudgetChanged(new EventBus.BudgetChangedArgs
            {
                TeamId = team.id,
                PreviousBudget = previousBudget,
                NewBudget = team.budget,
                ChangeAmount = -amount,
                Reason = $"[GASTO] {reason}",
                FinancialStatus = team.financialStatus
            });

            // Verificar crisis
            if (IsBelowCrisisThreshold(team))
            {
                ActivateCrisisMode(team);
            }
        }

        // ══════════════════════════════════════════════════════
        // PREMIOS POR CARRERA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Calcula y otorga premios de carrera a todos los equipos
        /// basado en las posiciones de la carrera
        /// </summary>
        public void AwardRacePrizes(List<TeamData> teams,
            List<EventBus.RacePositionInfo> positions)
        {
            // Agrupar premios por equipo (ambos pilotos suman)
            var teamPrizes = new Dictionary<string, float>();

            foreach (var pos in positions)
            {
                if (pos.DNF) continue;
                if (pos.Position < 1 || pos.Position > RACE_PRIZE_BY_POSITION.Length)
                    continue;

                float prize = RACE_PRIZE_BY_POSITION[pos.Position - 1];

                if (!teamPrizes.ContainsKey(pos.TeamId))
                    teamPrizes[pos.TeamId] = 0f;
                teamPrizes[pos.TeamId] += prize;
            }

            // Aplicar premios
            foreach (var kvp in teamPrizes)
            {
                var team = teams.Find(t => t.id == kvp.Key);
                if (team != null)
                {
                    AddIncome(team, kvp.Value,
                        $"Premio de carrera (${kvp.Value:F1}M)");
                }
            }
        }

        // ══════════════════════════════════════════════════════
        // PREMIOS DE FIN DE TEMPORADA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Otorga premios de constructor al final de la temporada
        /// </summary>
        public void AwardConstructorPrizes(List<TeamData> teams)
        {
            // Ordenar por puntos de constructor
            var sorted = new List<TeamData>(teams);
            sorted.Sort((a, b) => b.constructorPoints.CompareTo(a.constructorPoints));

            for (int i = 0; i < sorted.Count && i < CONSTRUCTOR_PRIZE.Length; i++)
            {
                float prize = CONSTRUCTOR_PRIZE[i];
                AddIncome(sorted[i], prize,
                    $"Premio campeonato constructores P{i + 1} (${prize:F0}M)");
            }
        }

        /// <summary>
        /// Calcula y aplica los ingresos por merchandise de cada equipo.
        /// Basado en reputación del equipo.
        /// </summary>
        public void CalculateMerchandiseIncome(List<TeamData> teams)
        {
            foreach (var team in teams)
            {
                float merchandise = MERCHANDISE_MIN +
                                   (team.reputation * MERCHANDISE_PER_REP);

                // Variación aleatoria ±15%
                float variance = 1f + ((float)_rng.NextDouble() * 0.30f - 0.15f);
                merchandise *= variance;

                AddIncome(team, merchandise,
                    $"Ingresos por merchandise (${merchandise:F1}M)");
            }
        }

        // ══════════════════════════════════════════════════════
        // GASTOS AUTOMÁTICOS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Procesa todos los gastos fijos de un equipo para la temporada.
        /// Llamar al inicio de cada temporada.
        /// </summary>
        public void ProcessSeasonExpenses(TeamData team,
            List<PilotData> pilots, List<StaffData> staff)
        {
            // 1. Salarios de pilotos y staff
            float totalSalaries = team.CalculateTotalSalaries(pilots, staff);
            if (totalSalaries > 0)
                AddExpense(team, totalSalaries, $"Salarios totales (${totalSalaries:F1}M)");

            // 2. Logística (viajes, transporte de equipo)
            float logistics = LOGISTICS_BASE +
                (team.factoryLevel * LOGISTICS_PER_FACTORY);
            AddExpense(team, logistics, $"Logística y transporte (${logistics:F1}M)");

            // 3. Infraestructura (mantenimiento fábrica, túnel, simulador)
            float totalLevel = team.factoryLevel + team.windTunnelLevel +
                              team.simulatorLevel;
            float infrastructure = INFRASTRUCTURE_BASE +
                (totalLevel * INFRASTRUCTURE_PER_LEVEL);
            AddExpense(team, infrastructure,
                $"Mantenimiento infraestructura (${infrastructure:F1}M)");
        }

        /// <summary>
        /// Procesa un gasto de R&D específico
        /// </summary>
        public void ProcessRnDExpense(TeamData team, float cost, string componentName)
        {
            AddExpense(team, cost, $"R&D: {componentName} (${cost:F1}M)");
        }

        // ══════════════════════════════════════════════════════
        // BUDGET CAP
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si un equipo excedió el budget cap y aplica multa FIA
        /// </summary>
        public float CheckBudgetCap(TeamData team)
        {
            float cap = Constants.BUDGET_CAP;

            // Los gastos no deben superar el cap
            if (team.expenses > cap)
            {
                float excess = team.expenses - cap;
                float penalty = excess * BUDGET_CAP_PENALTY_RATE;

                AddExpense(team, penalty,
                    $"Multa FIA por exceder budget cap por ${excess:F1}M");

                return penalty;
            }

            return 0f;
        }

        // ══════════════════════════════════════════════════════
        // CRISIS FINANCIERA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si el equipo está por debajo del umbral de crisis (20% del cap)
        /// </summary>
        public bool IsBelowCrisisThreshold(TeamData team)
        {
            return team.budget < (team.baseBudget * 0.20f);
        }

        /// <summary>
        /// Activa el modo crisis para un equipo.
        /// Restringe gastos y genera advertencias.
        /// </summary>
        private void ActivateCrisisMode(TeamData team)
        {
            team.financialStatus = "Crisis";

            // Bloquear gastos de R&D
            team.rndBudget = 0f;

            // Los pilotos se ponen nerviosos (se maneja via MoodSystem)
            _eventBus.FireBudgetChanged(new EventBus.BudgetChangedArgs
            {
                TeamId = team.id,
                PreviousBudget = team.budget,
                NewBudget = team.budget,
                ChangeAmount = 0f,
                Reason = "⚠️ CRISIS FINANCIERA ACTIVADA — R&D bloqueado, restricciones activas",
                FinancialStatus = "Crisis"
            });
        }

        /// <summary>
        /// Verifica si un equipo tiene presupuesto para un gasto
        /// </summary>
        public bool CanAfford(TeamData team, float amount)
        {
            return team.budget >= amount;
        }

        /// <summary>
        /// Obtiene el budget cap del equipo (basado en su tier)
        /// </summary>
        public float GetTeamBudgetCap(TeamData team)
        {
            // Los equipos con baseBudget alto tienen cap alto
            if (team.baseBudget >= 140f) return 145f;
            if (team.baseBudget >= 110f) return 130f;
            return 110f;
        }

        // ══════════════════════════════════════════════════════
        // RESUMEN FINANCIERO
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera un resumen financiero del equipo
        /// </summary>
        public string GetFinancialSummary(TeamData team)
        {
            float balance = team.revenue - team.expenses;
            string sign = balance >= 0 ? "+" : "";
            return $"{team.shortName}: Budget=${team.budget:F1}M | " +
                   $"Ingresos=${team.revenue:F1}M | " +
                   $"Gastos=${team.expenses:F1}M | " +
                   $"Balance={sign}{balance:F1}M | " +
                   $"Estado={team.financialStatus}";
        }

        /// <summary>
        /// Reset financiero para nueva temporada
        /// </summary>
        public void ResetSeasonFinances(TeamData team)
        {
            team.revenue = 0f;
            team.expenses = 0f;
            team.UpdateFinancialStatus();
        }

        // ══════════════════════════════════════════════════════
        // HANDLERS
        // ══════════════════════════════════════════════════════

        private void HandleRaceFinished(object sender, EventBus.RaceFinishedArgs args)
        {
            // Los premios de carrera se procesan externamente llamando a
            // AwardRacePrizes para tener acceso a la lista de equipos
        }

        /// <summary>Limpia suscripciones</summary>
        public void Dispose()
        {
            _eventBus.OnRaceFinished -= HandleRaceFinished;
        }
    }
}
