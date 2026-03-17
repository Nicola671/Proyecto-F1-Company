// ============================================================
// F1 Career Manager — InjuryManager.cs
// Sistema de lesiones — probabilidades del GDD
// ============================================================
// DEPENDENCIAS: EventBus.cs, PilotData.cs, Constants.cs
// EVENTOS QUE DISPARA: OnInjuryOccurred
// CONECTA CON: RaceSimulator (post-choque)
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.Injury
{
    /// <summary>Registro de una lesión activa</summary>
    public class InjuryRecord
    {
        public string InjuryId;
        public string PilotId;
        public string PilotName;
        public string Severity;        // "Light", "Moderate", "Severe", "CareerEnding"
        public string Description;
        public int RacesOut;           // Carreras que se pierde
        public int RacesRemaining;     // Carreras restantes de recuperación
        public bool AffectsStats;      // Si reduce stats al volver
        public int StatPenalty;        // Puntos que pierde temporalmente
        public bool AffectsPotential;  // Si reduce potencial permanentemente
        public int PotentialLoss;      // Cuánto pierde de potencial
        public bool IsRecovered;
    }

    /// <summary>
    /// Gestiona las lesiones de los pilotos.
    /// Probabilidades exactas del GDD:
    /// Leve 8%, Moderada 3%, Grave 0.8%, Carrera terminada 0.1%
    /// Fatiga: +2% si 10+ carreras seguidas.
    /// </summary>
    public class InjuryManager
    {
        // ── Datos ────────────────────────────────────────────
        private EventBus _eventBus;
        private Random _rng;
        private List<InjuryRecord> _activeInjuries;
        private Dictionary<string, int> _consecutiveRaces; // Contador de fatiga

        // ── Probabilidades del GDD ───────────────────────────
        private const float INJURY_LIGHT_CHANCE = 0.08f;        // 8% por choque
        private const float INJURY_MODERATE_CHANCE = 0.03f;     // 3% por choque
        private const float INJURY_SEVERE_CHANCE = 0.008f;      // 0.8% por choque
        private const float INJURY_CAREER_END_CHANCE = 0.001f;  // 0.1% por choque

        // ── Fatiga ───────────────────────────────────────────
        private const int FATIGUE_THRESHOLD = 10;               // 10 carreras seguidas
        private const float FATIGUE_BONUS_INJURY = 0.02f;       // +2% extra

        // ── Duraciones ───────────────────────────────────────
        private const int LIGHT_MIN_RACES = 1;
        private const int LIGHT_MAX_RACES = 2;
        private const int MODERATE_MIN_RACES = 3;
        private const int MODERATE_MAX_RACES = 6;
        private const int SEVERE_MIN_RACES = 8;
        private const int SEVERE_MAX_RACES = 24;  // Puede ser toda la temporada

        // ── Stats afectados ──────────────────────────────────
        private const int MODERATE_STAT_PENALTY = 5;   // -5 stats al volver
        private const int SEVERE_STAT_PENALTY = 10;    // -10 stats al volver
        private const int SEVERE_POTENTIAL_LOSS = 3;   // -3 potencial permanente

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public InjuryManager(Random rng = null)
        {
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
            _activeInjuries = new List<InjuryRecord>();
            _consecutiveRaces = new Dictionary<string, int>();
        }

        // ══════════════════════════════════════════════════════
        // EVALUACIÓN POST-CHOQUE
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Evalúa si un piloto se lesiona después de un choque.
        /// Llamar desde RaceSimulator cuando hay DNF por choque/crash.
        /// </summary>
        /// <param name="pilot">Piloto involucrado</param>
        /// <param name="crashSeverity">Severidad del choque 0-1 (0=toque, 1=impacto fuerte)</param>
        /// <returns>InjuryRecord si hubo lesión, null si no</returns>
        public InjuryRecord EvaluateCrashInjury(PilotData pilot, float crashSeverity)
        {
            // Modificador de severidad del choque
            float severityMod = 0.5f + crashSeverity * 1.5f;

            // Bonus por fatiga
            float fatigueMod = 0f;
            if (_consecutiveRaces.ContainsKey(pilot.id) &&
                _consecutiveRaces[pilot.id] >= FATIGUE_THRESHOLD)
            {
                fatigueMod = FATIGUE_BONUS_INJURY;
            }

            // Evaluar de más grave a menos grave
            float roll = (float)_rng.NextDouble();

            if (roll < (INJURY_CAREER_END_CHANCE * severityMod))
            {
                return ApplyInjury(pilot, "CareerEnding", 999);
            }

            if (roll < ((INJURY_CAREER_END_CHANCE + INJURY_SEVERE_CHANCE) * severityMod))
            {
                int races = _rng.Next(SEVERE_MIN_RACES, SEVERE_MAX_RACES + 1);
                return ApplyInjury(pilot, "Severe", races);
            }

            if (roll < ((INJURY_CAREER_END_CHANCE + INJURY_SEVERE_CHANCE +
                          INJURY_MODERATE_CHANCE) * severityMod + fatigueMod * 0.5f))
            {
                int races = _rng.Next(MODERATE_MIN_RACES, MODERATE_MAX_RACES + 1);
                return ApplyInjury(pilot, "Moderate", races);
            }

            if (roll < ((INJURY_CAREER_END_CHANCE + INJURY_SEVERE_CHANCE +
                          INJURY_MODERATE_CHANCE + INJURY_LIGHT_CHANCE) *
                          severityMod + fatigueMod))
            {
                int races = _rng.Next(LIGHT_MIN_RACES, LIGHT_MAX_RACES + 1);
                return ApplyInjury(pilot, "Light", races);
            }

            // Sin lesión
            return null;
        }

        /// <summary>
        /// Evalúa lesión leve por fatiga (sin choque, por estrés acumulado).
        /// Llamar entre carreras si el piloto tiene muchas seguidas.
        /// </summary>
        public InjuryRecord EvaluateFatigueInjury(PilotData pilot)
        {
            if (!_consecutiveRaces.ContainsKey(pilot.id)) return null;
            if (_consecutiveRaces[pilot.id] < FATIGUE_THRESHOLD) return null;

            float chance = FATIGUE_BONUS_INJURY *
                (_consecutiveRaces[pilot.id] - FATIGUE_THRESHOLD + 1);

            if ((float)_rng.NextDouble() < chance)
            {
                return ApplyInjury(pilot, "Light", 1);
            }

            return null;
        }

        // ══════════════════════════════════════════════════════
        // APLICAR LESIÓN
        // ══════════════════════════════════════════════════════

        private InjuryRecord ApplyInjury(PilotData pilot, string severity, int racesOut)
        {
            var injury = new InjuryRecord
            {
                InjuryId = $"inj_{_rng.Next(100000)}",
                PilotId = pilot.id,
                PilotName = $"{pilot.firstName} {pilot.lastName}",
                Severity = severity,
                RacesOut = racesOut,
                RacesRemaining = racesOut,
                IsRecovered = false
            };

            switch (severity)
            {
                case "Light":
                    injury.Description = GenerateLightInjuryDescription();
                    injury.AffectsStats = false;
                    injury.StatPenalty = 0;
                    injury.AffectsPotential = false;
                    break;

                case "Moderate":
                    injury.Description = GenerateModerateInjuryDescription();
                    injury.AffectsStats = true;
                    injury.StatPenalty = MODERATE_STAT_PENALTY;
                    injury.AffectsPotential = false;
                    break;

                case "Severe":
                    injury.Description = GenerateSevereInjuryDescription();
                    injury.AffectsStats = true;
                    injury.StatPenalty = SEVERE_STAT_PENALTY;
                    injury.AffectsPotential = true;
                    injury.PotentialLoss = SEVERE_POTENTIAL_LOSS;
                    break;

                case "CareerEnding":
                    injury.Description = "Lesión catastrófica — retiro forzoso";
                    injury.AffectsStats = true;
                    injury.StatPenalty = 0;
                    injury.AffectsPotential = true;
                    injury.PotentialLoss = 0;
                    break;
            }

            // Aplicar al piloto
            pilot.isInjured = true;
            pilot.racesUntilRecovery = racesOut;
            pilot.injurySeverity = severity;

            if (severity == "CareerEnding")
            {
                pilot.isRetired = true;
                pilot.isAvailable = false;
            }

            if (severity == "Severe" && injury.AffectsPotential)
            {
                pilot.potential = Math.Max(20, pilot.potential - injury.PotentialLoss);
            }

            _activeInjuries.Add(injury);

            // Reset fatiga
            if (_consecutiveRaces.ContainsKey(pilot.id))
                _consecutiveRaces[pilot.id] = 0;

            // Disparar evento
            _eventBus.FireInjuryOccurred(new EventBus.InjuryOccurredArgs
            {
                PilotId = pilot.id,
                PilotName = injury.PilotName,
                Severity = severity,
                RacesOut = racesOut,
                Description = injury.Description,
                AffectsPotential = injury.AffectsPotential
            });

            return injury;
        }

        // ══════════════════════════════════════════════════════
        // TRACKING DE FATIGA
        // ══════════════════════════════════════════════════════

        /// <summary>Registra que un piloto corrió una carrera (para fatiga)</summary>
        public void RegisterRaceParticipation(string pilotId)
        {
            if (!_consecutiveRaces.ContainsKey(pilotId))
                _consecutiveRaces[pilotId] = 0;
            _consecutiveRaces[pilotId]++;
        }

        /// <summary>Resetea el contador de fatiga (descanso)</summary>
        public void ResetFatigue(string pilotId)
        {
            if (_consecutiveRaces.ContainsKey(pilotId))
                _consecutiveRaces[pilotId] = 0;
        }

        /// <summary>Obtiene carreras consecutivas de un piloto</summary>
        public int GetConsecutiveRaces(string pilotId)
        {
            return _consecutiveRaces.ContainsKey(pilotId)
                ? _consecutiveRaces[pilotId] : 0;
        }

        // ══════════════════════════════════════════════════════
        // CONSULTAS
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene lesiones activas de un piloto</summary>
        public InjuryRecord GetActiveInjury(string pilotId)
        {
            return _activeInjuries.Find(i =>
                i.PilotId == pilotId && !i.IsRecovered);
        }

        /// <summary>¿Está lesionado?</summary>
        public bool IsInjured(string pilotId)
        {
            return GetActiveInjury(pilotId) != null;
        }

        /// <summary>Obtiene todas las lesiones activas</summary>
        public List<InjuryRecord> GetAllActiveInjuries()
        {
            return _activeInjuries.FindAll(i => !i.IsRecovered);
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN DE DESCRIPCIONES
        // ══════════════════════════════════════════════════════

        private string GenerateLightInjuryDescription()
        {
            string[] descriptions = {
                "Contusión en las costillas",
                "Dolor cervical leve",
                "Esguince de muñeca",
                "Golpe en la rodilla",
                "Dolor de espalda por impacto",
                "Contractura muscular por G-forces",
                "Hematoma en el hombro"
            };
            return descriptions[_rng.Next(descriptions.Length)];
        }

        private string GenerateModerateInjuryDescription()
        {
            string[] descriptions = {
                "Fractura menor en la mano",
                "Esguince severo de tobillo",
                "Fisura de costilla",
                "Lesión en los ligamentos de la rodilla",
                "Conmoción cerebral leve",
                "Fractura en el metacarpo",
                "Desgarro muscular en el cuello"
            };
            return descriptions[_rng.Next(descriptions.Length)];
        }

        private string GenerateSevereInjuryDescription()
        {
            string[] descriptions = {
                "Fractura múltiple en las piernas",
                "Lesión espinal — requiere cirugía",
                "Fractura de pelvis",
                "Conmoción cerebral severa",
                "Fractura de clavícula y escápula",
                "Daño severo en los ligamentos de ambas rodillas"
            };
            return descriptions[_rng.Next(descriptions.Length)];
        }
    }
}
