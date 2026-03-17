// ============================================================
// F1 Career Manager — SanctionSystem.cs
// Sistema de sanciones de la FIA
// ============================================================
// DEPENDENCIAS: EventBus.cs, TeamData.cs, RegulationChecker.cs,
//               BudgetManager.cs, Constants.cs
// EVENTOS QUE DISPARA: OnFIAInvestigation (sanción aplicada)
// EVENTOS QUE ESCUCHA: OnFIAInvestigation (detección)
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;
using F1CareerManager.AI.EconomyAI;

namespace F1CareerManager.AI.FIAAI
{
    /// <summary>
    /// Registro de una sanción aplicada
    /// </summary>
    public class SanctionRecord
    {
        public string SanctionId;
        public string TeamId;
        public string Type;            // "Warning", "Fine", "ComponentBan", etc.
        public string Severity;        // "Light", "Moderate", "Severe", "Extreme"
        public float FineAmount;       // Si es multa
        public int PointsPenalty;      // Puntos descontados
        public string Description;
        public int SeasonApplied;
        public bool WasAppealed;
        public bool AppealSuccessful;
        public int AppealWeeksRemaining;
    }

    /// <summary>
    /// Aplica sanciones basadas en detecciones de RegulationChecker.
    /// Severidad escala según historial. Incluye sistema de apelación.
    /// </summary>
    public class SanctionSystem
    {
        // ── Datos ────────────────────────────────────────────
        private EventBus _eventBus;
        private BudgetManager _budgetManager;
        private RegulationChecker _regulationChecker;
        private Random _rng;
        private List<SanctionRecord> _sanctionHistory;
        private List<SanctionRecord> _pendingAppeals;

        // ── Constantes ───────────────────────────────────────
        private const float FINE_MIN = 0.5f;                // $500K
        private const float FINE_MAX = 10f;                  // $10M
        private const int POINTS_PENALTY_MIN = 5;
        private const int POINTS_PENALTY_MAX = 50;
        private const float APPEAL_SUCCESS_CHANCE = 0.30f;   // 30% éxito
        private const int APPEAL_DURATION_WEEKS = 2;
        private const float SEASON_DISQUALIFICATION_CHANCE = 0.02f; // <2%

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public SanctionSystem(BudgetManager budgetManager,
            RegulationChecker regulationChecker, Random rng = null)
        {
            _budgetManager = budgetManager;
            _regulationChecker = regulationChecker;
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
            _sanctionHistory = new List<SanctionRecord>();
            _pendingAppeals = new List<SanctionRecord>();

            // Escuchar detecciones
            _eventBus.OnFIAInvestigation += HandleInvestigationResolved;
        }

