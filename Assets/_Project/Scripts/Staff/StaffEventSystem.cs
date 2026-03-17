// ============================================================
// F1 Career Manager — StaffEventSystem.cs
// Eventos aleatorios del staff — verificar cada semana
// ============================================================
// DEPENDENCIAS: StaffManager.cs, StaffData.cs, EventBus.cs,
//               BudgetManager.cs, Constants.cs
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;
using F1CareerManager.AI.EconomyAI;

namespace F1CareerManager.Staff
{
    /// <summary>
    /// Tipo de evento de staff
    /// </summary>
    public class StaffEvent
    {
        public string EventId;
        public string Type;          // "RivalOffer", "Conflict", "Burnout", "Genius", "SpyCaught"
        public string Title;
        public string Description;
        public string AffectedStaffId;
        public string AffectedTeamId;
        public List<StaffEventOption> Options;
        public bool RequiresPlayerAction;
        public bool IsResolved;
    }

    public class StaffEventOption
    {
        public string Label;
        public string Description;
        public Action Effect;
    }

    /// <summary>
    /// Genera y procesa eventos aleatorios del staff.
    /// Se verifica cada semana del calendario.
    /// </summary>
    public class StaffEventSystem
    {
        // ── Datos ────────────────────────────────────────────
        private StaffManager _staffManager;
        private BudgetManager _budgetManager;
        private EventBus _eventBus;
        private Random _rng;
        private List<StaffEvent> _pendingEvents;

        // ── Probabilidades base ──────────────────────────────
        private const float RIVAL_OFFER_CHANCE = 0.04f;      // 4% por semana
        private const float CONFLICT_CHANCE = 0.03f;          // 3% por semana
        private const float BURNOUT_CHANCE = 0.05f;           // 5% por semana
        private const float GENIUS_CHANCE = 0.02f;            // 2% por semana
        private const float SPY_CAUGHT_CHANCE = 0.06f;        // 6% por semana (si tiene espía)

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public StaffEventSystem(StaffManager staffManager,
            BudgetManager budgetManager, Random rng = null)
        {
            _staffManager = staffManager;
            _budgetManager = budgetManager;
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
            _pendingEvents = new List<StaffEvent>();
        }

        // ══════════════════════════════════════════════════════
        // CHECK SEMANAL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Verifica y genera eventos de staff para esta semana.
        /// Llamar una vez por semana de juego.
        /// </summary>
        public List<StaffEvent> CheckWeeklyEvents(string playerTeamId,
            List<TeamData> teams)
        {
            var newEvents = new List<StaffEvent>();
            var playerStaff = _staffManager.GetTeamStaff(playerTeamId);

            foreach (var staff in playerStaff)
            {
                if (staff.isRetired || staff.isBurnedOut) continue;

                // 1. Rival hace oferta a tu ingeniero estrella
                if (staff.stars >= 3 && (float)_rng.NextDouble() < RIVAL_OFFER_CHANCE)
                {
                    newEvents.Add(CreateRivalOfferEvent(staff, teams, playerTeamId));
                }

                // 2. Burnout por exceso de trabajo
                if (staff.motivation < 40 && (float)_rng.NextDouble() < BURNOUT_CHANCE)
                {
                    newEvents.Add(CreateBurnoutEvent(staff, playerTeamId));
                }

                // 3. Genio inesperado (staff bajo que tiene idea brillante)
                if (staff.stars <= 2 && (float)_rng.NextDouble() < GENIUS_CHANCE)
                {
                    newEvents.Add(CreateGeniusEvent(staff, playerTeamId));
                }

                // 4. Espía detectado
                if (staff.role == "Spy" && (float)_rng.NextDouble() < SPY_CAUGHT_CHANCE)
                {
                    newEvents.Add(CreateSpyCaughtEvent(staff, playerTeamId));
                }
            }

            // 5. Conflicto entre staff (check global)
            if (playerStaff.Count >= 2 && (float)_rng.NextDouble() < CONFLICT_CHANCE)
            {
                int idx1 = _rng.Next(playerStaff.Count);
                int idx2 = _rng.Next(playerStaff.Count);
                if (idx1 != idx2)
                {
                    newEvents.Add(CreateConflictEvent(
                        playerStaff[idx1], playerStaff[idx2], playerTeamId));
                }
            }

            _pendingEvents.AddRange(newEvents);
            return newEvents;
        }

        // ══════════════════════════════════════════════════════
        // CREACIÓN DE EVENTOS
        // ══════════════════════════════════════════════════════

