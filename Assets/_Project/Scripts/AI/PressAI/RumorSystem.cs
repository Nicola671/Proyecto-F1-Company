// ============================================================
// F1 Career Manager — RumorSystem.cs
// Sistema de rumores — 60% reales, 40% falsos
// ============================================================
// DEPENDENCIAS: EventBus.cs, NewsGenerator.cs, StaffManager.cs,
//               PilotData.cs, TeamData.cs
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;
using F1CareerManager.Staff;

namespace F1CareerManager.AI.PressAI
{
    /// <summary>
    /// Un rumor activo en el juego
    /// </summary>
    public class ActiveRumor
    {
        public string RumorId;
        public string Headline;
        public string Body;
        public string Source;          // Medio que lo publica
        public float Confidence;       // % de confianza visible al jugador (30-90%)
        public bool HasRealBasis;      // 60% verdadero, 40% falso
        public bool IsConfirmed;       // Si se confirmó como verdadero
        public bool IsDenied;          // Si se desmintió
        public bool IsExpired;         // Si ya expiró
        public int WeeksRemaining;     // Semanas antes de expirar
        public int MoodEffectOnMentioned; // Efecto en humor
        public List<string> RelatedPilotIds;
        public List<string> RelatedTeamIds;
        public string PlantedByTeamId; // Si un rival lo plantó (vacío si no)
    }

    /// <summary>
    /// Gestiona rumores del paddock.
    /// 60% tienen base real (reflejan estado del juego).
    /// 40% completamente falsos pero afectan igual.
    /// Rivales pueden plantar rumores para desestabilizar.
    /// Jefe de Comunicaciones reduce rumores negativos -30%.
    /// </summary>
    public class RumorSystem
    {
        // ── Datos ────────────────────────────────────────────
        private EventBus _eventBus;
        private StaffManager _staffManager;
        private Random _rng;
        private List<ActiveRumor> _activeRumors;

        // ── Constantes ───────────────────────────────────────
        private const float REAL_RUMOR_CHANCE = 0.60f;       // 60% base real
        private const float RIVAL_PLANT_CHANCE = 0.08f;      // 8% por semana
        private const float COMMS_REDUCTION = 0.30f;         // -30% con Jefe Comms
        private const int DEFAULT_RUMOR_DURATION = 4;        // 4 semanas
        private const float RUMOR_WEEKLY_CHANCE = 0.20f;     // 20% de rumor por semana

        // ── Plantillas de rumores basados en estado real ─────
        private static readonly string[] REAL_TRANSFER_RUMORS = {
            "Fuentes confirman: {0} está insatisfecho en {1}",
            "Contactos entre el agente de {0} y {1} rival",
            "Cláusula de salida de {0} podría activarse pronto",
            "{0} no renovaría: su contrato vence esta temporada",
            "Reunión secreta entre {0} y directivos de {1}"
        };

        private static readonly string[] REAL_PERFORMANCE_RUMORS = {
            "Fuentes internas: {0} está rindiendo por debajo en los entrenamientos",
            "El equipo {0} estaría considerando cambios en la alineación",
            "Problemas de fiabilidad ocultos: {0} tiene 3 fallos no reportados",
            "Según datos filtrados, {0} perdió rendimiento en las últimas actualizaciones"
        };

        private static readonly string[] FAKE_RUMORS = {
            "¿{0} a {1}? La noticia que sacude el paddock (FALSO)",
            "Se rumorea un cambio radical en {0}, pero no hay evidencia",
            "Periodista inventa drama: {0} no tiene problemas internos",
            "Rumor infundado: {0} NO estaría considerando retirarse",
            "Fuentes 'cercanas' afirman cambios que nadie en el equipo confirma",
            "Chisme sin base: supuesta pelea entre pilotos de {0}",
            "Información falsa circula: {0} niega problemas de presupuesto",
            "Tabloides inventan crisis donde no la hay en {0}"
        };

