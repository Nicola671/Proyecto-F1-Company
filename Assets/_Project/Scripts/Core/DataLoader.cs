// ============================================================
// F1 Career Manager — DataLoader.cs
// Carga todos los JSONs y cachea en memoria
// ============================================================
// DEPENDENCIAS: Ninguna de otros scripts (es independiente)
// ORDEN: Debe ejecutarse ANTES que cualquier otro sistema
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Data;

namespace F1CareerManager.Core
{
    /// <summary>
    /// Carga y cachea todos los datos del juego desde Resources/Data/.
    /// Provee búsqueda por ID y validación.
    /// </summary>
    public class DataLoader
    {
        // ── Cache ────────────────────────────────────────────
        private List<PilotData> _pilots;
        private List<TeamData> _teams;
        private List<CircuitData> _circuits;
        private List<ComponentData> _components;
        private List<StaffData> _staffPool;
        private List<SponsorData> _sponsors;
        private List<RandomEventData> _randomEvents;
        private DifficultyConfig _difficultyConfig;

        // ── Flags ────────────────────────────────────────────
        private bool _pilotsLoaded;
        private bool _teamsLoaded;
        private bool _circuitsLoaded;
        private bool _componentsLoaded;
        private bool _staffLoaded;
        private bool _sponsorsLoaded;
        private bool _eventsLoaded;

        // ── Indices de búsqueda ──────────────────────────────
        private Dictionary<string, PilotData> _pilotIndex;
        private Dictionary<string, TeamData> _teamIndex;
        private Dictionary<string, CircuitData> _circuitIndex;

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public DataLoader()
        {
            _pilotIndex = new Dictionary<string, PilotData>();
            _teamIndex = new Dictionary<string, TeamData>();
            _circuitIndex = new Dictionary<string, CircuitData>();
        }

        // ══════════════════════════════════════════════════════
        // CARGA DE DATOS
        // ══════════════════════════════════════════════════════

        /// <summary>Carga todos los datos del juego de una vez</summary>
        public bool LoadAll()
        {
            bool success = true;

            success &= LoadAllPilots() != null;
            success &= LoadAllTeams() != null;
            success &= LoadAllCircuits() != null;
            LoadComponents();
            LoadStaffPool();
            LoadSponsors();
            LoadRandomEvents();

            if (success)
                Log("Todos los datos cargados correctamente");
            else
                LogError("Error al cargar datos — revise los JSONs");

            return success;
        }

        /// <summary>Carga pilotos desde Resources/Data/pilots.json</summary>
        public List<PilotData> LoadAllPilots()
        {
            if (_pilotsLoaded && _pilots != null)
                return _pilots;

            string json = LoadJsonResource("Data/pilots");
            if (string.IsNullOrEmpty(json))
            {
                LogError("No se encontró pilots.json en Resources/Data/");
                _pilots = new List<PilotData>();
                return _pilots;
            }

            var container = JsonParser.FromJson<PilotsContainer>(json);
            _pilots = container?.pilots ?? new List<PilotData>();

            // Construir índice
            _pilotIndex.Clear();
            foreach (var p in _pilots)
            {
                if (!string.IsNullOrEmpty(p.id))
                    _pilotIndex[p.id] = p;
            }

            _pilotsLoaded = true;
            Log($"Pilotos cargados: {_pilots.Count}");
            return _pilots;
        }

        /// <summary>Carga equipos desde Resources/Data/teams.json</summary>
        public List<TeamData> LoadAllTeams()
        {
            if (_teamsLoaded && _teams != null)
                return _teams;

            string json = LoadJsonResource("Data/teams");
            if (string.IsNullOrEmpty(json))
            {
                LogError("No se encontró teams.json en Resources/Data/");
                _teams = new List<TeamData>();
                return _teams;
            }

            var container = JsonParser.FromJson<TeamsContainer>(json);
            _teams = container?.teams ?? new List<TeamData>();

            _teamIndex.Clear();
            foreach (var t in _teams)
            {
                if (!string.IsNullOrEmpty(t.id))
                    _teamIndex[t.id] = t;
            }

            _teamsLoaded = true;
            Log($"Equipos cargados: {_teams.Count}");
            return _teams;
        }

        /// <summary>Carga circuitos desde Resources/Data/circuits.json</summary>
        public List<CircuitData> LoadAllCircuits()
        {
            if (_circuitsLoaded && _circuits != null)
                return _circuits;

            string json = LoadJsonResource("Data/circuits");
            if (string.IsNullOrEmpty(json))
            {
                LogError("No se encontró circuits.json en Resources/Data/");
                _circuits = new List<CircuitData>();
                return _circuits;
            }

            var container = JsonParser.FromJson<CircuitsContainer>(json);
            _circuits = container?.circuits ?? new List<CircuitData>();

            _circuitIndex.Clear();
            foreach (var c in _circuits)
            {
                if (!string.IsNullOrEmpty(c.id))
                    _circuitIndex[c.id] = c;
            }

            _circuitsLoaded = true;
            Log($"Circuitos cargados: {_circuits.Count}");
            return _circuits;
        }

