// ============================================================
// F1 Career Manager — TransferManager.cs
// Gestión de fichajes y transferencias
// ============================================================
// DEPENDENCIAS: EventBus.cs, PilotData.cs, TeamData.cs,
//               NegotiationSystem.cs, BudgetManager.cs
// EVENTOS QUE DISPARA: OnRivalTransfer, OnContractSigned
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;
using F1CareerManager.AI.EconomyAI;

namespace F1CareerManager.Market
{
    /// <summary>Estado de la ventana de transferencias</summary>
    public enum TransferWindowState
    {
        Closed,           // Fuera de ventana
        MainWindow,       // Ventana principal (entre temporadas, 8 semanas)
        EmergencyWindow   // Ventana de emergencia (lesión grave)
    }

    /// <summary>Registro de una transferencia completada</summary>
    public class TransferRecord
    {
        public string PilotId;
        public string FromTeamId;
        public string ToTeamId;
        public float TransferFee;
        public float NewSalary;
        public int ContractYears;
        public string Role;
        public int Season;
    }

    /// <summary>
    /// Gestiona ventanas de transferencia, listas de pilotos disponibles,
    /// fichajes del jugador y transferencias de rivales.
    /// </summary>
    public class TransferManager
    {
        // ── Datos ────────────────────────────────────────────
        private EventBus _eventBus;
        private BudgetManager _budgetManager;
        private NegotiationSystem _negotiation;
        private Random _rng;
        private TransferWindowState _windowState;
        private int _windowWeeksRemaining;
        private List<TransferRecord> _transferHistory;
        private List<string> _activeNegotiationPilotIds;

        // ── Constantes ───────────────────────────────────────
        private const int MAIN_WINDOW_WEEKS = 8;
        private const int EMERGENCY_WINDOW_WEEKS = 3;
        private const float OUT_OF_WINDOW_PENALTY = 0.25f;  // +25% costo
        private const float RIVAL_SNATCH_PROB_BASE = 0.12f;  // 12% base rival ficha

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public TransferManager(BudgetManager budgetManager, Random rng = null)
        {
            _budgetManager = budgetManager;
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
            _negotiation = new NegotiationSystem(budgetManager, _rng);
            _windowState = TransferWindowState.Closed;
            _windowWeeksRemaining = 0;
            _transferHistory = new List<TransferRecord>();
            _activeNegotiationPilotIds = new List<string>();
        }

        // ══════════════════════════════════════════════════════
        // VENTANA DE TRANSFERENCIAS
        // ══════════════════════════════════════════════════════

        /// <summary>Abre la ventana principal (entre temporadas)</summary>
        public void OpenMainWindow()
        {
            _windowState = TransferWindowState.MainWindow;
            _windowWeeksRemaining = MAIN_WINDOW_WEEKS;
            _activeNegotiationPilotIds.Clear();
        }

        /// <summary>Abre ventana de emergencia (lesión grave)</summary>
        public void OpenEmergencyWindow()
        {
            _windowState = TransferWindowState.EmergencyWindow;
            _windowWeeksRemaining = EMERGENCY_WINDOW_WEEKS;
        }

        /// <summary>Avanza una semana en la ventana</summary>
        public void AdvanceWeek()
        {
            if (_windowState == TransferWindowState.Closed) return;

            _windowWeeksRemaining--;
            if (_windowWeeksRemaining <= 0)
            {
                _windowState = TransferWindowState.Closed;
                _activeNegotiationPilotIds.Clear();
            }
        }

        /// <summary>¿Está la ventana abierta?</summary>
        public bool IsWindowOpen()
        {
            return _windowState != TransferWindowState.Closed;
        }

        // ══════════════════════════════════════════════════════
        // LISTAS DE PILOTOS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene pilotos libres (sin equipo, disponibles)
        /// </summary>
        public List<PilotData> GetFreePilots(List<PilotData> allPilots)
        {
            return allPilots.FindAll(p =>
                !p.isRetired &&
                !p.isInjured &&
                p.isAvailable &&
                string.IsNullOrEmpty(p.currentTeamId));
        }

        /// <summary>
        /// Obtiene pilotos transferibles (con equipo pero último año o insatisfechos)
        /// </summary>
        public List<PilotData> GetTransferablePilots(List<PilotData> allPilots)
        {
            return allPilots.FindAll(p =>
                !p.isRetired &&
                !string.IsNullOrEmpty(p.currentTeamId) &&
                (p.contractYearsLeft <= 1 ||
                 p.mood == "WantsOut" ||
                 p.mood == "Furious"));
        }