        private static readonly string[] RIVAL_PLANTED_RUMORS = {
            "Filtración: {0} podría perder su sponsor principal",
            "¿Problemas legales para {0}? Fuentes anónimas sugieren investigación",
            "Clima tenso en {0}: se habla de despidos inminentes",
            "Personal de {0} insatisfecho: amenaza de huelga interna",
            "El auto de {0} tendría un defecto de diseño fundamental"
        };

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public RumorSystem(StaffManager staffManager, Random rng = null)
        {
            _staffManager = staffManager;
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
            _activeRumors = new List<ActiveRumor>();
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN SEMANAL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera rumores para esta semana.
        /// Aplicar reducción del Jefe de Comunicaciones automáticamente.
        /// </summary>
        public List<ActiveRumor> GenerateWeeklyRumors(
            List<PilotData> allPilots, List<TeamData> allTeams,
            string playerTeamId)
        {
            var newRumors = new List<ActiveRumor>();

            // ¿Hay rumor esta semana?
            if ((float)_rng.NextDouble() >= RUMOR_WEEKLY_CHANCE) return newRumors;

            // Generar rumor
            bool isReal = (float)_rng.NextDouble() < REAL_RUMOR_CHANCE;

            ActiveRumor rumor;
            if (isReal)
            {
                rumor = GenerateRealBasedRumor(allPilots, allTeams);
            }
            else
            {
                rumor = GenerateFakeRumor(allTeams);
            }

            if (rumor == null) return newRumors;

            // Verificar si el Jefe de Comunicaciones reduce este rumor
            if (rumor.RelatedTeamIds.Contains(playerTeamId))
            {
                float commsBonus = _staffManager.GetCommsRumorReduction(playerTeamId);
                if (commsBonus > 0 && rumor.MoodEffectOnMentioned < 0)
                {
                    // Reducir efecto negativo
                    float reduction = COMMS_REDUCTION + commsBonus * 0.5f;
                    rumor.MoodEffectOnMentioned = (int)(rumor.MoodEffectOnMentioned *
                        (1f - reduction));

                    // Posibilidad de bloquear completamente
                    if ((float)_rng.NextDouble() < reduction)
                    {
                        return newRumors; // Rumor bloqueado
                    }
                }
            }

            _activeRumors.Add(rumor);
            newRumors.Add(rumor);

            return newRumors;
        }

        /// <summary>
        /// Rival planta un rumor contra el equipo del jugador.
        /// 8% de chance por semana en dificultad normal.
        /// </summary>
        public ActiveRumor TryPlantRivalRumor(string playerTeamId,
            List<TeamData> allTeams, string difficulty)
        {
            float plantChance = RIVAL_PLANT_CHANCE;
            if (difficulty == "Hard") plantChance *= 1.5f;
            if (difficulty == "Legend") plantChance *= 2.0f;

            if ((float)_rng.NextDouble() >= plantChance) return null;

            // Encontrar un equipo rival que plante
            var rivals = allTeams.FindAll(t => t.id != playerTeamId);
            if (rivals.Count == 0) return null;
            var rival = rivals[_rng.Next(rivals.Count)];

            var playerTeam = allTeams.Find(t => t.id == playerTeamId);
            string teamName = playerTeam?.shortName ?? "Tu equipo";

            string template = RIVAL_PLANTED_RUMORS[
                _rng.Next(RIVAL_PLANTED_RUMORS.Length)];
            string headline = template.Replace("{0}", teamName);

            var rumor = new ActiveRumor
            {
                RumorId = $"rumor_plant_{_rng.Next(100000)}",
                Headline = headline,
                Body = $"Fuentes no confirmadas sugieren problemas en {teamName}. " +
                    "La información no ha sido verificada oficialmente.",
                Source = "Paddock Rumors",
                Confidence = 30 + _rng.Next(0, 30),
                HasRealBasis = false,
                IsConfirmed = false,
                IsDenied = false,
                IsExpired = false,
                WeeksRemaining = DEFAULT_RUMOR_DURATION,
                MoodEffectOnMentioned = -5 - _rng.Next(0, 6),
                RelatedPilotIds = new List<string>(),
                RelatedTeamIds = new List<string> { playerTeamId },
                PlantedByTeamId = rival.id
            };

            // Verificar Jefe de Comunicaciones
            float commsBonus = _staffManager.GetCommsRumorReduction(playerTeamId);
            if (commsBonus > 0 && (float)_rng.NextDouble() < COMMS_REDUCTION + commsBonus)
            {
                return null; // Bloqueado
            }

            _activeRumors.Add(rumor);
            return rumor;
        }

        // ══════════════════════════════════════════════════════
        // GENERADORES INTERNOS
        // ══════════════════════════════════════════════════════

        private ActiveRumor GenerateRealBasedRumor(List<PilotData> pilots,
            List<TeamData> teams)
        {
            // Buscar situación real para basar el rumor
            // Pilotos insatisfechos, contratos por vencer, etc.
            var unhappyPilots = pilots.FindAll(p =>
                p.mood == "Furious" || p.mood == "WantsOut" ||
                p.contractYearsLeft <= 1);

            if (unhappyPilots.Count > 0 && (float)_rng.NextDouble() < 0.60f)
            {
                var pilot = unhappyPilots[_rng.Next(unhappyPilots.Count)];
                var dstTeam = teams[_rng.Next(teams.Count)];
                while (dstTeam.id == pilot.currentTeamId && teams.Count > 1)
                    dstTeam = teams[_rng.Next(teams.Count)];

                string template = REAL_TRANSFER_RUMORS[
                    _rng.Next(REAL_TRANSFER_RUMORS.Length)];
                string headline = template
                    .Replace("{0}", pilot.lastName)
                    .Replace("{1}", dstTeam.shortName);

                return new ActiveRumor
                {
                    RumorId = $"rumor_real_{_rng.Next(100000)}",
                    Headline = headline,
                    Body = $"El descontento de {pilot.firstName} {pilot.lastName} " +
                        $"con su situación actual podría llevar a un cambio de equipo.",
                    Source = "Formula Insider",
                    Confidence = 50 + _rng.Next(0, 35),
                    HasRealBasis = true,
                    IsConfirmed = false,
                    IsDenied = false,
                    IsExpired = false,
                    WeeksRemaining = DEFAULT_RUMOR_DURATION + _rng.Next(0, 3),
                    MoodEffectOnMentioned = -3 - _rng.Next(0, 5),
                    RelatedPilotIds = new List<string> { pilot.id },
                    RelatedTeamIds = new List<string>
                        { pilot.currentTeamId ?? "", dstTeam.id },
                    PlantedByTeamId = ""
                };
            }

            // Rumor basado en rendimiento
            var team = teams[_rng.Next(teams.Count)];
            string perfTemplate = REAL_PERFORMANCE_RUMORS[
                _rng.Next(REAL_PERFORMANCE_RUMORS.Length)];
            string perfHeadline = perfTemplate.Replace("{0}", team.shortName);

            return new ActiveRumor
            {
                RumorId = $"rumor_perf_{_rng.Next(100000)}",
                Headline = perfHeadline,
                Body = $"Información no confirmada sobre el rendimiento de {team.shortName}.",
                Source = "Paddock Rumors",
                Confidence = 40 + _rng.Next(0, 25),
                HasRealBasis = true,
                WeeksRemaining = DEFAULT_RUMOR_DURATION,
                MoodEffectOnMentioned = -2,
                RelatedPilotIds = new List<string>(),
                RelatedTeamIds = new List<string> { team.id },
                PlantedByTeamId = ""
            };
        }

        private ActiveRumor GenerateFakeRumor(List<TeamData> teams)
        {
            var team = teams[_rng.Next(teams.Count)];
            string template = FAKE_RUMORS[_rng.Next(FAKE_RUMORS.Length)];
            string headline = template.Replace("{0}", team.shortName)
                .Replace("{1}", teams[_rng.Next(teams.Count)].shortName);

            return new ActiveRumor
            {
                RumorId = $"rumor_fake_{_rng.Next(100000)}",
                Headline = headline,
                Body = $"Este rumor no tiene fundamento real pero circula por el paddock.",
                Source = "Formula Insider",
                Confidence = 20 + _rng.Next(0, 30),
                HasRealBasis = false,
                IsConfirmed = false,
                IsDenied = false,
                IsExpired = false,
                WeeksRemaining = _rng.Next(2, DEFAULT_RUMOR_DURATION + 1),
                MoodEffectOnMentioned = -2 - _rng.Next(0, 4),
                RelatedPilotIds = new List<string>(),
                RelatedTeamIds = new List<string> { team.id },
                PlantedByTeamId = ""
            };
        }

        // ══════════════════════════════════════════════════════
        // AVANCE SEMANAL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Avanza una semana: reduce duración y expira rumores viejos.
        /// Los rumores reales pueden confirmarse, los falsos desmentirse.
        /// </summary>
        public List<ActiveRumor> AdvanceWeek()
        {
            var resolved = new List<ActiveRumor>();

            foreach (var rumor in _activeRumors)
            {
                if (rumor.IsExpired) continue;

                rumor.WeeksRemaining--;

                if (rumor.WeeksRemaining <= 0)
                {
                    // Expirar o resolver
                    if (rumor.HasRealBasis && (float)_rng.NextDouble() < 0.40f)
                    {
                        rumor.IsConfirmed = true;
                        rumor.IsExpired = true;
                    }
                    else
                    {
                        rumor.IsDenied = !rumor.HasRealBasis;
                        rumor.IsExpired = true;
                    }
                    resolved.Add(rumor);
                }
            }

            // Limpiar expirados antiguos
            _activeRumors.RemoveAll(r => r.IsExpired && r.WeeksRemaining < -2);

            return resolved;
        }

        // ══════════════════════════════════════════════════════
        // CONSULTAS
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene rumores activos (no expirados)</summary>
        public List<ActiveRumor> GetActiveRumors()
        {
            return _activeRumors.FindAll(r => !r.IsExpired);
        }

        /// <summary>Obtiene rumores sobre un equipo</summary>
        public List<ActiveRumor> GetTeamRumors(string teamId)
        {
            return _activeRumors.FindAll(r =>
                !r.IsExpired && r.RelatedTeamIds.Contains(teamId));
        }

        /// <summary>Obtiene rumores con efecto negativo activo</summary>
        public List<ActiveRumor> GetNegativeRumors()
        {
            return _activeRumors.FindAll(r =>
                !r.IsExpired && r.MoodEffectOnMentioned < 0);
        }
    }
}