        /// <summary>Carga componentes R&D</summary>
        public List<ComponentData> LoadComponents()
        {
            if (_componentsLoaded && _components != null)
                return _components;

            string json = LoadJsonResource("Data/components");
            if (string.IsNullOrEmpty(json))
            {
                Log("components.json no encontrado (se generarán en runtime)");
                _components = new List<ComponentData>();
                _componentsLoaded = true;
                return _components;
            }

            var container = JsonParser.FromJson<ComponentsContainer>(json);
            _components = container?.components ?? new List<ComponentData>();
            _componentsLoaded = true;
            return _components;
        }

        /// <summary>Pool de staff disponibles</summary>
        public List<StaffData> LoadStaffPool()
        {
            if (_staffLoaded && _staffPool != null)
                return _staffPool;

            string json = LoadJsonResource("Data/staff");
            if (string.IsNullOrEmpty(json))
            {
                Log("staff.json no encontrado (se generarán en runtime)");
                _staffPool = new List<StaffData>();
                _staffLoaded = true;
                return _staffPool;
            }

            var container = JsonParser.FromJson<StaffContainer>(json);
            _staffPool = container?.staff ?? new List<StaffData>();
            _staffLoaded = true;
            return _staffPool;
        }

        /// <summary>Carga sponsors disponibles</summary>
        public List<SponsorData> LoadSponsors()
        {
            if (_sponsorsLoaded && _sponsors != null)
                return _sponsors;

            string json = LoadJsonResource("Data/sponsors");
            if (string.IsNullOrEmpty(json))
            {
                Log("sponsors.json no encontrado (se generarán en runtime)");
                _sponsors = new List<SponsorData>();
                _sponsorsLoaded = true;
                return _sponsors;
            }

            var container = JsonParser.FromJson<SponsorsContainer>(json);
            _sponsors = container?.sponsors ?? new List<SponsorData>();
            _sponsorsLoaded = true;
            return _sponsors;
        }

        /// <summary>Carga pool de eventos aleatorios</summary>
        public List<RandomEventData> LoadRandomEvents()
        {
            if (_eventsLoaded && _randomEvents != null)
                return _randomEvents;

            string json = LoadJsonResource("Data/random_events");
            if (string.IsNullOrEmpty(json))
            {
                Log("random_events.json no encontrado (se usará generación por código)");
                _randomEvents = new List<RandomEventData>();
                _eventsLoaded = true;
                return _randomEvents;
            }

            var container = JsonParser.FromJson<RandomEventsContainer>(json);
            _randomEvents = container?.events ?? new List<RandomEventData>();
            _eventsLoaded = true;
            return _randomEvents;
        }

        /// <summary>Carga config de dificultad</summary>
        public DifficultyConfig LoadDifficulty(DifficultyLevel level)
        {
            string filename = $"Data/difficulty_{level.ToString().ToLower()}";
            string json = LoadJsonResource(filename);

            if (string.IsNullOrEmpty(json))
            {
                Log($"Usando configuración de dificultad por defecto para {level}");
                return GetDefaultDifficulty(level);
            }

            _difficultyConfig = JsonParser.FromJson<DifficultyConfig>(json);
            return _difficultyConfig;
        }

        // ══════════════════════════════════════════════════════
        // BÚSQUEDA POR ID (O(1) con índices)
        // ══════════════════════════════════════════════════════

        /// <summary>Busca piloto por ID — O(1)</summary>
        public PilotData GetPilotById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _pilotIndex.TryGetValue(id, out PilotData pilot);
            return pilot;
        }