        private StaffEvent CreateRivalOfferEvent(StaffData staff,
            List<TeamData> teams, string playerTeamId)
        {
            // Buscar rival creíble
            string rivalName = "Un equipo rival";
            float offerSalary = staff.salary * (1.4f + (float)_rng.NextDouble() * 0.3f);
            foreach (var t in teams)
            {
                if (t.id != playerTeamId && t.budget > offerSalary * 3f)
                {
                    rivalName = t.shortName;
                    break;
                }
            }

            var evt = new StaffEvent
            {
                EventId = $"staff_evt_{_rng.Next(100000)}",
                Type = "RivalOffer",
                Title = "📋 Oferta rival por tu staff",
                Description = $"{rivalName} quiere fichar a {staff.firstName} {staff.lastName} " +
                    $"({staff.role}, {staff.stars}⭐). Ofrecen ${offerSalary:F1}M/año.",
                AffectedStaffId = staff.id,
                AffectedTeamId = playerTeamId,
                RequiresPlayerAction = true,
                IsResolved = false,
                Options = new List<StaffEventOption>
                {
                    new StaffEventOption
                    {
                        Label = "Igualar oferta",
                        Description = $"Subir salario a ${offerSalary:F1}M para retenerlo",
                        Effect = () => {
                            staff.salary = offerSalary;
                            staff.loyalty += 10;
                            staff.motivation += 5;
                        }
                    },
                    new StaffEventOption
                    {
                        Label = "Contraoferta moral",
                        Description = "Hablar con él, apelar a la lealtad (50% éxito)",
                        Effect = () => {
                            if (_rng.NextDouble() < 0.5)
                            {
                                staff.loyalty += 5;
                            }
                            else
                            {
                                staff.loyalty -= 15;
                                staff.motivation -= 10;
                            }
                        }
                    },
                    new StaffEventOption
                    {
                        Label = "Dejarlo ir",
                        Description = "No impedirlo, perderás su contribución",
                        Effect = () => {
                            _staffManager.FireStaff(staff);
                        }
                    }
                }
            };

            return evt;
        }

        private StaffEvent CreateBurnoutEvent(StaffData staff, string teamId)
        {
            return new StaffEvent
            {
                EventId = $"staff_evt_{_rng.Next(100000)}",
                Type = "Burnout",
                Title = "🔥 Burnout en el equipo",
                Description = $"{staff.firstName} {staff.lastName} ({staff.role}) muestra " +
                    "signos de agotamiento extremo. Su rendimiento ha caído.",
                AffectedStaffId = staff.id,
                AffectedTeamId = teamId,
                RequiresPlayerAction = true,
                IsResolved = false,
                Options = new List<StaffEventOption>
                {
                    new StaffEventOption
                    {
                        Label = "Dar descanso (3 semanas)",
                        Description = "Pierde contribución 3 semanas, se recupera al 100%",
                        Effect = () => {
                            staff.isBurnedOut = true;
                            staff.burnoutRecoveryWeeks = 3;
                            staff.motivation = 50;
                        }
                    },
                    new StaffEventOption
                    {
                        Label = "Forzar a seguir",
                        Description = "Mantiene contribución reducida, riesgo de renuncia",
                        Effect = () => {
                            staff.motivation -= 25;
                            staff.loyalty -= 15;
                            if (staff.motivation < 20)
                            {
                                staff.isBurnedOut = true;
                                staff.burnoutRecoveryWeeks = 6;
                            }
                        }
                    },
                    new StaffEventOption
                    {
                        Label = "Bonus motivacional ($0.5M)",
                        Description = "Pagar bonus para mantener operativo",
                        Effect = () => {
                            staff.motivation += 15;
                            staff.loyalty += 5;
                        }
                    }
                }
            };
        }