        // ══════════════════════════════════════════════════════
        // DETERMINACIÓN DE SANCIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Determina y aplica la sanción apropiada para una infracción detectada.
        /// La severidad escala según el historial del equipo.
        /// </summary>
        public SanctionRecord DetermineSanction(string teamId,
            string componentLegality, string reason, TeamData team, int currentSeason)
        {
            // Obtener historial de infracciones
            int priorInfractions = _regulationChecker.GetInfractionCount(teamId);

            // Determinar severidad base según legalidad
            string severity = DetermineSeverity(componentLegality, priorInfractions);

            // Crear la sanción según severidad
            var sanction = new SanctionRecord
            {
                SanctionId = $"san_{_rng.Next(100000)}",
                TeamId = teamId,
                Severity = severity,
                Description = reason,
                SeasonApplied = currentSeason,
                WasAppealed = false,
                AppealSuccessful = false,
                AppealWeeksRemaining = 0
            };

            // Aplicar sanción según severidad
            switch (severity)
            {
                case "Light":
                    sanction.Type = "Warning";
                    sanction.FineAmount = 0f;
                    sanction.PointsPenalty = 0;
                    sanction.Description = $"Advertencia oficial: {reason}";
                    break;

                case "Moderate":
                    // Multa o prohibición de componente
                    if ((float)_rng.NextDouble() < 0.6f)
                    {
                        sanction.Type = "Fine";
                        sanction.FineAmount = FINE_MIN +
                            (float)_rng.NextDouble() * (FINE_MAX * 0.4f - FINE_MIN);
                        sanction.Description = $"Multa de ${sanction.FineAmount:F1}M: {reason}";
                    }
                    else
                    {
                        sanction.Type = "ComponentBan";
                        sanction.Description = $"Componente prohibido: {reason}";
                    }
                    break;

                case "Severe":
                    // Multa grande + penalización de puntos
                    sanction.Type = "PointsPenalty";
                    sanction.FineAmount = FINE_MAX * 0.4f +
                        (float)_rng.NextDouble() * (FINE_MAX * 0.6f);
                    sanction.PointsPenalty = POINTS_PENALTY_MIN +
                        _rng.Next(0, POINTS_PENALTY_MAX / 2);
                    sanction.Description = $"Penalización severa: -{sanction.PointsPenalty} puntos, " +
                        $"multa ${sanction.FineAmount:F1}M. {reason}";
                    break;

                case "Extreme":
                    // Descalificación posible
                    if ((float)_rng.NextDouble() < SEASON_DISQUALIFICATION_CHANCE * 5f)
                    {
                        sanction.Type = "SeasonDisqualification";
                        sanction.PointsPenalty = int.MaxValue;
                        sanction.FineAmount = FINE_MAX;
                        sanction.Description = $"¡DESCALIFICACIÓN DE LA TEMPORADA! {reason}";
                    }
                    else
                    {
                        sanction.Type = "RaceExclusion";
                        sanction.FineAmount = FINE_MAX;
                        sanction.PointsPenalty = POINTS_PENALTY_MAX;
                        sanction.Description = $"Exclusión + multa máxima: -{sanction.PointsPenalty} puntos. {reason}";
                    }
                    break;
            }

            // Aplicar efectos financieros
            if (sanction.FineAmount > 0 && _budgetManager != null && team != null)
            {
                _budgetManager.AddExpense(team, sanction.FineAmount,
                    $"Multa FIA: {sanction.Description}");
            }

            // Aplicar penalización de puntos
            if (sanction.PointsPenalty > 0 && team != null)
            {
                team.constructorPoints = Math.Max(0,
                    team.constructorPoints - sanction.PointsPenalty);
            }

            // Guardar en historial
            _sanctionHistory.Add(sanction);

            // Disparar evento de sanción aplicada
            _eventBus.FireFIAInvestigation(new EventBus.FIAInvestigationArgs
            {
                TeamId = teamId,
                ComponentId = "",
                InvestigationReason = reason,
                WasDetected = true,
                SanctionType = sanction.Type,
                FineAmount = sanction.FineAmount,
                PointsPenalty = sanction.PointsPenalty,
                Description = sanction.Description
            });

            return sanction;
        }

        // ══════════════════════════════════════════════════════
        // SEVERIDAD
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Determina la severidad de la sanción basada en la legalidad
        /// y el historial de infracciones previas.
        /// El historial agrava: cada infracción previa sube un nivel.
        /// </summary>
        private string DetermineSeverity(string legality, int priorInfractions)
        {
            int baseSeverity;

            switch (legality)
            {
                case "Illegal":
                    baseSeverity = 3; // Severe
                    break;
                case "GreyAggressive":
                    baseSeverity = 2; // Moderate
                    break;
                case "GreySubtle":
                    baseSeverity = 1; // Light
                    break;
                default:
                    baseSeverity = 1; // Light por defecto
                    break;
            }

            // El historial agrava: +1 nivel por cada 2 infracciones previas
            baseSeverity += priorInfractions / 2;

            // Clamp
            baseSeverity = Math.Min(baseSeverity, 4);

            switch (baseSeverity)
            {
                case 1: return "Light";
                case 2: return "Moderate";
                case 3: return "Severe";
                default: return "Extreme";
            }
        }