        // ══════════════════════════════════════════════════════
        // INICIO DE NEGOCIACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Inicia una negociación por un piloto.
        /// Retorna null si no se puede negociar.
        /// </summary>
        public NegotiationState StartNegotiation(PilotData pilot,
            TeamData buyerTeam, string offeredRole)
        {
            // Verificar si ya estamos negociando con este piloto
            if (_activeNegotiationPilotIds.Contains(pilot.id))
                return null;

            // Verificar ventana
            bool outOfWindow = _windowState == TransferWindowState.Closed;

            var state = _negotiation.StartNegotiation(
                pilot, buyerTeam, offeredRole, outOfWindow);

            if (state != null)
                _activeNegotiationPilotIds.Add(pilot.id);

            return state;
        }

        /// <summary>
        /// Hace una oferta en una negociación activa
        /// </summary>
        public NegotiationResult MakeOffer(NegotiationState state,
            float salary, float releaseClause,
            float victoryBonus, float championBonus)
        {
            return _negotiation.MakeOffer(state, salary,
                releaseClause, victoryBonus, championBonus);
        }

        /// <summary>
        /// Completa una transferencia exitosa
        /// </summary>
        public void CompleteTransfer(NegotiationState negotiation,
            PilotData pilot, TeamData newTeam, int currentSeason)
        {
            string fromTeamId = pilot.currentTeamId;
            float transferFee = 0f;

            // Si tiene equipo, calcular fee de salida
            if (!string.IsNullOrEmpty(fromTeamId))
            {
                transferFee = pilot.releaseClause > 0
                    ? pilot.releaseClause : pilot.marketValue;

                // Penalización fuera de ventana
                if (_windowState == TransferWindowState.Closed)
                    transferFee *= (1f + OUT_OF_WINDOW_PENALTY);

                // Pagar fee
                _budgetManager.AddExpense(newTeam, transferFee,
                    $"Fee fichaje {pilot.firstName} {pilot.lastName}");
            }

            // Actualizar datos del piloto
            pilot.currentTeamId = newTeam.id;
            pilot.contractYearsLeft = negotiation.OfferedContractYears;
            pilot.salary = negotiation.FinalSalary;
            pilot.releaseClause = negotiation.FinalReleaseClause;
            pilot.role = negotiation.OfferedRole;
            pilot.isAvailable = false;
            pilot.currentCategory = "F1";

            // Registrar
            var record = new TransferRecord
            {
                PilotId = pilot.id,
                FromTeamId = fromTeamId,
                ToTeamId = newTeam.id,
                TransferFee = transferFee,
                NewSalary = negotiation.FinalSalary,
                ContractYears = negotiation.OfferedContractYears,
                Role = negotiation.OfferedRole,
                Season = currentSeason
            };
            _transferHistory.Add(record);
            _activeNegotiationPilotIds.Remove(pilot.id);

            // Disparar eventos
            _eventBus.FireContractSigned(new EventBus.ContractSignedArgs
            {
                PilotId = pilot.id,
                PilotName = $"{pilot.firstName} {pilot.lastName}",
                TeamId = newTeam.id,
                IsRenewal = fromTeamId == newTeam.id,
                Salary = negotiation.FinalSalary,
                ContractYears = negotiation.OfferedContractYears,
                Role = negotiation.OfferedRole
            });

            if (fromTeamId != newTeam.id)
            {
                _eventBus.FireRivalTransfer(new EventBus.RivalTransferArgs
                {
                    PilotId = pilot.id,
                    PilotName = $"{pilot.firstName} {pilot.lastName}",
                    FromTeamId = fromTeamId ?? "",
                    ToTeamId = newTeam.id,
                    TransferFee = transferFee,
                    Salary = negotiation.FinalSalary
                });
            }
        }