        /// <summary>Busca equipo por ID — O(1)</summary>
        public TeamData GetTeamById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _teamIndex.TryGetValue(id, out TeamData team);
            return team;
        }

        /// <summary>Busca circuito por ID — O(1)</summary>
        public CircuitData GetCircuitById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _circuitIndex.TryGetValue(id, out CircuitData circuit);
            return circuit;
        }

        /// <summary>Pilotos de un equipo</summary>
        public List<PilotData> GetPilotsByTeam(string teamId)
        {
            if (_pilots == null) return new List<PilotData>();
            return _pilots.FindAll(p =>
                p.currentTeamId == teamId && !p.isRetired);
        }

        // ══════════════════════════════════════════════════════
        // VALIDACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>Valida integridad de los datos cargados</summary>
        public List<string> ValidateData()
        {
            var errors = new List<string>();

            // Verificar pilotos
            if (_pilots == null || _pilots.Count == 0)
                errors.Add("No hay pilotos cargados");
            else if (_pilots.Count < Constants.TOTAL_PILOTS)
                errors.Add($"Faltan pilotos: {_pilots.Count}/{Constants.TOTAL_PILOTS}");

            // Verificar equipos
            if (_teams == null || _teams.Count == 0)
                errors.Add("No hay equipos cargados");
            else if (_teams.Count < Constants.MAX_TEAMS)
                errors.Add($"Faltan equipos: {_teams.Count}/{Constants.MAX_TEAMS}");

            // Verificar circuitos
            if (_circuits == null || _circuits.Count == 0)
                errors.Add("No hay circuitos cargados");
            else if (_circuits.Count < Constants.TOTAL_CIRCUITS)
                errors.Add($"Faltan circuitos: {_circuits.Count}/{Constants.TOTAL_CIRCUITS}");

            // Pilotos sin equipo válido
            if (_pilots != null && _teams != null)
            {
                foreach (var p in _pilots)
                {
                    if (!string.IsNullOrEmpty(p.currentTeamId) &&
                        GetTeamById(p.currentTeamId) == null)
                    {
                        errors.Add($"Piloto {p.id} tiene teamId inválido: {p.currentTeamId}");
                    }
                }
            }

            // Circuitos con rounds duplicados
            if (_circuits != null)
            {
                var rounds = new HashSet<int>();
                foreach (var c in _circuits)
                {
                    if (!rounds.Add(c.roundNumber))
                        errors.Add($"Round duplicado: {c.roundNumber} ({c.id})");
                }
            }

            if (errors.Count == 0)
                Log("Validación exitosa — todos los datos son correctos");
            else
                foreach (var err in errors) LogError($"Validación: {err}");

            return errors;
        }

        /// <summary>Limpia todo el cache</summary>
        public void ClearCache()
        {
            _pilots = null; _teams = null; _circuits = null;
            _components = null; _staffPool = null; _sponsors = null;
            _randomEvents = null;
            _pilotsLoaded = _teamsLoaded = _circuitsLoaded = false;
            _componentsLoaded = _staffLoaded = _sponsorsLoaded = false;
            _eventsLoaded = false;
            _pilotIndex.Clear(); _teamIndex.Clear(); _circuitIndex.Clear();
        }

        // ══════════════════════════════════════════════════════
        // CONTENEDORES JSON
        // ══════════════════════════════════════════════════════

        [Serializable] public class ComponentsContainer { public List<ComponentData> components; }
        [Serializable] public class StaffContainer { public List<StaffData> staff; }
        [Serializable] public class SponsorsContainer { public List<SponsorData> sponsors; }
        [Serializable] public class RandomEventsContainer { public List<RandomEventData> events; }

        // ══════════════════════════════════════════════════════
        // DIFICULTAD POR DEFECTO
        // ══════════════════════════════════════════════════════

        private DifficultyConfig GetDefaultDifficulty(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Narrative:
                    return new DifficultyConfig
                    {
                        level = "Narrative", economyMult = 1.25f,
                        moodBonus = 15, fiaDetectionMult = 0.7f,
                        negativeEventMult = 0.5f, aiStrengthMult = 0.85f
                    };
                case DifficultyLevel.Demanding:
                    return new DifficultyConfig
                    {
                        level = "Demanding", economyMult = 0.85f,
                        moodBonus = 0, fiaDetectionMult = 1.2f,
                        negativeEventMult = 1.3f, aiStrengthMult = 1.1f
                    };
                case DifficultyLevel.Legend:
                    return new DifficultyConfig
                    {
                        level = "Legend", economyMult = 0.7f,
                        moodBonus = -10, fiaDetectionMult = 1.5f,
                        negativeEventMult = 1.5f, aiStrengthMult = 1.25f
                    };
                default: // Standard
                    return new DifficultyConfig
                    {
                        level = "Standard", economyMult = 1.0f,
                        moodBonus = 0, fiaDetectionMult = 1.0f,
                        negativeEventMult = 1.0f, aiStrengthMult = 1.0f
                    };
            }
        }

        // ══════════════════════════════════════════════════════
        // IO
        // ══════════════════════════════════════════════════════

        private string LoadJsonResource(string path)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            var textAsset = UnityEngine.Resources.Load<UnityEngine.TextAsset>(path);
            if (textAsset == null) return null;
            return textAsset.text;
            #else
            // Fallback para testing fuera de Unity
            string filePath = $"Assets/Resources/{path}.json";
            if (System.IO.File.Exists(filePath))
                return System.IO.File.ReadAllText(filePath);
            return null;
            #endif
        }

        private void Log(string msg)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            UnityEngine.Debug.Log($"[DataLoader] {msg}");
            #else
            Console.WriteLine($"[DataLoader] {msg}");
            #endif
        }

        private void LogError(string msg)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            UnityEngine.Debug.LogError($"[DataLoader] {msg}");
            #else
            Console.Error.WriteLine($"[DataLoader] ERROR: {msg}");
            #endif
        }
    }

    // ══════════════════════════════════════════════════════
    // TIPOS AUXILIARES
    // ══════════════════════════════════════════════════════

    [Serializable]
    public class DifficultyConfig
    {
        public string level;
        public float economyMult;
        public int moodBonus;
        public float fiaDetectionMult;
        public float negativeEventMult;
        public float aiStrengthMult;
    }

    [Serializable]
    public class RandomEventData
    {
        public string id;
        public string title;
        public string description;
        public string type;        // "Positive", "Negative", "Neutral"
        public string target;      // "Player", "Rival", "Any"
        public float probability;
        public List<RandomEventOption> options;
    }

    [Serializable]
    public class RandomEventOption
    {
        public string text;
        public string effect;
        public float value;
    }
}