        private StaffEvent CreateConflictEvent(StaffData staff1,
            StaffData staff2, string teamId)
        {
            return new StaffEvent
            {
                EventId = $"staff_evt_{_rng.Next(100000)}",
                Type = "Conflict",
                Title = "⚔️ Conflicto interno",
                Description = $"Tensión entre {staff1.firstName} {staff1.lastName} ({staff1.role}) " +
                    $"y {staff2.firstName} {staff2.lastName} ({staff2.role}). " +
                    "El rendimiento del área se resiente.",
                AffectedStaffId = staff1.id,
                AffectedTeamId = teamId,
                RequiresPlayerAction = true,
                IsResolved = false,
                Options = new List<StaffEventOption>
                {
                    new StaffEventOption
                    {
                        Label = "Mediar entre ambos",
                        Description = "Intentar resolver (60% éxito, sino empeora)",
                        Effect = () => {
                            if (_rng.NextDouble() < 0.6)
                            {
                                staff1.pilotRelation += 10;
                                staff2.pilotRelation += 10;
                                staff1.motivation += 5;
                                staff2.motivation += 5;
                            }
                            else
                            {
                                staff1.motivation -= 10;
                                staff2.motivation -= 10;
                            }
                        }
                    },
                    new StaffEventOption
                    {
                        Label = $"Apoyar a {staff1.lastName}",
                        Description = $"{staff2.lastName} quedará resentido",
                        Effect = () => {
                            staff1.motivation += 10;
                            staff1.loyalty += 5;
                            staff2.motivation -= 15;
                            staff2.loyalty -= 10;
                        }
                    },
                    new StaffEventOption
                    {
                        Label = $"Apoyar a {staff2.lastName}",
                        Description = $"{staff1.lastName} quedará resentido",
                        Effect = () => {
                            staff2.motivation += 10;
                            staff2.loyalty += 5;
                            staff1.motivation -= 15;
                            staff1.loyalty -= 10;
                        }
                    },
                    new StaffEventOption
                    {
                        Label = "Ignorar el conflicto",
                        Description = "Ambos bajan rendimiento, se solucionará solo... o no",
                        Effect = () => {
                            staff1.motivation -= 8;
                            staff2.motivation -= 8;
                        }
                    }
                }
            };
        }

        private StaffEvent CreateGeniusEvent(StaffData staff, string teamId)
        {
            return new StaffEvent
            {
                EventId = $"staff_evt_{_rng.Next(100000)}",
                Type = "Genius",
                Title = "💡 ¡Idea brillante!",
                Description = $"¡{staff.firstName} {staff.lastName} ({staff.role}, {staff.stars}⭐) " +
                    "tuvo una idea revolucionaria! Si funciona, el próximo componente " +
                    "en su área tendrá +20% rendimiento.",
                AffectedStaffId = staff.id,
                AffectedTeamId = teamId,
                RequiresPlayerAction = false,
                IsResolved = true,
                Options = new List<StaffEventOption>()
            };

            // Efecto: se aplica externamente al próximo componente del área
        }

        private StaffEvent CreateSpyCaughtEvent(StaffData staff, string teamId)
        {
            return new StaffEvent
            {
                EventId = $"staff_evt_{_rng.Next(100000)}",
                Type = "SpyCaught",
                Title = "🚨 ¡Espía descubierto!",
                Description = $"La FIA descubrió que {staff.firstName} {staff.lastName} " +
                    "espiaba a equipos rivales. Se esperan sanciones severas.",
                AffectedStaffId = staff.id,
                AffectedTeamId = teamId,
                RequiresPlayerAction = true,
                IsResolved = false,
                Options = new List<StaffEventOption>
                {
                    new StaffEventOption
                    {
                        Label = "Negar todo",
                        Description = "70% multa FIA, 30% se sale (pero peor si falla)",
                        Effect = () => {
                            if (_rng.NextDouble() < 0.7)
                            {
                                // Multa severa
                                _staffManager.FireStaff(staff);
                            }
                        }
                    },
                    new StaffEventOption
                    {
                        Label = "Cooperar con la FIA",
                        Description = "Multa reducida, pierdes al espía",
                        Effect = () => {
                            _staffManager.FireStaff(staff);
                        }
                    },
                    new StaffEventOption
                    {
                        Label = "Culpar al espía solo",
                        Description = "Espía despedido, reputación del equipo dañada",
                        Effect = () => {
                            _staffManager.FireStaff(staff);
                        }
                    }
                }
            };
        }

        // ══════════════════════════════════════════════════════
        // RESOLUCIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Resuelve un evento pendiente con la opción elegida por el jugador
        /// </summary>
        public void ResolveEvent(string eventId, int optionIndex)
        {
            var evt = _pendingEvents.Find(e => e.EventId == eventId);
            if (evt == null || evt.IsResolved) return;

            if (optionIndex >= 0 && optionIndex < evt.Options.Count)
            {
                evt.Options[optionIndex].Effect?.Invoke();
            }

            evt.IsResolved = true;
        }

        /// <summary>Obtiene eventos pendientes de resolución</summary>
        public List<StaffEvent> GetPendingEvents()
        {
            return _pendingEvents.FindAll(e => !e.IsResolved && e.RequiresPlayerAction);
        }

        /// <summary>Limpia eventos resueltos</summary>
        public void CleanResolvedEvents()
        {
            _pendingEvents.RemoveAll(e => e.IsResolved);
        }
    }
}
