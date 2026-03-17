// ============================================================
// F1 Career Manager — ObjectiveManager.cs
// Genera y evalúa objetivos por temporada
// ============================================================
// DEPENDENCIAS: TeamData.cs, Constants.cs, EventBus.cs
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Data;

namespace F1CareerManager.Core
{
    /// <summary>
    /// Al inicio de temporada genera 3 objetivos (Mínimo/Estándar/Élite).
    /// Al final evalúa y aplica consecuencias.
    /// 2 temporadas sin mínimo → presión máxima de la junta.
    /// </summary>
    public class ObjectiveManager
    {
        // ── Estado ───────────────────────────────────────────
        private GameManager _gm;
        private EventBus _eventBus;
        private List<SeasonObjective> _currentObjectives;
        private int _consecutiveFailures;
        private bool _boardPressureActive;

        // Acceso público
        public IReadOnlyList<SeasonObjective> CurrentObjectives
            => _currentObjectives?.AsReadOnly();
        public int ConsecutiveFailures => _consecutiveFailures;
        public bool BoardPressure => _boardPressureActive;

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public ObjectiveManager(GameManager gm)
        {
            _gm = gm;
            _eventBus = EventBus.Instance;
            _currentObjectives = new List<SeasonObjective>();
            _consecutiveFailures = 0;
            _boardPressureActive = false;
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN DE OBJETIVOS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera 3 objetivos para la temporada según el equipo.
        /// Calibrados por nivel: top teams esperan campeonatos,
        /// equipos bajos solo piden sumar puntos.
        /// </summary>
        public void GenerateObjectives(TeamData team)
        {
            _currentObjectives.Clear();
            if (team == null) return;

            // Determinar tier del equipo por reputación/budget
            TeamTier tier = ClassifyTeam(team);

            switch (tier)
            {
                case TeamTier.Top:
                    // Ferrari, Red Bull, McLaren, Mercedes
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Minimum,
                        Description = "Terminar en el Top 3 de constructores",
                        TargetPosition = 3,
                        RewardMoney = 0f,
                        FailConsequence = "Presión de la junta directiva"
                    });
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Standard,
                        Description = "Ganar el campeonato de constructores",
                        TargetPosition = 1,
                        RewardMoney = 5f,
                        FailConsequence = ""
                    });
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Elite,
                        Description = "Ganar ambos campeonatos (constructores + piloto)",
                        TargetPosition = 1,
                        RequiresBothChampionships = true,
                        RewardMoney = 15f,
                        FailConsequence = ""
                    });
                    break;

                case TeamTier.MidHigh:
                    // Aston Martin, Alpine
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Minimum,
                        Description = "Terminar en el Top 5 de constructores",
                        TargetPosition = 5,
                        RewardMoney = 0f,
                        FailConsequence = "Presión de la junta directiva"
                    });
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Standard,
                        Description = "Terminar en el Top 3 de constructores",
                        TargetPosition = 3,
                        RewardMoney = 5f,
                        FailConsequence = ""
                    });
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Elite,
                        Description = "Ganar una carrera y estar en el Top 2",
                        TargetPosition = 2,
                        RequiresWin = true,
                        RewardMoney = 10f,
                        FailConsequence = ""
                    });
                    break;

                case TeamTier.Mid:
                    // RB, Haas
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Minimum,
                        Description = "Terminar en el Top 7 de constructores",
                        TargetPosition = 7,
                        RewardMoney = 0f,
                        FailConsequence = "Presión de la junta directiva"
                    });
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Standard,
                        Description = "Terminar en el Top 5 de constructores",
                        TargetPosition = 5,
                        RewardMoney = 3f,
                        FailConsequence = ""
                    });
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Elite,
                        Description = "Conseguir una victoria o podio regular",
                        TargetPosition = 4,
                        RequiresPodium = true,
                        RewardMoney = 8f,
                        FailConsequence = ""
                    });
                    break;

                case TeamTier.Low:
                    // Williams, Sauber/Kick
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Minimum,
                        Description = "Sumar al menos 10 puntos en el campeonato",
                        TargetPoints = 10,
                        TargetPosition = 10,
                        RewardMoney = 0f,
                        FailConsequence = "Recorte de presupuesto -10%"
                    });
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Standard,
                        Description = "Terminar en el Top 8 de constructores",
                        TargetPosition = 8,
                        RewardMoney = 3f,
                        FailConsequence = ""
                    });
                    _currentObjectives.Add(new SeasonObjective
                    {
                        Level = ObjectiveLevel.Elite,
                        Description = "Terminar en el Top 5 — sorprender a todos",
                        TargetPosition = 5,
                        RewardMoney = 10f,
                        FailConsequence = ""
                    });
                    break;
            }

            // Actualizar campos ocultos en TeamData
            if (team != null && _currentObjectives.Count >= 3)
            {
                team.objectiveMinTarget = _currentObjectives[0].TargetPosition;
                team.objectiveStdTarget = _currentObjectives[1].TargetPosition;
                team.objectiveEliteTarget = _currentObjectives[2].TargetPosition;
            }

            Log($"Objetivos generados para {team.fullName} (Tier: {tier}):");
            foreach (var obj in _currentObjectives)
                Log($"  [{obj.Level}] {obj.Description}");
        }

        // ══════════════════════════════════════════════════════
        // EVALUACIÓN FIN DE TEMPORADA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Evalúa objetivos al final de temporada.
        /// Retorna resultado con consecuencias.
        /// </summary>
        public ObjectiveResult EvaluateObjectives()
        {
            var team = _gm.GetPlayerTeam();
            if (team == null || _currentObjectives.Count == 0)
                return new ObjectiveResult();

            var result = new ObjectiveResult
            {
                MinimumMet = false,
                StandardMet = false,
                EliteMet = false
            };

            foreach (var obj in _currentObjectives)
            {
                bool met = EvaluateSingleObjective(obj, team);

                switch (obj.Level)
                {
                    case ObjectiveLevel.Minimum: result.MinimumMet = met; break;
                    case ObjectiveLevel.Standard: result.StandardMet = met; break;
                    case ObjectiveLevel.Elite: result.EliteMet = met; break;
                }
            }

            // Consecuencias
            if (!result.MinimumMet)
            {
                _consecutiveFailures++;
                result.Consequences = "❌ Objetivo mínimo NO cumplido — " +
                    $"La junta está descontenta ({_consecutiveFailures} temporada(s) consecutiva(s))";

                if (_consecutiveFailures >= 2)
                {
                    _boardPressureActive = true;
                    result.Consequences += "\n🚨 ¡PRESIÓN MÁXIMA DE LA JUNTA! " +
                        "Una temporada más así y habrá consecuencias graves";
                    result.BudgetPenalty = -10f; // -$10M

                    // Evento de presión
                    _eventBus.FireRandomEvent(new EventBus.RandomEventArgs
                    {
                        EventId = "board_pressure",
                        EventType = "Negative",
                        Title = "Presión de la Junta Directiva",
                        Description = "La junta está muy insatisfecha con los resultados. " +
                            "Necesitas mejorar o enfrentarás consecuencias graves.",
                        AffectedTeamId = _gm.PlayerTeamId,
                        PlayerOptions = new List<string>
                        {
                            "Prometer mejoras",
                            "Pedir paciencia",
                            "Considerar cambios radicales"
                        }
                    });
                }
            }
            else
            {
                _consecutiveFailures = 0;
                _boardPressureActive = false;
            }

            if (result.StandardMet)
            {
                result.BudgetBonus = 5f;
                result.Consequences += "\n✅ Objetivo estándar cumplido — +$5M para próxima temporada";
            }

            if (result.EliteMet)
            {
                result.BudgetBonus = 15f; // Reemplaza el de standard
                result.Consequences += "\n🌟 ¡OBJETIVO ÉLITE! — +$15M para próxima temporada";
            }

            Log($"Evaluación T{_gm.CurrentSeason}: " +
                $"Min={result.MinimumMet}, Std={result.StandardMet}, " +
                $"Elite={result.EliteMet}");

            return result;
        }

        private bool EvaluateSingleObjective(SeasonObjective obj, TeamData team)
        {
            // Posición
            if (obj.TargetPosition > 0 &&
                team.constructorPosition > obj.TargetPosition)
                return false;

            // Puntos mínimos
            if (obj.TargetPoints > 0 &&
                team.constructorPoints < obj.TargetPoints)
                return false;

            // Necesita victoria
            if (obj.RequiresWin && team.totalWins == 0)
                return false;

            // Necesita podio
            if (obj.RequiresPodium && team.totalPodiums == 0)
                return false;

            // Ambos campeonatos
            if (obj.RequiresBothChampionships)
            {
                var driverChamp = _gm.GetDriverChampion();
                var constructorChamp = _gm.GetConstructorChampion();

                if (constructorChamp?.id != _gm.PlayerTeamId)
                    return false;
                if (driverChamp?.currentTeamId != _gm.PlayerTeamId)
                    return false;
            }

            return true;
        }

        // ══════════════════════════════════════════════════════
        // CLASIFICACIÓN DE EQUIPO
        // ══════════════════════════════════════════════════════

        private enum TeamTier { Top, MidHigh, Mid, Low }

        private TeamTier ClassifyTeam(TeamData team)
        {
            // Usar reputación y presupuesto base
            float score = team.reputation * 0.6f +
                (team.baseBudget / Constants.BUDGET_CAP) * 100f * 0.4f;

            if (score >= 80) return TeamTier.Top;
            if (score >= 60) return TeamTier.MidHigh;
            if (score >= 40) return TeamTier.Mid;
            return TeamTier.Low;
        }

        // ══════════════════════════════════════════════════════
        // LOG
        // ══════════════════════════════════════════════════════

        private void Log(string msg)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            UnityEngine.Debug.Log($"[ObjectiveManager] {msg}");
            #else
            Console.WriteLine($"[ObjectiveManager] {msg}");
            #endif
        }
    }

    // ══════════════════════════════════════════════════════
    // TIPOS
    // ══════════════════════════════════════════════════════

    [Serializable]
    public class SeasonObjective
    {
        public ObjectiveLevel Level;
        public string Description;
        public int TargetPosition;
        public int TargetPoints;
        public bool RequiresWin;
        public bool RequiresPodium;
        public bool RequiresBothChampionships;
        public float RewardMoney;
        public string FailConsequence;
    }

    [Serializable]
    public class ObjectiveResult
    {
        public bool MinimumMet;
        public bool StandardMet;
        public bool EliteMet;
        public string Consequences;
        public float BudgetBonus;
        public float BudgetPenalty;
    }
}
