// ============================================================
// F1 Career Manager — RecoverySystem.cs
// Recuperación de lesiones + piloto de reserva
// ============================================================
// DEPENDENCIAS: InjuryManager.cs, StaffManager.cs, PilotData.cs,
//               TeamData.cs
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Data;
using F1CareerManager.Staff;

namespace F1CareerManager.Injury
{
    /// <summary>
    /// Gestiona la recuperación de pilotos lesionados.
    /// El Médico del staff reduce tiempo de recuperación.
    /// Si no hay reserva disponible, asigna regen de F2 con stats -20%.
    /// </summary>
    public class RecoverySystem
    {
        // ── Datos ────────────────────────────────────────────
        private InjuryManager _injuryManager;
        private StaffManager _staffManager;
        private Random _rng;

        // ── Constantes ───────────────────────────────────────
        private const float DOCTOR_MAX_REDUCTION = 0.30f;   // Médico nivel 5: -30%
        private const float STAT_RECOVERY_RATE = 0.20f;     // 20% stats por carrera al volver
        private const float RESERVE_STAT_PENALTY = 0.20f;   // -20% stats para sustituto F2

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public RecoverySystem(InjuryManager injuryManager,
            StaffManager staffManager, Random rng = null)
        {
            _injuryManager = injuryManager;
            _staffManager = staffManager;
            _rng = rng ?? new Random();
        }

        // ══════════════════════════════════════════════════════
        // AVANCE DE RECUPERACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Avanza una carrera de recuperación para todos los pilotos lesionados.
        /// El médico del staff reduce el tiempo restante.
        /// Llamar después de cada carrera.
        /// </summary>
        /// <returns>Lista de pilotos que se recuperaron esta carrera</returns>
        public List<RecoveryResult> AdvanceRecovery(List<PilotData> allPilots)
        {
            var recovered = new List<RecoveryResult>();
            var injuries = _injuryManager.GetAllActiveInjuries();

            foreach (var injury in injuries)
            {
                if (injury.Severity == "CareerEnding") continue;

                var pilot = allPilots.Find(p => p.id == injury.PilotId);
                if (pilot == null) continue;

                // Calcular reducción del médico
                float doctorBonus = 0f;
                if (!string.IsNullOrEmpty(pilot.currentTeamId))
                {
                    doctorBonus = _staffManager.GetMedicalRecoveryFactor(
                        pilot.currentTeamId);
                }

                // Cada carrera reduce el tiempo restante
                float reduction = 1f + doctorBonus; // 1.0 base + bonus doctor
                injury.RacesRemaining -= (int)Math.Ceiling(reduction);

                if (injury.RacesRemaining <= 0)
                {
                    injury.RacesRemaining = 0;
                    injury.IsRecovered = true;

                    // Recuperar al piloto
                    pilot.isInjured = false;
                    pilot.racesUntilRecovery = 0;
                    pilot.injurySeverity = "";

                    var result = new RecoveryResult
                    {
                        PilotId = pilot.id,
                        PilotName = $"{pilot.firstName} {pilot.lastName}",
                        Severity = injury.Severity,
                        HasStatPenalty = injury.AffectsStats,
                        RemainingStatPenalty = injury.StatPenalty,
                        RacesToFullRecovery = injury.AffectsStats
                            ? (int)(injury.StatPenalty / STAT_RECOVERY_RATE) : 0
                    };

                    // Aplicar penalización temporal de stats al volver
                    if (injury.AffectsStats)
                    {
                        ApplyReturnPenalty(pilot, injury.StatPenalty);
                    }

                    recovered.Add(result);
                }
                else
                {
                    pilot.racesUntilRecovery = injury.RacesRemaining;
                }
            }

            return recovered;
        }

        /// <summary>
        /// Recuperación gradual de stats post-lesión.
        /// Llamar cada carrera que el piloto corre después de volver.
        /// Los stats van subiendo gradualmente.
        /// </summary>
        public void GradualStatRecovery(PilotData pilot)
        {
            // Si el piloto tenía penalización temporal, ir recuperando
            // Los stats se recuperan ~20% de la penalización por carrera
            float recoveryAmount = STAT_RECOVERY_RATE;

            // El médico acelera la recuperación gradual también
            if (!string.IsNullOrEmpty(pilot.currentTeamId))
            {
                float doctorBonus = _staffManager.GetMedicalRecoveryFactor(
                    pilot.currentTeamId);
                recoveryAmount += doctorBonus * 0.5f;
            }

            // Recuperar 1-2 puntos de stats por carrera
            int pointsToRecover = Math.Max(1, (int)Math.Round(recoveryAmount * 5f));
            int recovering = 0;

            // Subir stats aleatorios hasta el valor original
            for (int i = 0; i < pointsToRecover; i++)
            {
                int stat = _rng.Next(0, 10);
                switch (stat)
                {
                    case 0: if (pilot.speed < pilot.potential) { pilot.speed++; recovering++; } break;
                    case 1: if (pilot.consistency < pilot.potential) { pilot.consistency++; recovering++; } break;
                    case 2: if (pilot.rainSkill < pilot.potential) { pilot.rainSkill++; recovering++; } break;
                    case 3: if (pilot.startSkill < pilot.potential) { pilot.startSkill++; recovering++; } break;
                    case 4: if (pilot.defense < pilot.potential) { pilot.defense++; recovering++; } break;
                    case 5: if (pilot.attack < pilot.potential) { pilot.attack++; recovering++; } break;
                    case 6: if (pilot.tireManagement < pilot.potential) { pilot.tireManagement++; recovering++; } break;
                    case 7: if (pilot.fuelManagement < pilot.potential) { pilot.fuelManagement++; recovering++; } break;
                    case 8: if (pilot.concentration < pilot.potential) { pilot.concentration++; recovering++; } break;
                    case 9: if (pilot.adaptability < pilot.potential) { pilot.adaptability++; recovering++; } break;
                }
            }

            if (recovering > 0)
                pilot.CalculateOverall();
        }