        // ══════════════════════════════════════════════════════
        // FICHAJES DE RIVALES (IA)
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Simula los fichajes de equipos rivales.
        /// Rivales fichan agresivamente en dificultad alta.
        /// Llamar durante la ventana de transferencias.
        /// </summary>
        public List<TransferRecord> ProcessRivalTransfers(
            List<PilotData> allPilots, List<TeamData> teams,
            string playerTeamId, string difficulty, int currentSeason)
        {
            var rivalTransfers = new List<TransferRecord>();
            float diffMultiplier = difficulty == "Hard" ? 1.5f :
                                   difficulty == "Legend" ? 2.0f : 1.0f;

            foreach (var team in teams)
            {
                if (team.id == playerTeamId || team.isPlayerControlled) continue;

                // ¿Necesita piloto?
                bool needsPilot = false;
                var teamPilots = allPilots.FindAll(p =>
                    p.currentTeamId == team.id && !p.isRetired);

                if (teamPilots.Count < 2)
                    needsPilot = true;
                else if (teamPilots.Exists(p => p.contractYearsLeft <= 0))
                    needsPilot = true;

                if (!needsPilot) continue;

                // Buscar piloto disponible
                var candidates = GetFreePilots(allPilots);
                candidates.AddRange(GetTransferablePilots(allPilots));

                // Filtrar: no fichar a pilotos del jugador activamente
                candidates.RemoveAll(p =>
                    p.currentTeamId == playerTeamId ||
                    _activeNegotiationPilotIds.Contains(p.id));

                if (candidates.Count == 0) continue;

                // Elegir al mejor candidato que puedan pagar
                candidates.Sort((a, b) => b.overallRating.CompareTo(a.overallRating));

                foreach (var candidate in candidates)
                {
                    float requiredSalary = candidate.salary > 0
                        ? candidate.salary : candidate.marketValue * 0.1f;

                    if (team.budget > requiredSalary * 3f)
                    {
                        float signChance = RIVAL_SNATCH_PROB_BASE * diffMultiplier;
                        if ((float)_rng.NextDouble() < signChance)
                        {
                            // Rival ficha al piloto
                            string fromTeam = candidate.currentTeamId;
                            candidate.currentTeamId = team.id;
                            candidate.contractYearsLeft = _rng.Next(1, 4);
                            candidate.salary = requiredSalary;
                            candidate.isAvailable = false;
                            candidate.currentCategory = "F1";

                            var record = new TransferRecord
                            {
                                PilotId = candidate.id,
                                FromTeamId = fromTeam,
                                ToTeamId = team.id,
                                TransferFee = candidate.marketValue,
                                NewSalary = requiredSalary,
                                ContractYears = candidate.contractYearsLeft,
                                Role = "Second",
                                Season = currentSeason
                            };
                            rivalTransfers.Add(record);
                            _transferHistory.Add(record);

                            // Si el jugador estaba negociando → lo perdió
                            if (_activeNegotiationPilotIds.Contains(candidate.id))
                            {
                                _activeNegotiationPilotIds.Remove(candidate.id);
                            }

                            _eventBus.FireRivalTransfer(new EventBus.RivalTransferArgs
                            {
                                PilotId = candidate.id,
                                PilotName = $"{candidate.firstName} {candidate.lastName}",
                                FromTeamId = fromTeam ?? "",
                                ToTeamId = team.id,
                                TransferFee = candidate.marketValue,
                                Salary = requiredSalary
                            });

                            break; // Solo ficha uno por equipo por cycle
                        }
                    }
                }
            }

            return rivalTransfers;
        }

        /// <summary>
        /// Verifica si un piloto en negociación fue fichado por un rival.
        /// Devuelve true si lo perdiste.
        /// </summary>
        public bool CheckRivalSnatch(string pilotId, string difficulty)
        {
            if (!_activeNegotiationPilotIds.Contains(pilotId)) return false;

            float snatchChance = RIVAL_SNATCH_PROB_BASE;
            if (difficulty == "Hard") snatchChance *= 1.5f;
            if (difficulty == "Legend") snatchChance *= 2.0f;

            return (float)_rng.NextDouble() < snatchChance;
        }

        // ══════════════════════════════════════════════════════
        // CONFLICTO DE ROLES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si hay conflicto de roles (dos pilotos #1 en el mismo equipo)
        /// </summary>
        public bool CheckRoleConflict(TeamData team, List<PilotData> allPilots)
        {
            var teamPilots = allPilots.FindAll(p =>
                p.currentTeamId == team.id && !p.isRetired);

            int firstDrivers = 0;
            foreach (var p in teamPilots)
                if (p.role == "First") firstDrivers++;

            return firstDrivers > 1;
        }

        // ══════════════════════════════════════════════════════
        // CONSULTAS
        // ══════════════════════════════════════════════════════

        public TransferWindowState GetWindowState() => _windowState;
        public int GetWeeksRemaining() => _windowWeeksRemaining;
        public List<TransferRecord> GetTransferHistory() => _transferHistory;
        public NegotiationSystem GetNegotiationSystem() => _negotiation;
    }
}
