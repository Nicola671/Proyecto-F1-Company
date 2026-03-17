// ============================================================
// F1 Career Manager — StaffManager.cs
// Gestión de staff del equipo — 10 roles del GDD
// ============================================================
// DEPENDENCIAS: EventBus.cs, StaffData.cs, TeamData.cs, Constants.cs
// EVENTOS QUE DISPARA: OnStaffChanged
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.Staff
{
    /// <summary>
    /// Gestiona el staff completo de todos los equipos.
    /// Expone GetTotalBonus(role, teamId) para que otros sistemas
    /// consulten los bonuses acumulados. 10 roles del GDD.
    /// </summary>
    public class StaffManager
    {
        // ── Datos ────────────────────────────────────────────
        private List<StaffData> _allStaff;
        private EventBus _eventBus;
        private Random _rng;

        // ── Constantes ───────────────────────────────────────
        private const float STEAL_BASE_CHANCE_PER_STAR = 0.10f;  // 10% por estrella
        private const int RETIREMENT_MIN_AGE = 58;
        private const int RETIREMENT_MAX_AGE = 70;
        private const float RETIREMENT_PROB_PER_YEAR = 0.08f;    // 8% por año sobre min

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public StaffManager(List<StaffData> allStaff, Random rng = null)
        {
            _allStaff = allStaff ?? new List<StaffData>();
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
        }

        // ══════════════════════════════════════════════════════
        // CONSULTA DE BONUSES — API PRINCIPAL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene el bonus total de un rol específico para un equipo.
        /// Suma contribuciones de staff principal + secundario del área.
        /// Otros sistemas llaman a este método para obtener sus bonuses.
        /// </summary>
        /// <param name="role">Rol a consultar (ej: "TechnicalDirector")</param>
        /// <param name="teamId">ID del equipo</param>
        /// <returns>Bonus total (0.0 a ~0.30 según nivel)</returns>
        public float GetTotalBonus(string role, string teamId)
        {
            float totalBonus = 0f;

            foreach (var staff in _allStaff)
            {
                if (staff.teamId != teamId) continue;
                if (staff.isBurnedOut || staff.isRetired) continue;

                // Bonus directo si es del rol
                if (staff.role == role)
                {
                    totalBonus += staff.GetRoleBonus();

                    // Motivación afecta el rendimiento
                    float motivationFactor = staff.motivation / 100f;
                    totalBonus *= (0.7f + motivationFactor * 0.3f);
                }

                // Bonus secundario si su especialidad coincide
                if (staff.secondarySpecialty == role)
                    totalBonus += staff.GetRoleBonus() * 0.3f;
            }

            return totalBonus;
        }

        /// <summary>
        /// Obtiene el bonus del Director Técnico (velocidad R&D)
        /// </summary>
        public float GetRnDSpeedBonus(string teamId)
        {
            return GetTotalBonus("TechnicalDirector", teamId);
        }

        /// <summary>
        /// Obtiene el bonus aerodinámico del Jefe Aero
        /// </summary>
        public float GetAeroBonus(string teamId)
        {
            return GetTotalBonus("AeroChief", teamId);
        }

        /// <summary>
        /// Obtiene el bonus de fiabilidad del Jefe Motor
        /// </summary>
        public float GetEngineReliabilityBonus(string teamId)
        {
            return GetTotalBonus("EngineChief", teamId);
        }

        /// <summary>
        /// Obtiene el bonus de estrategia del Ingeniero de Carrera
        /// </summary>
        public float GetStrategyBonus(string teamId)
        {
            return GetTotalBonus("RaceEngineer", teamId);
        }

        /// <summary>
        /// Obtiene el bonus del Analista de Datos (setup + debilidades)
        /// </summary>
        public float GetDataAnalysisBonus(string teamId)
        {
            return GetTotalBonus("DataAnalyst", teamId);
        }

        /// <summary>
        /// Obtiene el factor de recuperación del Médico
        /// </summary>
        public float GetMedicalRecoveryFactor(string teamId)
        {
            return GetTotalBonus("TeamDoctor", teamId);
        }

        /// <summary>
        /// Obtiene la reducción de rumores negativos del Jefe de Comunicaciones
        /// </summary>
        public float GetCommsRumorReduction(string teamId)
        {
            return GetTotalBonus("CommsChief", teamId);
        }

        /// <summary>
        /// Obtiene el bonus de desarrollo junior del Director de Academia
        /// </summary>
        public float GetAcademyBonus(string teamId)
        {
            return GetTotalBonus("AcademyDirector", teamId);
        }

        /// <summary>
        /// Obtiene el bonus de ingresos sponsors del Jefe Financiero
        /// </summary>
        public float GetFinanceBonus(string teamId)
        {
            return GetTotalBonus("FinanceDirector", teamId);
        }

        /// <summary>
        /// Obtiene el bonus de espionaje (si tiene Espía)
        /// </summary>
        public float GetSpyBonus(string teamId)
        {
            return GetTotalBonus("Spy", teamId);
        }

        // ══════════════════════════════════════════════════════
        // GESTIÓN DE STAFF
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Ficha un nuevo miembro de staff para un equipo
        /// </summary>
        public bool HireStaff(StaffData staff, TeamData team)
        {
            if (!staff.isAvailable) return false;

            string previousTeamId = staff.teamId;
            staff.teamId = team.id;
            staff.isAvailable = false;
            staff.motivation = 80 + _rng.Next(0, 21); // Motivado al empezar

            _eventBus.FireStaffChanged(new EventBus.StaffChangedArgs
            {
                StaffId = staff.id,
                StaffName = $"{staff.firstName} {staff.lastName}",
                TeamId = team.id,
                Role = staff.role,
                ChangeType = "Hired",
                PreviousTeamId = previousTeamId
            });

            return true;
        }

        /// <summary>
        /// Despide a un miembro de staff
        /// </summary>
        public void FireStaff(StaffData staff)
        {
            string previousTeamId = staff.teamId;
            staff.teamId = "";
            staff.isAvailable = true;

            _eventBus.FireStaffChanged(new EventBus.StaffChangedArgs
            {
                StaffId = staff.id,
                StaffName = $"{staff.firstName} {staff.lastName}",
                TeamId = "",
                Role = staff.role,
                ChangeType = "Fired",
                PreviousTeamId = previousTeamId
            });
        }

        // ══════════════════════════════════════════════════════
        // ROBO DE STAFF POR RIVALES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Simula intentos de robo de staff por equipos rivales.
        /// Prob = (estrellas × 10%) - (lealtad ÷ 100).
        /// Llamar entre temporadas.
        /// </summary>
        public List<StaffStealAttempt> ProcessRivalStealAttempts(
            List<TeamData> teams, string difficulty)
        {
            var attempts = new List<StaffStealAttempt>();
            float difficultyMultiplier = difficulty == "Hard" ? 1.3f :
                                         difficulty == "Legend" ? 1.6f : 1.0f;

            foreach (var staff in _allStaff)
            {
                if (staff.isRetired || string.IsNullOrEmpty(staff.teamId))
                    continue;
                if (staff.stars < 3) continue; // Solo roban buenos

                float stealChance = (staff.stars * STEAL_BASE_CHANCE_PER_STAR)
                                    - (staff.loyalty / 100f);
                stealChance *= difficultyMultiplier;
                stealChance = Math.Max(0f, Math.Min(stealChance, 0.50f));

                if ((float)_rng.NextDouble() < stealChance)
                {
                    // Buscar equipo rival interesado
                    var rival = FindInterestedRival(staff, teams);
                    if (rival != null)
                    {
                        attempts.Add(new StaffStealAttempt
                        {
                            Staff = staff,
                            RivalTeamId = rival.id,
                            RivalTeamName = rival.shortName,
                            OfferSalary = staff.salary * (1.3f + (float)_rng.NextDouble() * 0.4f),
                            StealProbability = stealChance
                        });
                    }
                }
            }

            return attempts;
        }

        /// <summary>
        /// Confirma el robo de un staff por un rival (si el jugador no iguala)
        /// </summary>
        public void ConfirmStaffSteal(StaffStealAttempt attempt)
        {
            string previousTeam = attempt.Staff.teamId;
            attempt.Staff.teamId = attempt.RivalTeamId;
            attempt.Staff.salary = attempt.OfferSalary;
            attempt.Staff.loyalty = 40 + _rng.Next(0, 20); // Reset lealtad

            _eventBus.FireStaffChanged(new EventBus.StaffChangedArgs
            {
                StaffId = attempt.Staff.id,
                StaffName = $"{attempt.Staff.firstName} {attempt.Staff.lastName}",
                TeamId = attempt.RivalTeamId,
                Role = attempt.Staff.role,
                ChangeType = "StolenByRival",
                PreviousTeamId = previousTeam
            });
        }

        // ══════════════════════════════════════════════════════
        // ACTUALIZACIÓN SEMANAL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Actualización semanal del staff: motivación, burnout, envejecimiento
        /// </summary>
        public void WeeklyUpdate()
        {
            foreach (var staff in _allStaff)
            {
                if (staff.isRetired || string.IsNullOrEmpty(staff.teamId))
                    continue;

                // Motivación tiende al neutro (70)
                if (staff.motivation > 75)
                    staff.motivation -= _rng.Next(0, 3);
                else if (staff.motivation < 60)
                    staff.motivation += _rng.Next(0, 3);

                // Recuperación de burnout
                if (staff.isBurnedOut)
                {
                    staff.burnoutRecoveryWeeks--;
                    if (staff.burnoutRecoveryWeeks <= 0)
                    {
                        staff.isBurnedOut = false;
                        staff.motivation = 60;
                    }
                }

                // Lealtad sube ligeramente con el tiempo
                if (staff.contractYearsLeft > 1)
                    staff.loyalty = Math.Min(100, staff.loyalty + _rng.Next(0, 2));
            }
        }

        /// <summary>
        /// Procesamiento de fin de temporada: envejecimiento, retiros, contratos
        /// </summary>
        public void ProcessEndOfSeason()
        {
            foreach (var staff in _allStaff)
            {
                if (staff.isRetired) continue;

                staff.age++;
                staff.contractYearsLeft--;

                // Retiro por edad
                if (staff.age >= RETIREMENT_MIN_AGE)
                {
                    float retireChance = (staff.age - RETIREMENT_MIN_AGE) *
                                         RETIREMENT_PROB_PER_YEAR;
                    if ((float)_rng.NextDouble() < retireChance)
                    {
                        staff.isRetired = true;
                        staff.isAvailable = false;
                        string oldTeam = staff.teamId;
                        staff.teamId = "";

                        _eventBus.FireStaffChanged(new EventBus.StaffChangedArgs
                        {
                            StaffId = staff.id,
                            StaffName = $"{staff.firstName} {staff.lastName}",
                            TeamId = "",
                            Role = staff.role,
                            ChangeType = "Retired",
                            PreviousTeamId = oldTeam
                        });
                    }
                }

                // Contrato expirado
                if (staff.contractYearsLeft <= 0 && !staff.isRetired)
                {
                    staff.isAvailable = true;
                }
            }
        }

        // ══════════════════════════════════════════════════════
        // CONSULTAS
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene todo el staff de un equipo</summary>
        public List<StaffData> GetTeamStaff(string teamId)
        {
            return _allStaff.FindAll(s => s.teamId == teamId && !s.isRetired);
        }

        /// <summary>Obtiene staff disponible para fichaje</summary>
        public List<StaffData> GetAvailableStaff()
        {
            return _allStaff.FindAll(s => s.isAvailable && !s.isRetired);
        }

        /// <summary>Obtiene staff de un rol específico en un equipo</summary>
        public StaffData GetStaffByRole(string role, string teamId)
        {
            return _allStaff.Find(s =>
                s.role == role && s.teamId == teamId && !s.isRetired);
        }

        /// <summary>Verifica si un equipo tiene un rol cubierto</summary>
        public bool HasRole(string role, string teamId)
        {
            return GetStaffByRole(role, teamId) != null;
        }

        /// <summary>Agrega staff al pool global</summary>
        public void AddStaff(StaffData staff)
        {
            _allStaff.Add(staff);
        }

        private TeamData FindInterestedRival(StaffData staff, List<TeamData> teams)
        {
            // Buscar equipo que necesite ese rol y pueda pagar
            foreach (var team in teams)
            {
                if (team.id == staff.teamId) continue;
                if (!HasRole(staff.role, team.id) ||
                    GetStaffByRole(staff.role, team.id).stars < staff.stars)
                {
                    if (team.budget > staff.salary * 2f)
                        return team;
                }
            }
            return null;
        }
    }

    /// <summary>Info sobre un intento de robo de staff</summary>
    public class StaffStealAttempt
    {
        public StaffData Staff;
        public string RivalTeamId;
        public string RivalTeamName;
        public float OfferSalary;
        public float StealProbability;
    }
}
