// ============================================================
// F1 Career Manager — GameManager.cs
// Manager principal del juego — Singleton
// Coordina todos los subsistemas y gestiona el estado del juego
// ============================================================
// DEPENDENCIAS: EventBus.cs, PilotData.cs, TeamData.cs,
//               CircuitData.cs, StaffData.cs
// EVENTOS QUE DISPARA: OnGameStateChanged, OnSeasonEnd
// EVENTOS QUE ESCUCHA: OnRaceFinished, OnSeasonEnd
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.Core
{
    /// <summary>
    /// Estados posibles del juego
    /// </summary>
    public enum GameState
    {
        MainMenu,
        TeamSelection,
        Hub,
        RaceWeekend,
        EndSeason,
        Loading,
        PressConference,
        TransferWindow,
        Paused
    }

    /// <summary>
    /// Contenedor para la deserialización de los JSON de pilotos
    /// </summary>
    [Serializable]
    public class PilotsContainer
    {
        public List<PilotData> pilots;
    }

    /// <summary>
    /// Contenedor para la deserialización de los JSON de equipos
    /// </summary>
    [Serializable]
    public class TeamsContainer
    {
        public List<TeamData> teams;
    }

    /// <summary>
    /// Contenedor para la deserialización de los JSON de circuitos
    /// </summary>
    [Serializable]
    public class CircuitsContainer
    {
        public List<CircuitData> circuits;
    }

    /// <summary>
    /// Manager principal que coordina todos los sistemas del juego.
    /// Gestiona el ciclo de vida: menú → selección → temporada → carrera → fin.
    /// </summary>
    public class GameManager
    {
        // ── Singleton ────────────────────────────────────────
        private static GameManager _instance;
        private static readonly object _lock = new object();

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new GameManager();
                    }
                }
                return _instance;
            }
        }

        // ── Estado del juego ─────────────────────────────────
        private GameState _currentState;
        public GameState CurrentState => _currentState;

        // ── Datos del juego ──────────────────────────────────
        private List<PilotData> _allPilots;
        private List<TeamData> _allTeams;
        private List<CircuitData> _allCircuits;
        private List<StaffData> _allStaff;

        // Acceso público de solo lectura
        public IReadOnlyList<PilotData> AllPilots => _allPilots?.AsReadOnly();
        public IReadOnlyList<TeamData> AllTeams => _allTeams?.AsReadOnly();
        public IReadOnlyList<CircuitData> AllCircuits => _allCircuits?.AsReadOnly();
        public IReadOnlyList<StaffData> AllStaff => _allStaff?.AsReadOnly();

        // ── Datos del jugador ────────────────────────────────
        private string _playerTeamId;
        private int _currentSeason;
        private int _currentRound;
        private DifficultyLevel _difficulty;
        private LegacyData _legacyData;
        private List<SeasonData> _seasonHistory;

        public string PlayerTeamId => _playerTeamId;
        public int CurrentSeason => _currentSeason;
        public int CurrentRound => _currentRound;
        public DifficultyLevel Difficulty => _difficulty;
        public LegacyData Legacy => _legacyData;

        // ── Random global (para consistencia) ────────────────
        private Random _rng;
        public Random RNG => _rng;

        // ── Referencia al EventBus ───────────────────────────
        private EventBus _eventBus;

        // ── Flag de inicialización ───────────────────────────
        private bool _isInitialized;
        public bool IsInitialized => _isInitialized;

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        private GameManager()
        {
            _currentState = GameState.MainMenu;
            _eventBus = EventBus.Instance;
            _rng = new Random();
            _isInitialized = false;
            _seasonHistory = new List<SeasonData>();
            _legacyData = new LegacyData();

            // Suscribirse a eventos relevantes
            _eventBus.OnRaceFinished += HandleRaceFinished;
        }

        // ══════════════════════════════════════════════════════
        // INICIO DE JUEGO NUEVO
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Inicia un juego nuevo. Carga los JSON y prepara todos los datos.
        /// </summary>
        /// <param name="teamId">ID del equipo seleccionado por el jugador</param>
        /// <param name="difficulty">Nivel de dificultad elegido</param>
        /// <param name="pilotsJson">Contenido JSON de pilotos</param>
        /// <param name="teamsJson">Contenido JSON de equipos</param>
        /// <param name="circuitsJson">Contenido JSON de circuitos</param>
        public void StartNewGame(string teamId, DifficultyLevel difficulty,
            string pilotsJson, string teamsJson, string circuitsJson)
        {
            ChangeState(GameState.Loading);

            _difficulty = difficulty;
            _playerTeamId = teamId;
            _currentSeason = 1;
            _currentRound = 1;

            // Cargar datos desde JSON
            LoadGameData(pilotsJson, teamsJson, circuitsJson);

            // Marcar el equipo del jugador
            var playerTeam = GetTeamById(teamId);
            if (playerTeam != null)
            {
                playerTeam.isPlayerControlled = true;
            }

            // Aplicar modificadores de dificultad a los datos iniciales
            ApplyDifficultyModifiers();

            // Calcular overalls y estrellas de todos los pilotos
            foreach (var pilot in _allPilots)
            {
                pilot.CalculateOverall();
                pilot.CalculateStars();
            }

            // Calcular rendimiento de todos los autos
            foreach (var team in _allTeams)
            {
                team.CalculateCarPerformance();
            }

            // Inicializar datos de legado
            _legacyData = new LegacyData
            {
                totalLegacyPoints = 0,
                totalSeasons = 0,
                constructorChampionships = 0,
                driverChampionships = 0,
                totalWins = 0,
                totalPodiums = 0,
                regensToChampion = 0,
                hallOfFameEntries = new List<string>(),
                memorableEvents = new List<string>()
            };

            _seasonHistory = new List<SeasonData>();
            _isInitialized = true;

            // Ir al Hub
            ChangeState(GameState.Hub);
        }

        // ══════════════════════════════════════════════════════
        // CARGA DE DATOS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Carga los datos del juego desde strings JSON
        /// </summary>
        private void LoadGameData(string pilotsJson, string teamsJson, string circuitsJson)
        {
            // Deserializar pilotos
            var pilotsContainer = JsonParser.FromJson<PilotsContainer>(pilotsJson);
            _allPilots = pilotsContainer?.pilots ?? new List<PilotData>();

            // Deserializar equipos
            var teamsContainer = JsonParser.FromJson<TeamsContainer>(teamsJson);
            _allTeams = teamsContainer?.teams ?? new List<TeamData>();

            // Deserializar circuitos
            var circuitsContainer = JsonParser.FromJson<CircuitsContainer>(circuitsJson);
            _allCircuits = circuitsContainer?.circuits ?? new List<CircuitData>();

            // Staff se inicializa vacío (se generará en su sistema)
            _allStaff = new List<StaffData>();
        }

        /// <summary>
        /// Aplica los modificadores de dificultad a los datos del juego
        /// </summary>
        private void ApplyDifficultyModifiers()
        {
            var playerTeam = GetTeamById(_playerTeamId);
            if (playerTeam == null) return;

            switch (_difficulty)
            {
                case DifficultyLevel.Narrative:
                    // Economía más generosa para el jugador
                    playerTeam.budget *= Constants.DIFF_NARRATIVE_ECONOMY;
                    playerTeam.baseBudget *= Constants.DIFF_NARRATIVE_ECONOMY;
                    // Pilotos del jugador más contentos
                    foreach (var pilot in _allPilots)
                    {
                        if (pilot.currentTeamId == _playerTeamId)
                            pilot.moodValue += (int)Constants.DIFF_NARRATIVE_MOOD;
                    }
                    break;

                case DifficultyLevel.Demanding:
                    // Presupuesto reducido
                    playerTeam.budget *= Constants.DIFF_HARD_BUDGET;
                    playerTeam.baseBudget *= Constants.DIFF_HARD_BUDGET;
                    break;

                case DifficultyLevel.Legend:
                    // Empezás con el peor equipo
                    // (El equipo ya elegido se mantiene, pero se puede forzar
                    //  el peor equipo desde la UI de selección)
                    playerTeam.budget *= 0.70f; // -30% presupuesto
                    playerTeam.baseBudget *= 0.70f;
                    break;

                case DifficultyLevel.Standard:
                default:
                    // Sin modificaciones
                    break;
            }
        }

        // ══════════════════════════════════════════════════════
        // GESTIÓN DE ESTADO
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Cambia el estado actual del juego y notifica al EventBus
        /// </summary>
        public void ChangeState(GameState newState)
        {
            var previousState = _currentState;
            _currentState = newState;

            _eventBus.FireGameStateChanged(new EventBus.GameStateChangedArgs
            {
                PreviousState = previousState.ToString(),
                NewState = newState.ToString()
            });
        }

        // ══════════════════════════════════════════════════════
        // FLUJO DE TEMPORADA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Avanza a la siguiente ronda del calendario
        /// </summary>
        public void AdvanceToNextRound()
        {
            if (_currentRound >= _allCircuits.Count)
            {
                // Fin de temporada
                EndSeason();
                return;
            }

            _currentRound++;
        }

        /// <summary>
        /// Obtiene el circuito de la ronda actual
        /// </summary>
        public CircuitData GetCurrentCircuit()
        {
            if (_allCircuits == null || _currentRound < 1 || _currentRound > _allCircuits.Count)
                return null;

            return _allCircuits.Find(c => c.roundNumber == _currentRound);
        }

        /// <summary>
        /// Inicia el fin de semana de carrera para la ronda actual
        /// </summary>
        public void StartRaceWeekend()
        {
            ChangeState(GameState.RaceWeekend);
        }

        /// <summary>
        /// Finaliza la temporada actual y prepara la siguiente
        /// </summary>
        private void EndSeason()
        {
            ChangeState(GameState.EndSeason);

            // Determinar campeones
            var driverChampion = GetDriverChampion();
            var constructorChampion = GetConstructorChampion();
            var playerTeam = GetTeamById(_playerTeamId);

            // Guardar resumen de temporada
            var seasonSummary = new SeasonData
            {
                seasonNumber = _currentSeason,
                currentRound = _allCircuits.Count,
                totalRounds = _allCircuits.Count,
                driverChampionId = driverChampion?.id ?? "",
                constructorChampionId = constructorChampion?.id ?? "",
                raceResults = new List<RaceResultData>(),
                majorEvents = new List<string>()
            };
            _seasonHistory.Add(seasonSummary);

            // Evaluar objetivos del jugador
            bool minMet = false, stdMet = false, eliteMet = false;
            if (playerTeam != null)
            {
                minMet = playerTeam.constructorPosition <= playerTeam.objectiveMinTarget;
                stdMet = playerTeam.constructorPosition <= playerTeam.objectiveStdTarget;
                eliteMet = playerTeam.constructorPosition <= playerTeam.objectiveEliteTarget;
            }

            // Calcular puntos de legado
            UpdateLegacyPoints(driverChampion, constructorChampion, playerTeam);

            // Disparar evento de fin de temporada
            _eventBus.FireSeasonEnd(new EventBus.SeasonEndArgs
            {
                SeasonNumber = _currentSeason,
                DriverChampionId = driverChampion?.id ?? "",
                DriverChampionName = driverChampion != null
                    ? $"{driverChampion.firstName} {driverChampion.lastName}" : "",
                ConstructorChampionId = constructorChampion?.id ?? "",
                ConstructorChampionName = constructorChampion?.fullName ?? "",
                PlayerTeamPosition = playerTeam?.constructorPosition.ToString() ?? "N/A",
                ObjectiveMinMet = minMet,
                ObjectiveStdMet = stdMet,
                ObjectiveEliteMet = eliteMet
            });

            _legacyData.totalSeasons++;
        }

        /// <summary>
        /// Prepara la nueva temporada (después de la pantalla de fin de temporada)
        /// </summary>
        public void StartNewSeason()
        {
            _currentSeason++;
            _currentRound = 1;

            // Resetear puntos de campeonato de todos los equipos
            foreach (var team in _allTeams)
            {
                team.constructorPoints = 0;
                team.constructorPosition = 0;
            }

            // Envejecer pilotos
            foreach (var pilot in _allPilots)
            {
                if (!pilot.isRetired)
                    pilot.age++;
            }

            // Decrementar años de contrato
            foreach (var pilot in _allPilots)
            {
                if (!pilot.isRetired && pilot.contractYearsLeft > 0)
                    pilot.contractYearsLeft--;
            }

            foreach (var staff in _allStaff)
            {
                if (!staff.isRetired && staff.contractYearsLeft > 0)
                    staff.contractYearsLeft--;
            }

            ChangeState(GameState.Hub);
        }

        // ══════════════════════════════════════════════════════
        // LEGADO
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Actualiza los puntos de legado al final de la temporada
        /// </summary>
        private void UpdateLegacyPoints(PilotData driverChamp, TeamData constructorChamp,
            TeamData playerTeam)
        {
            if (playerTeam == null) return;

            // ¿El jugador ganó el campeonato de constructores?
            if (constructorChamp?.id == _playerTeamId)
            {
                _legacyData.totalLegacyPoints += Constants.LEGACY_CONSTRUCTORS_CHAMP;
                _legacyData.constructorChampionships++;
            }

            // ¿Un piloto del jugador ganó el campeonato de pilotos?
            if (driverChamp?.currentTeamId == _playerTeamId)
            {
                _legacyData.totalLegacyPoints += Constants.LEGACY_DRIVERS_CHAMP;
                _legacyData.driverChampionships++;

                // ¿Era un regen?
                if (driverChamp.isRegen)
                {
                    _legacyData.totalLegacyPoints += Constants.LEGACY_REGEN_TO_CHAMP;
                    _legacyData.regensToChampion++;
                }
            }

            // Bonus por año de permanencia
            _legacyData.totalLegacyPoints += Constants.LEGACY_YEAR_BONUS;
        }

        // ══════════════════════════════════════════════════════
        // BÚSQUEDA DE DATOS
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene un piloto por su ID</summary>
        public PilotData GetPilotById(string pilotId)
        {
            return _allPilots?.Find(p => p.id == pilotId);
        }

        /// <summary>Obtiene un equipo por su ID</summary>
        public TeamData GetTeamById(string teamId)
        {
            return _allTeams?.Find(t => t.id == teamId);
        }

        /// <summary>Obtiene un circuito por su ID</summary>
        public CircuitData GetCircuitById(string circuitId)
        {
            return _allCircuits?.Find(c => c.id == circuitId);
        }

        /// <summary>Obtiene el equipo del jugador</summary>
        public TeamData GetPlayerTeam()
        {
            return GetTeamById(_playerTeamId);
        }

        /// <summary>Obtiene los pilotos de un equipo</summary>
        public List<PilotData> GetPilotsByTeam(string teamId)
        {
            return _allPilots?.FindAll(p => p.currentTeamId == teamId && !p.isRetired)
                   ?? new List<PilotData>();
        }

        /// <summary>Obtiene el piloto líder del campeonato</summary>
        public PilotData GetDriverChampion()
        {
            if (_allPilots == null || _allPilots.Count == 0) return null;

            PilotData leader = _allPilots[0];
            foreach (var p in _allPilots)
            {
                if (p.totalPoints > leader.totalPoints && !p.isRetired)
                    leader = p;
            }
            return leader;
        }

        /// <summary>Obtiene el equipo líder del campeonato de constructores</summary>
        public TeamData GetConstructorChampion()
        {
            if (_allTeams == null || _allTeams.Count == 0) return null;

            TeamData leader = _allTeams[0];
            foreach (var t in _allTeams)
            {
                if (t.constructorPoints > leader.constructorPoints)
                    leader = t;
            }
            return leader;
        }

        /// <summary>Obtiene la clasificación ordenada de constructores</summary>
        public List<TeamData> GetConstructorStandings()
        {
            if (_allTeams == null) return new List<TeamData>();

            var sorted = new List<TeamData>(_allTeams);
            sorted.Sort((a, b) => b.constructorPoints.CompareTo(a.constructorPoints));

            for (int i = 0; i < sorted.Count; i++)
                sorted[i].constructorPosition = i + 1;

            return sorted;
        }

        /// <summary>Obtiene la clasificación ordenada de pilotos</summary>
        public List<PilotData> GetDriverStandings()
        {
            if (_allPilots == null) return new List<PilotData>();

            var active = _allPilots.FindAll(p => !p.isRetired && p.currentTeamId != null);
            active.Sort((a, b) => b.totalPoints.CompareTo(a.totalPoints));
            return active;
        }

        // ══════════════════════════════════════════════════════
        // GUARDADO Y CARGA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera el objeto de guardado con todo el estado actual
        /// </summary>
        public SaveData CreateSaveData(int slotNumber, string saveType)
        {
            return new SaveData
            {
                saveId = Guid.NewGuid().ToString(),
                saveType = saveType,
                slotNumber = slotNumber,
                teamId = _playerTeamId,
                currentSeason = _currentSeason,
                constructorPosition = GetTeamById(_playerTeamId)?.constructorPosition ?? 0,
                realDateSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                allPilots = _allPilots != null ? new List<PilotData>(_allPilots) : new List<PilotData>(),
                allTeams = _allTeams != null ? new List<TeamData>(_allTeams) : new List<TeamData>(),
                allStaff = _allStaff != null ? new List<StaffData>(_allStaff) : new List<StaffData>(),
                legacy = _legacyData,
                pastSeasons = _seasonHistory != null
                    ? new List<SeasonData>(_seasonHistory)
                    : new List<SeasonData>()
            };
        }

        /// <summary>
        /// Carga un juego guardado desde un SaveData
        /// </summary>
        public void LoadGame(SaveData saveData)
        {
            if (saveData == null) return;

            ChangeState(GameState.Loading);

            _playerTeamId = saveData.teamId;
            _currentSeason = saveData.currentSeason;
            _allPilots = saveData.allPilots ?? new List<PilotData>();
            _allTeams = saveData.allTeams ?? new List<TeamData>();
            _allStaff = saveData.allStaff ?? new List<StaffData>();
            _legacyData = saveData.legacy ?? new LegacyData();
            _seasonHistory = saveData.pastSeasons ?? new List<SeasonData>();

            _isInitialized = true;

            ChangeState(GameState.Hub);
        }

        // ══════════════════════════════════════════════════════
        // HANDLERS DE EVENTOS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Maneja el evento de carrera finalizada.
        /// Actualiza puntos del campeonato y avanza la ronda.
        /// </summary>
        private void HandleRaceFinished(object sender, EventBus.RaceFinishedArgs args)
        {
            if (args.FinalPositions == null) return;

            // Actualizar puntos de pilotos y equipos
            foreach (var pos in args.FinalPositions)
            {
                if (pos.DNF) continue;

                var pilot = GetPilotById(pos.PilotId);
                var team = GetTeamById(pos.TeamId);

                if (pilot != null)
                {
                    pilot.totalPoints += pos.PointsEarned;
                    pilot.totalRaces++;

                    if (pos.Position == 1) pilot.totalWins++;
                    if (pos.Position <= 3) pilot.totalPodiums++;
                }

                if (team != null)
                {
                    team.constructorPoints += pos.PointsEarned;
                    if (pos.Position == 1) team.totalWins++;
                    if (pos.Position <= 3) team.totalPodiums++;
                }
            }

            // Actualizar clasificaciones
            GetConstructorStandings();

            // Actualizar legado del jugador
            var playerTeam = GetPlayerTeam();
            if (playerTeam != null && args.WinnerId != null)
            {
                var winner = GetPilotById(args.WinnerId);
                if (winner?.currentTeamId == _playerTeamId)
                {
                    _legacyData.totalWins++;
                    _legacyData.totalLegacyPoints += Constants.LEGACY_PER_WIN;

                    // ¿Ganó en Mónaco?
                    if (args.CircuitId == "monaco")
                        _legacyData.totalLegacyPoints += Constants.LEGACY_MONACO_WIN;
                }

                // Contar podios del jugador
                foreach (var pos in args.FinalPositions)
                {
                    if (pos.Position <= 3 && pos.TeamId == _playerTeamId && !pos.DNF)
                        _legacyData.totalPodiums++;
                }
            }
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Resetea el singleton (para testing o volver al menú)
        /// </summary>
        public static void ResetInstance()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    EventBus.Instance.OnRaceFinished -= _instance.HandleRaceFinished;
                }
                _instance = null;
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // Parser JSON simple (compatible con Unity JsonUtility
    // o System.Text.Json según el contexto)
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Wrapper de JSON parsing. En Unity usará JsonUtility,
    /// fuera de Unity usa System serialization básica.
    /// </summary>
    public static class JsonParser
    {
        /// <summary>
        /// Deserializa un string JSON a un objeto del tipo T.
        /// En Unity: usar JsonUtility.FromJson<T>(json)
        /// Fuera de Unity: usa reflexión básica (placeholder)
        /// </summary>
        public static T FromJson<T>(string json) where T : class
        {
            // NOTA: En Unity, reemplazar esta línea por:
            // return UnityEngine.JsonUtility.FromJson<T>(json);
            //
            // Para testing fuera de Unity, se puede usar:
            // return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            //
            // Placeholder que lanza excepción si no se configura
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            return UnityEngine.JsonUtility.FromJson<T>(json);
            #else
            // Fallback para testing fuera de Unity
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(json,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch
            {
                return null;
            }
            #endif
        }

        /// <summary>
        /// Serializa un objeto a JSON string
        /// </summary>
        public static string ToJson<T>(T obj, bool prettyPrint = false)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            return UnityEngine.JsonUtility.ToJson(obj, prettyPrint);
            #else
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(obj,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = prettyPrint
                    });
            }
            catch
            {
                return "{}";
            }
            #endif
        }
    }
}
