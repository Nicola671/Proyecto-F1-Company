// ============================================================
// F1 Career Manager — RegulationChecker.cs
// IA de la FIA — Detección de irregularidades
// ============================================================
// DEPENDENCIAS: EventBus.cs, TeamData.cs, ComponentData.cs,
//               Constants.cs
// EVENTOS QUE DISPARA: OnFIAInvestigation
// EJECUTAR: Automáticamente post-carrera
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.AI.FIAAI
{
    /// <summary>
    /// Info de una investigación activa de la FIA
    /// </summary>
    public class FIAInvestigation
    {
        public string InvestigationId;
        public string TeamId;
        public string ComponentId;
        public string Reason;
        public float DetectionProbability;
        public int WeeksToResolve;
        public int CurrentWeek;
        public bool IsResolved;
        public bool WasDetected;
    }

    /// <summary>
    /// Monitorea las regulaciones de todos los equipos.
    /// Detecta ventajas excesivas, componentes ilegales,
    /// y procesa denuncias de otros equipos.
    /// Probabilidades exactas del GDD.
    /// </summary>
    public class RegulationChecker
    {
        // ── Datos ────────────────────────────────────────────
        private EventBus _eventBus;
        private Random _rng;
        private List<FIAInvestigation> _activeInvestigations;
        private Dictionary<string, int> _teamInfractionHistory;

        // ── Constantes ───────────────────────────────────────
        private const float ADVANTAGE_THRESHOLD = 0.08f;  // 8% ventaja desencadena investigación
        private const float RIVAL_DENOUNCE_CHANCE = 0.20f; // 20% de denuncia
        private const float TOP_TEAM_BIAS = 0.10f;         // 10% menos prob para equipos top
        private const float FIA_RANDOM_VARIANCE = 0.15f;   // ±15% variación aleatoria
        private const int INVESTIGATION_WEEKS = 2;          // Semanas para resolver

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public RegulationChecker(Random rng = null)
        {
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
            _activeInvestigations = new List<FIAInvestigation>();
            _teamInfractionHistory = new Dictionary<string, int>();
        }

        // ══════════════════════════════════════════════════════
        // CHEQUEO POST-CARRERA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Monitoreo automático post-carrera.
        /// Chequea todos los equipos buscando irregularidades.
        /// Llamar después de cada carrera.
        /// </summary>
        public List<FIAInvestigation> PostRaceCheck(
            List<TeamData> teams, List<ComponentData> allComponents)
        {
            var newInvestigations = new List<FIAInvestigation>();

            // Calcular rendimiento promedio de la grilla
            float avgPerformance = 0f;
            foreach (var team in teams)
                avgPerformance += team.carPerformance;
            avgPerformance /= teams.Count;

            foreach (var team in teams)
            {
                // 1. Chequear ventaja excesiva sobre el promedio
                float advantage = (team.carPerformance - avgPerformance) / avgPerformance;

                if (advantage > ADVANTAGE_THRESHOLD)
                {
                    // Probabilidad de investigación por ventaja
                    float investigationChance = advantage * 2f;

                    // Aplicar sesgo: equipos top son menos investigados
                    if (team.reputation > 85)
                        investigationChance -= TOP_TEAM_BIAS;

                    // Variación aleatoria de la FIA (no siempre justa)
                    float variance = ((float)_rng.NextDouble() * FIA_RANDOM_VARIANCE * 2f)
                                     - FIA_RANDOM_VARIANCE;
                    investigationChance += variance;

                    if ((float)_rng.NextDouble() < investigationChance)
                    {
                        var inv = CreateInvestigation(team.id, null,
                            $"Ventaja excesiva detectada ({advantage * 100:F1}% sobre promedio)");
                        newInvestigations.Add(inv);
                    }
                }

                // 2. Chequear componentes instalados
                var teamComponents = allComponents.FindAll(c =>
                    c.ownerTeamId == team.id && c.hasBeenInstalled && !c.isBanned);

                foreach (var comp in teamComponents)
                {
                    if (comp.legality == "Legal" || comp.hasBeenInvestigated)
                        continue;

                    float detectionChance = GetDetectionChance(comp.legality, team);

                    if ((float)_rng.NextDouble() < detectionChance)
                    {
                        var inv = CreateInvestigation(team.id, comp.id,
                            $"Componente sospechoso: {comp.name} ({comp.legality})");
                        newInvestigations.Add(inv);

                        comp.hasBeenInvestigated = true;
                    }
                }
            }

            // 3. Procesar denuncias de equipos rivales
            var denounceInvestigations = ProcessRivalDenouncements(teams, allComponents);
            newInvestigations.AddRange(denounceInvestigations);

            return newInvestigations;
        }

        // ══════════════════════════════════════════════════════
        // DENUNCIAS DE RIVALES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Los equipos rivales pueden denunciar componentes sospechosos.
        /// 20% de chance de denuncia si detectan ventaja.
        /// </summary>
        private List<FIAInvestigation> ProcessRivalDenouncements(
            List<TeamData> teams, List<ComponentData> allComponents)
        {
            var investigations = new List<FIAInvestigation>();

            foreach (var team in teams)
            {
                // Cada equipo puede denunciar a uno de sus rivales
                foreach (var rival in teams)
                {
                    if (team.id == rival.id) continue;

                    // ¿El rival tiene ventaja sobre este equipo?
                    if (rival.carPerformance <= team.carPerformance + 5) continue;

                    // ¿Decide denunciar?
                    if ((float)_rng.NextDouble() >= RIVAL_DENOUNCE_CHANCE) continue;

                    // Buscar componente sospechoso del rival
                    var suspectComponents = allComponents.FindAll(c =>
                        c.ownerTeamId == rival.id &&
                        c.hasBeenInstalled &&
                        !c.isBanned &&
                        c.legality != "Legal" &&
                        !c.hasBeenInvestigated);

                    if (suspectComponents.Count > 0)
                    {
                        var target = suspectComponents[
                            _rng.Next(suspectComponents.Count)];

                        // La denuncia aumenta la probabilidad de detección
                        float boostedChance = target.GetDetectionChance() * 1.3f;

                        if ((float)_rng.NextDouble() < boostedChance)
                        {
                            var inv = CreateInvestigation(rival.id, target.id,
                                $"Denuncia de {team.shortName}: componente {target.name} sospechoso");
                            investigations.Add(inv);
                            target.hasBeenInvestigated = true;
                        }
                    }
                }
            }

            return investigations;
        }

        // ══════════════════════════════════════════════════════
        // RESOLUCIÓN DE INVESTIGACIONES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Avanza una semana en todas las investigaciones activas.
        /// Las que se resuelven disparan eventos.
        /// </summary>
        /// <returns>Lista de investigaciones resueltas esta semana</returns>
        public List<FIAInvestigation> AdvanceWeek(List<ComponentData> allComponents)
        {
            var resolved = new List<FIAInvestigation>();

            foreach (var inv in _activeInvestigations)
            {
                if (inv.IsResolved) continue;

                inv.CurrentWeek++;

                if (inv.CurrentWeek >= inv.WeeksToResolve)
                {
                    // Resolver investigación
                    inv.IsResolved = true;

                    // ¿Se detectó la irregularidad?
                    inv.WasDetected = (float)_rng.NextDouble() < inv.DetectionProbability;

                    if (inv.WasDetected && inv.ComponentId != null)
                    {
                        // Marcar componente como bando
                        var comp = allComponents.Find(c => c.id == inv.ComponentId);
                        if (comp != null)
                        {
                            comp.isBanned = true;
                            comp.status = "Banned";
                        }

                        // Incrementar historial de infracciones
                        if (!_teamInfractionHistory.ContainsKey(inv.TeamId))
                            _teamInfractionHistory[inv.TeamId] = 0;
                        _teamInfractionHistory[inv.TeamId]++;
                    }

                    // Disparar evento
                    _eventBus.FireFIAInvestigation(new EventBus.FIAInvestigationArgs
                    {
                        TeamId = inv.TeamId,
                        ComponentId = inv.ComponentId ?? "",
                        InvestigationReason = inv.Reason,
                        WasDetected = inv.WasDetected,
                        SanctionType = inv.WasDetected ? "Pending" : "Cleared",
                        FineAmount = 0f, // SanctionSystem se encarga
                        PointsPenalty = 0,
                        Description = inv.WasDetected
                            ? $"La FIA encontró irregularidades: {inv.Reason}"
                            : $"La FIA no encontró evidencia: {inv.Reason}"
                    });

                    resolved.Add(inv);
                }
            }

            // Limpiar resueltas
            _activeInvestigations.RemoveAll(i => i.IsResolved);

            return resolved;
        }

        // ══════════════════════════════════════════════════════
        // CÁLCULO DE PROBABILIDADES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Calcula la probabilidad de detección según legalidad.
        /// Exactamente como define el GDD con ajustes por sesgo.
        /// </summary>
        private float GetDetectionChance(string legality, TeamData team)
        {
            float baseChance;
            switch (legality)
            {
                case "Illegal":
                    baseChance = Constants.FIA_DETECT_ILLEGAL; // 85%
                    break;
                case "GreyAggressive":
                    // 50-65%, tomo promedio
                    baseChance = Constants.FIA_DETECT_GREY_AGGRESSIVE; // 57.5%
                    break;
                case "GreySubtle":
                    // 25-40%, tomo promedio
                    baseChance = Constants.FIA_DETECT_GREY_SUBTLE; // 32.5%
                    break;
                default:
                    baseChance = Constants.FIA_DETECT_SUSPICIOUS; // 22.5%
                    break;
            }

            // Sesgo: equipos top tienen 10% menos probabilidad de sanción
            if (team.reputation > 85)
                baseChance *= (1f - TOP_TEAM_BIAS);

            // Variación aleatoria ±15% (la FIA no siempre es justa)
            float variance = ((float)_rng.NextDouble() * FIA_RANDOM_VARIANCE * 2f)
                             - FIA_RANDOM_VARIANCE;
            baseChance += variance;

            // Historial agrava: +5% por infracción previa
            if (_teamInfractionHistory.ContainsKey(team.id))
                baseChance += _teamInfractionHistory[team.id] * 0.05f;

            return Math.Max(0f, Math.Min(1f, baseChance));
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private FIAInvestigation CreateInvestigation(string teamId,
            string componentId, string reason)
        {
            float detectionProb = 0.50f;

            var inv = new FIAInvestigation
            {
                InvestigationId = $"fia_inv_{_rng.Next(100000)}",
                TeamId = teamId,
                ComponentId = componentId,
                Reason = reason,
                DetectionProbability = detectionProb,
                WeeksToResolve = INVESTIGATION_WEEKS,
                CurrentWeek = 0,
                IsResolved = false,
                WasDetected = false
            };

            _activeInvestigations.Add(inv);
            return inv;
        }

        /// <summary>Obtiene investigaciones activas de un equipo</summary>
        public List<FIAInvestigation> GetActiveInvestigations(string teamId)
        {
            return _activeInvestigations.FindAll(i =>
                i.TeamId == teamId && !i.IsResolved);
        }

        /// <summary>Obtiene historial de infracciones de un equipo</summary>
        public int GetInfractionCount(string teamId)
        {
            if (_teamInfractionHistory.ContainsKey(teamId))
                return _teamInfractionHistory[teamId];
            return 0;
        }
    }
}