        // ══════════════════════════════════════════════════════
        // PILOTO DE RESERVA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Busca un piloto de reserva para sustituir al lesionado.
        /// Prioridad: 1) Reserva del equipo, 2) Regen de F2 (stats -20%)
        /// </summary>
        /// <returns>Piloto sustituto, null si no hay disponible</returns>
        public PilotData FindReservePilot(TeamData team,
            List<PilotData> allPilots)
        {
            // 1. Buscar piloto de reserva del equipo
            var reserve = allPilots.Find(p =>
                p.currentTeamId == team.id &&
                p.role == "Reserve" &&
                !p.isInjured &&
                !p.isRetired);

            if (reserve != null)
                return reserve;

            // 2. Buscar regen de F2 disponible
            var f2Pilots = allPilots.FindAll(p =>
                p.isRegen &&
                string.IsNullOrEmpty(p.currentTeamId) &&
                p.currentCategory == "F2" &&
                !p.isInjured &&
                !p.isRetired &&
                p.isAvailable);

            if (f2Pilots.Count > 0)
            {
                // Elegir al mejor disponible
                f2Pilots.Sort((a, b) => b.overallRating.CompareTo(a.overallRating));
                var f2Sub = f2Pilots[0];

                // Aplicar penalización de stats -20% (desventaja por inexperiencia)
                ApplySubstitutePenalty(f2Sub);

                // Asignar temporalmente al equipo
                f2Sub.currentTeamId = team.id;
                f2Sub.role = "Substitute";
                f2Sub.currentCategory = "F1";

                return f2Sub;
            }

            // 3. Sin reserva disponible
            return null;
        }

        /// <summary>
        /// Libera al piloto sustituto cuando el titular vuelve
        /// </summary>
        public void ReleaseSubstitute(PilotData substitute)
        {
            if (substitute.role != "Substitute") return;

            substitute.currentTeamId = "";
            substitute.role = "JuniorDriver";
            substitute.currentCategory = "F2";
            substitute.isAvailable = true;

            // Revertir penalización (el regen sale con experiencia ganada)
            RevertSubstitutePenalty(substitute);
        }

        // ══════════════════════════════════════════════════════
        // PENALIZACIONES
        // ══════════════════════════════════════════════════════

        private void ApplyReturnPenalty(PilotData pilot, int penalty)
        {
            pilot.speed = Math.Max(20, pilot.speed - penalty);
            pilot.consistency = Math.Max(20, pilot.consistency - penalty);
            pilot.concentration = Math.Max(20, pilot.concentration - penalty);
            pilot.CalculateOverall();
        }

        private void ApplySubstitutePenalty(PilotData pilot)
        {
            float factor = 1f - RESERVE_STAT_PENALTY;
            pilot.speed = (int)(pilot.speed * factor);
            pilot.consistency = (int)(pilot.consistency * factor);
            pilot.rainSkill = (int)(pilot.rainSkill * factor);
            pilot.startSkill = (int)(pilot.startSkill * factor);
            pilot.defense = (int)(pilot.defense * factor);
            pilot.attack = (int)(pilot.attack * factor);
            pilot.tireManagement = (int)(pilot.tireManagement * factor);
            pilot.fuelManagement = (int)(pilot.fuelManagement * factor);
            pilot.concentration = (int)(pilot.concentration * factor);
            pilot.adaptability = (int)(pilot.adaptability * factor);
            pilot.CalculateOverall();
        }

        private void RevertSubstitutePenalty(PilotData pilot)
        {
            // Da un pequeño boost por la experiencia F1 ganada
            pilot.speed += 2;
            pilot.consistency += 1;
            pilot.concentration += 1;
            pilot.adaptability += 3; // La adaptación mejora mucho
            pilot.CalculateOverall();
        }
    }

    /// <summary>Resultado de una recuperación</summary>
    public class RecoveryResult
    {
        public string PilotId;
        public string PilotName;
        public string Severity;
        public bool HasStatPenalty;
        public int RemainingStatPenalty; // Puntos aún por recuperar
        public int RacesToFullRecovery; // Carreras hasta 100%
    }
}