        // ══════════════════════════════════════════════════════
        // APELACIONES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Inicia una apelación contra una sanción.
        /// 30% de éxito, demora 2 semanas.
        /// Solo se puede apelar sanciones de tipo Moderate o superior.
        /// </summary>
        /// <returns>true si la apelación fue aceptada para procesarse</returns>
        public bool FileAppeal(string sanctionId)
        {
            var sanction = _sanctionHistory.Find(s => s.SanctionId == sanctionId);
            if (sanction == null) return false;

            // No se puede apelar advertencias
            if (sanction.Severity == "Light") return false;

            // No se puede apelar dos veces
            if (sanction.WasAppealed) return false;

            sanction.WasAppealed = true;
            sanction.AppealWeeksRemaining = APPEAL_DURATION_WEEKS;

            _pendingAppeals.Add(sanction);

            return true;
        }

        /// <summary>
        /// Avanza una semana en las apelaciones pendientes.
        /// </summary>
        /// <returns>Lista de apelaciones resueltas esta semana</returns>
        public List<SanctionRecord> AdvanceAppealWeek(List<TeamData> teams)
        {
            var resolved = new List<SanctionRecord>();

            foreach (var appeal in _pendingAppeals)
            {
                appeal.AppealWeeksRemaining--;

                if (appeal.AppealWeeksRemaining <= 0)
                {
                    // Resolver apelación
                    appeal.AppealSuccessful = (float)_rng.NextDouble() < APPEAL_SUCCESS_CHANCE;

                    if (appeal.AppealSuccessful)
                    {
                        // Revertir sanción
                        var team = teams.Find(t => t.id == appeal.TeamId);
                        if (team != null)
                        {
                            // Devolver puntos
                            if (appeal.PointsPenalty > 0 &&
                                appeal.PointsPenalty < int.MaxValue)
                            {
                                team.constructorPoints += appeal.PointsPenalty;
                            }

                            // Devolver multa
                            if (appeal.FineAmount > 0 && _budgetManager != null)
                            {
                                _budgetManager.AddIncome(team, appeal.FineAmount,
                                    "Devolución por apelación exitosa");
                            }
                        }

                        // Generar noticia de apelación exitosa
                        _eventBus.FireNewsGenerated(new EventBus.NewsGeneratedArgs
                        {
                            NewsId = $"news_appeal_{_rng.Next(100000)}",
                            Headline = $"Apelación exitosa: {appeal.TeamId} revierte sanción FIA",
                            Body = $"La apelación contra '{appeal.Description}' fue aceptada.",
                            Type = "Praise",
                            MediaOutlet = "FIA Official",
                            IsRumor = false,
                            IsTrue = true,
                            RelatedPilotIds = new List<string>(),
                            RelatedTeamIds = new List<string> { appeal.TeamId }
                        });
                    }

                    resolved.Add(appeal);
                }
            }

            // Limpiar resueltas
            _pendingAppeals.RemoveAll(a => a.AppealWeeksRemaining <= 0);

            return resolved;
        }

        // ══════════════════════════════════════════════════════
        // HANDLERS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Cuando una investigación se resuelve con detección,
        /// aplicar la sanción automáticamente.
        /// </summary>
        private void HandleInvestigationResolved(object sender,
            EventBus.FIAInvestigationArgs args)
        {
            // Solo procesar si fue detectado y el tipo es "Pending"
            // (para evitar re-procesar nuestros propios eventos de sanción)
            if (args.WasDetected && args.SanctionType == "Pending")
            {
                // La sanción se aplica externamente llamando a DetermineSanction
                // desde el flujo principal del juego, para tener acceso al TeamData
            }
        }

        // ══════════════════════════════════════════════════════
        // CONSULTAS
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene historial de sanciones de un equipo</summary>
        public List<SanctionRecord> GetTeamSanctions(string teamId)
        {
            return _sanctionHistory.FindAll(s => s.TeamId == teamId);
        }

        /// <summary>Obtiene sanciones del último año</summary>
        public List<SanctionRecord> GetCurrentSeasonSanctions(int currentSeason)
        {
            return _sanctionHistory.FindAll(s => s.SeasonApplied == currentSeason);
        }

        /// <summary>¿Tiene apelaciones pendientes?</summary>
        public bool HasPendingAppeals(string teamId)
        {
            return _pendingAppeals.Exists(a => a.TeamId == teamId);
        }

        /// <summary>Limpia suscripciones</summary>
        public void Dispose()
        {
            _eventBus.OnFIAInvestigation -= HandleInvestigationResolved;
        }
    }
}
