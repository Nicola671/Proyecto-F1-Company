// ============================================================
// F1 Career Manager — RaceSimulator.cs
// Motor de simulación de carreras completo — vuelta por vuelta
// ============================================================
// DEPENDENCIAS: EventBus.cs, PilotBehavior.cs, MoodSystem.cs,
//               PilotData.cs, TeamData.cs, CircuitData.cs,
//               RaceResultData.cs, Constants.cs
// EVENTOS QUE DISPARA: OnRaceFinished, OnQualifyingFinished
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;
using F1CareerManager.AI.PilotAI;

namespace F1CareerManager.Simulation
{
    /// <summary>
    /// Estado interno de un piloto durante la simulación de carrera.
    /// Contiene datos que cambian vuelta a vuelta.
    /// </summary>
    public class RaceEntry
    {
        public PilotData Pilot;
        public TeamData Team;
        public PilotBehavior Behavior;
        public int GridPosition;           // Posición de salida
        public int CurrentPosition;        // Posición actual
        public float CumulativeTime;       // Tiempo acumulado en segundos
        public float LapTime;              // Tiempo de la última vuelta
        public float BestLapTime;          // Mejor vuelta personal
        public float PerformanceRating;    // Rating de rendimiento actual
        public string CurrentTire;         // "Soft", "Medium", "Hard", "Intermediate", "Wet"
        public float TireCondition;        // 100 = nuevo, 0 = destruido
        public float TireDegPerLap;        // Degradación por vuelta
        public int PitStops;               // Pit stops realizados
        public List<int> PitStopLaps;      // En qué vuelta paró
        public bool HasDNF;                // Si abandonó
        public string DNFReason;           // Razón del abandono
        public int DNFLap;                 // Vuelta del abandono
        public int OvertakesMade;          // Adelantamientos realizados
        public float GapToLeader;          // Gap al líder
        public string MotorMode;           // "Conservation", "Normal", "Attack"
        public bool HasFastestLap;         // Si tiene la vuelta rápida
        public int PointsEarned;           // Puntos ganados
        public float FuelLevel;            // 100% al inicio
    }

    /// <summary>
    /// Evento que ocurre durante la carrera
    /// </summary>
    public class RaceIncident
    {
        public int Lap;
        public string Type;                // "SafetyCar", "Crash", etc.
        public string Description;
        public List<string> InvolvedPilotIds;
        public int DurationLaps;           // Duración en vueltas (para safety car)
    }

    /// <summary>
    /// Simula carreras completas vuelta por vuelta.
    /// Input: circuito + pilotos + equipos
    /// Output: RaceResultData con posiciones, tiempos y eventos
    /// </summary>
    public class RaceSimulator
    {
        // ── Datos de la carrera ──────────────────────────────
        private CircuitData _circuit;
        private List<RaceEntry> _entries;
        private List<RaceIncident> _incidents;
        private Random _rng;
        private EventBus _eventBus;

        // ── Estado de carrera ────────────────────────────────
        private bool _isSafetyCarActive;
        private int _safetyCarLapsRemaining;
        private bool _isVSCActive;
        private int _vscLapsRemaining;
        private bool _isRedFlagged;
        private bool _isRaining;
        private string _currentWeather;          // "Sunny", "LightRain", "HeavyRain"
        private float _fastestLapTime;
        private string _fastestLapPilotId;

        // ── Constantes de simulación ─────────────────────────
        private const float BASE_LAP_TIME = 90f;          // Tiempo base en segundos
        private const float PIT_STOP_TIME = 22f;           // Tiempo de pit stop en segundos
        private const float PIT_STOP_VARIANCE = 3f;        // Variación ±
        private const float SAFETY_CAR_SLOW_FACTOR = 0.65f; // Los autos van al 65% bajo SC
        private const float TIRE_SOFT_SPEED = 1.02f;       // Soft = 2% más rápido
        private const float TIRE_MEDIUM_SPEED = 1.00f;     // Medium = base
        private const float TIRE_HARD_SPEED = 0.98f;       // Hard = 2% más lento
        private const float TIRE_INTER_SPEED = 0.95f;      // Intermedios en seco
        private const float TIRE_WET_SPEED = 0.90f;        // Lluvia en seco
        private const float TIRE_WRONG_COMPOUND_RAIN = 0.80f;// Slicks bajo lluvia
        private const float SOFT_DEG_RATE = 2.5f;          // Deg por vuelta (%)
        private const float MEDIUM_DEG_RATE = 1.5f;
        private const float HARD_DEG_RATE = 0.8f;
        private const float DRS_OVERTAKE_BONUS = 0.015f;   // +1.5% con DRS

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public RaceSimulator(Random rng = null)
        {
            _rng = rng ?? new Random();
            _eventBus = EventBus.Instance;
        }

        // ══════════════════════════════════════════════════════
        // SIMULACIÓN DE CLASIFICACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Simula la clasificación completa (Q1 → Q2 → Q3).
        /// Retorna las posiciones de salida ordenadas.
        /// </summary>
        public List<RaceEntry> SimulateQualifying(CircuitData circuit,
            List<PilotData> pilots, List<TeamData> teams)
        {
            _circuit = circuit;
            _entries = new List<RaceEntry>();

            // Crear entries para cada piloto
            foreach (var pilot in pilots)
            {
                if (pilot.isRetired || pilot.isInjured) continue;

                var team = teams.Find(t => t.id == pilot.currentTeamId);
                if (team == null) continue;

                var behavior = new PilotBehavior(pilot, _rng);
                float circuitMod = circuit.GetCarModifier(team);

                // Rendimiento de clasificación
                float qualiPerf = behavior.CalculateQualifyingPerformance(
                    team.carPerformance, circuitMod);

                // Convertir a tiempo de vuelta (menor = mejor)
                float lapTime = ConvertPerformanceToLapTime(qualiPerf);

                var entry = new RaceEntry
                {
                    Pilot = pilot,
                    Team = team,
                    Behavior = behavior,
                    PerformanceRating = qualiPerf,
                    BestLapTime = lapTime,
                    CurrentTire = "Soft",
                    TireCondition = 100f,
                    PitStops = 0,
                    PitStopLaps = new List<int>(),
                    HasDNF = false,
                    OvertakesMade = 0,
                    MotorMode = "Normal",
                    FuelLevel = 100f
                };

                _entries.Add(entry);
            }

            // Simular Q1 → elimina los 5 más lentos
            SimulateQualifyingSession(_entries, "Q1");

            // Q2 → los top 15, elimina los 5 más lentos
            if (_entries.Count > 10)
            {
                var q2Entries = _entries.GetRange(0, Math.Min(15, _entries.Count));
                SimulateQualifyingSession(q2Entries, "Q2");
            }

            // Q3 → los top 10 definen el orden final
            if (_entries.Count > 5)
            {
                var q3Entries = _entries.GetRange(0, Math.Min(10, _entries.Count));
                SimulateQualifyingSession(q3Entries, "Q3");
            }

            // Ordenar por mejor tiempo y asignar posiciones
            _entries.Sort((a, b) => a.BestLapTime.CompareTo(b.BestLapTime));

            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].GridPosition = i + 1;
                _entries[i].CurrentPosition = i + 1;
            }

            // Disparar evento
            var gridPositions = new List<EventBus.RacePositionInfo>();
            foreach (var e in _entries)
            {
                gridPositions.Add(new EventBus.RacePositionInfo
                {
                    Position = e.GridPosition,
                    PilotId = e.Pilot.id,
                    TeamId = e.Team.id,
                    PointsEarned = 0,
                    DNF = false
                });
            }

            _eventBus.FireQualifyingFinished(new EventBus.QualifyingFinishedArgs
            {
                CircuitId = circuit.id,
                RoundNumber = circuit.roundNumber,
                Grid = gridPositions,
                PoleSitterId = _entries.Count > 0 ? _entries[0].Pilot.id : ""
            });

            return _entries;
        }

        /// <summary>
        /// Simula una sesión de clasificación (mejora el mejor tiempo)
        /// </summary>
        private void SimulateQualifyingSession(List<RaceEntry> sessionEntries, string session)
        {
            foreach (var entry in sessionEntries)
            {
                float circuitMod = _circuit.GetCarModifier(entry.Team);

                // 2 intentos por sesión
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    float perf = entry.Behavior.CalculateQualifyingPerformance(
                        entry.Team.carPerformance, circuitMod);
                    float lapTime = ConvertPerformanceToLapTime(perf);

                    // Si es mejor que el anterior, actualizar
                    if (lapTime < entry.BestLapTime)
                        entry.BestLapTime = lapTime;
                }
            }

            // Reordenar
            _entries.Sort((a, b) => a.BestLapTime.CompareTo(b.BestLapTime));
        }

        // ══════════════════════════════════════════════════════
        // SIMULACIÓN DE CARRERA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Simula una carrera completa vuelta por vuelta.
        /// Requiere que SimulateQualifying se haya ejecutado antes para tener el grid.
        /// </summary>
        /// <param name="gridEntries">Entries ordenados por posición de salida</param>
        /// <param name="playerTireStrategy">Compuesto elegido por el jugador para su equipo</param>
        /// <returns>RaceResultData con resultados completos</returns>
        public RaceResultData SimulateRace(List<RaceEntry> gridEntries,
            string playerTireStrategy = "Medium")
        {
            _entries = gridEntries;
            _incidents = new List<RaceIncident>();
            _fastestLapTime = float.MaxValue;
            _fastestLapPilotId = "";
            _isSafetyCarActive = false;
            _isVSCActive = false;
            _isRedFlagged = false;
            _currentWeather = "Sunny";

            // Inicializar cada entry para la carrera
            foreach (var entry in _entries)
            {
                entry.CumulativeTime = 0f;
                entry.PitStops = 0;
                entry.PitStopLaps = new List<int>();
                entry.HasDNF = false;
                entry.OvertakesMade = 0;
                entry.GapToLeader = 0f;
                entry.FuelLevel = 100f;
                entry.MotorMode = "Normal";
                entry.HasFastestLap = false;
                entry.PointsEarned = 0;
                entry.LapTime = 0f;

                // Asignar neumáticos iniciales (IA para rivales)
                entry.CurrentTire = entry.Team.isPlayerControlled
                    ? playerTireStrategy : ChooseStartingTire(entry);
                entry.TireCondition = 100f;
                entry.TireDegPerLap = GetBaseDegradationRate(entry.CurrentTire);
            }

            // ── Simular salida ───────────────────────────────
            SimulateStart();

            // ── Simular vuelta por vuelta ────────────────────
            for (int lap = 1; lap <= _circuit.totalLaps; lap++)
            {
                if (_isRedFlagged) break;

                // Verificar cambio de clima
                CheckWeatherChange(lap);

                // Gestionar Safety Car / VSC
                UpdateSafetyCarState();

                // Generar eventos aleatorios (cada ~5 vueltas)
                if (lap % 5 == 0 || lap == 1)
                    GenerateRaceEvents(lap);

                // Simular la vuelta para cada piloto
                foreach (var entry in _entries)
                {
                    if (entry.HasDNF) continue;

                    SimulateLap(entry, lap);
                }

                // Verificar fallo mecánico por fiabilidad
                CheckMechanicalFailures(lap);

                // Verificar si alguien necesita pit stop (IA)
                CheckAIPitStops(lap);

                // Actualizar posiciones
                UpdatePositions();
            }

            // ── Calcular resultados finales ──────────────────
            return BuildRaceResult();
        }

        // ══════════════════════════════════════════════════════
        // SIMULACIÓN POR VUELTA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Simula la salida/primera vuelta (posiciones pueden cambiar mucho)
        /// </summary>
        private void SimulateStart()
        {
            // Calcular rendimiento de salida para cada piloto
            var startPerformances = new List<(RaceEntry entry, float perf)>();

            foreach (var entry in _entries)
            {
                if (entry.HasDNF) continue;

                float startPerf = entry.Behavior.CalculateStartPerformance();
                startPerformances.Add((entry, startPerf));
            }

            // Ordenar por rendimiento de salida (mayor = mejor)
            startPerformances.Sort((a, b) => b.perf.CompareTo(a.perf));

            // Aplicar cambios de posición por la salida (máximo ±3 posiciones)
            for (int i = 0; i < startPerformances.Count; i++)
            {
                int originalPos = startPerformances[i].entry.GridPosition;
                int startDelta = originalPos - (i + 1);
                int maxChange = 3;

                int newPos = originalPos - Clamp(startDelta, -maxChange, maxChange);
                startPerformances[i].entry.CurrentPosition = Clamp(newPos, 1, _entries.Count);
            }

            // Posibilidad de choque en la salida (10%)
            if ((float)_rng.NextDouble() < 0.10f)
            {
                // Un piloto aleatorio de las posiciones medias se ve afectado
                int victimIndex = _rng.Next(_entries.Count / 3, _entries.Count);
                if (victimIndex < _entries.Count)
                {
                    var victim = _entries[victimIndex];
                    victim.HasDNF = true;
                    victim.DNFReason = "Choque en la salida";
                    victim.DNFLap = 1;

                    _incidents.Add(new RaceIncident
                    {
                        Lap = 1,
                        Type = "Crash",
                        Description = $"{victim.Pilot.firstName} {victim.Pilot.lastName} se retiró tras un incidente en la salida",
                        InvolvedPilotIds = new List<string> { victim.Pilot.id },
                        DurationLaps = 0
                    });
                }
            }

            UpdatePositions();
        }

        /// <summary>
        /// Simula una vuelta individual para un piloto
        /// </summary>
        private void SimulateLap(RaceEntry entry, int currentLap)
        {
            float circuitMod = _circuit.GetCarModifier(entry.Team);
            bool isWet = _isRaining;

            // Calcular rendimiento base de la vuelta
            float performance;
            if (isWet)
                performance = entry.Behavior.CalculateWetPerformance(
                    entry.Team.carPerformance, circuitMod);
            else
                performance = entry.Behavior.CalculateRacePerformance(
                    entry.Team.carPerformance, circuitMod);

            // Modificador de neumáticos
            float tireSpeed = GetTireSpeedModifier(entry.CurrentTire, isWet);
            float tireDegMod = 1f - ((100f - entry.TireCondition) / 200f);

            // Degradación de neumáticos por vuelta
            float pilotDegRate = entry.Behavior.GetTireDegradationRate();
            float circuitDegMod = GetCircuitDegradationModifier();
            entry.TireCondition -= entry.TireDegPerLap * pilotDegRate * circuitDegMod;
            if (entry.TireCondition < 0) entry.TireCondition = 0;

            // Consumo de combustible
            entry.FuelLevel -= (100f / _circuit.totalLaps);
            float fuelMod = 1f + ((100f - entry.FuelLevel) / 100f * 0.005f);

            // Si hay Safety Car, todos van más lento
            float scFactor = _isSafetyCarActive ? SAFETY_CAR_SLOW_FACTOR : 1f;
            float vscFactor = _isVSCActive ? 0.80f : 1f;

            // Calcular tiempo de vuelta
            float lapTime = ConvertPerformanceToLapTime(
                performance * tireSpeed * tireDegMod * fuelMod * scFactor * vscFactor);

            entry.LapTime = lapTime;
            entry.CumulativeTime += lapTime;

            // Verificar vuelta rápida
            if (lapTime < _fastestLapTime && !_isSafetyCarActive && !_isVSCActive)
            {
                _fastestLapTime = lapTime;
                // Quitar la marca al anterior
                foreach (var e in _entries)
                    e.HasFastestLap = false;
                entry.HasFastestLap = true;
                _fastestLapPilotId = entry.Pilot.id;
            }

            // Verificar error del piloto
            if (entry.Behavior.RollForError(currentLap, _circuit.totalLaps))
            {
                // Error = pierde tiempo (trompo, excursión, etc.)
                float timeLost = (float)_rng.NextDouble() * 5f + 2f; // 2-7 segundos
                entry.CumulativeTime += timeLost;

                // 15% de chance de que el error cause abandono
                if ((float)_rng.NextDouble() < 0.15f)
                {
                    entry.HasDNF = true;
                    entry.DNFReason = "Error del piloto";
                    entry.DNFLap = currentLap;

                    _incidents.Add(new RaceIncident
                    {
                        Lap = currentLap,
                        Type = "Crash",
                        Description = $"{entry.Pilot.firstName} {entry.Pilot.lastName} cometió un error y abandonó",
                        InvolvedPilotIds = new List<string> { entry.Pilot.id },
                        DurationLaps = 0
                    });
                }
            }

            // Intentar adelantamientos (si no hay SC)
            if (!_isSafetyCarActive && !_isVSCActive)
            {
                TryOvertakes(entry, currentLap);
            }

            // Actualizar mejor vuelta personal
            if (lapTime < entry.BestLapTime || entry.BestLapTime == 0)
                entry.BestLapTime = lapTime;
        }

        // ══════════════════════════════════════════════════════
        // ADELANTAMIENTOS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Intenta adelantamientos con el auto de adelante
        /// </summary>
        private void TryOvertakes(RaceEntry attacker, int currentLap)
        {
            if (attacker.HasDNF || attacker.CurrentPosition == 1) return;

            // Encontrar al piloto de adelante
            RaceEntry defender = null;
            foreach (var e in _entries)
            {
                if (!e.HasDNF && e.CurrentPosition == attacker.CurrentPosition - 1)
                {
                    defender = e;
                    break;
                }
            }
            if (defender == null) return;

            // Gap de rendimiento
            float performanceGap = attacker.PerformanceRating - defender.PerformanceRating;

            // Bonus DRS (si el circuito tiene zonas DRS)
            float drsBonus = _circuit.drsZones > 0 ? DRS_OVERTAKE_BONUS * _circuit.drsZones : 0f;
            performanceGap += drsBonus;

            // ¿El atacante decide intentar?
            if (attacker.Behavior.DecideOvertakeAttempt(performanceGap, _circuit.overtakingDifficulty))
            {
                // ¿El defensor defiende agresivamente?
                bool aggressiveDefense = defender.Behavior.DecideAggressiveDefense();

                // Probabilidad de éxito del adelantamiento
                float successChance = 0.30f + (performanceGap * 0.5f);
                if (aggressiveDefense) successChance -= 0.15f;

                successChance = ClampF(successChance, 0.05f, 0.85f);

                if ((float)_rng.NextDouble() < successChance)
                {
                    // ¡Adelantamiento exitoso!
                    int oldPos = attacker.CurrentPosition;
                    attacker.CurrentPosition = defender.CurrentPosition;
                    defender.CurrentPosition = oldPos;
                    attacker.OvertakesMade++;

                    _incidents.Add(new RaceIncident
                    {
                        Lap = currentLap,
                        Type = "Overtake",
                        Description = $"{attacker.Pilot.lastName} adelantó a {defender.Pilot.lastName}",
                        InvolvedPilotIds = new List<string>
                            { attacker.Pilot.id, defender.Pilot.id },
                        DurationLaps = 0
                    });
                }
                else if (aggressiveDefense && (float)_rng.NextDouble() < 0.08f)
                {
                    // Intento fallido → posible choque entre ambos
                    _incidents.Add(new RaceIncident
                    {
                        Lap = currentLap,
                        Type = "Crash",
                        Description = $"Contacto entre {attacker.Pilot.lastName} y {defender.Pilot.lastName}",
                        InvolvedPilotIds = new List<string>
                            { attacker.Pilot.id, defender.Pilot.id },
                        DurationLaps = 0
                    });

                    // 50/50 quién se retira
                    if ((float)_rng.NextDouble() < 0.5f)
                    {
                        attacker.HasDNF = true;
                        attacker.DNFReason = "Choque con " + defender.Pilot.lastName;
                        attacker.DNFLap = currentLap;
                    }
                    else
                    {
                        defender.HasDNF = true;
                        defender.DNFReason = "Choque con " + attacker.Pilot.lastName;
                        defender.DNFLap = currentLap;
                    }
                }
            }
        }

        // ══════════════════════════════════════════════════════
        // EVENTOS DE CARRERA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera eventos aleatorios durante la carrera
        /// </summary>
        private void GenerateRaceEvents(int currentLap)
        {
            // Safety Car
            if (!_isSafetyCarActive && !_isVSCActive &&
                (float)_rng.NextDouble() < Constants.RACE_SAFETY_CAR_CHANCE * 0.2f)
            {
                _isSafetyCarActive = true;
                _safetyCarLapsRemaining = _rng.Next(2, 5);

                _incidents.Add(new RaceIncident
                {
                    Lap = currentLap,
                    Type = "SafetyCar",
                    Description = "¡Safety Car en pista!",
                    InvolvedPilotIds = new List<string>(),
                    DurationLaps = _safetyCarLapsRemaining
                });
            }

            // Virtual Safety Car
            if (!_isSafetyCarActive && !_isVSCActive &&
                (float)_rng.NextDouble() < Constants.RACE_VSC_CHANCE * 0.2f)
            {
                _isVSCActive = true;
                _vscLapsRemaining = _rng.Next(2, 4);

                _incidents.Add(new RaceIncident
                {
                    Lap = currentLap,
                    Type = "VirtualSafetyCar",
                    Description = "Virtual Safety Car desplegado",
                    InvolvedPilotIds = new List<string>(),
                    DurationLaps = _vscLapsRemaining
                });
            }

            // Bandera roja (muy raro)
            if ((float)_rng.NextDouble() < Constants.RACE_RED_FLAG_CHANCE * 0.1f)
            {
                _isRedFlagged = true;
                _incidents.Add(new RaceIncident
                {
                    Lap = currentLap,
                    Type = "RedFlag",
                    Description = "¡Bandera roja! La carrera se detiene",
                    InvolvedPilotIds = new List<string>(),
                    DurationLaps = 0
                });
            }
        }

        /// <summary>
        /// Verifica si el clima cambia durante la carrera
        /// </summary>
        private void CheckWeatherChange(int currentLap)
        {
            // Solo verificar cada ~10 vueltas
            if (currentLap % 10 != 0) return;

            float rainChance = _circuit.rainChance;

            if (!_isRaining && (float)_rng.NextDouble() < rainChance * 0.3f)
            {
                _isRaining = true;
                _currentWeather = (float)_rng.NextDouble() < 0.6f ? "LightRain" : "HeavyRain";

                _incidents.Add(new RaceIncident
                {
                    Lap = currentLap,
                    Type = "Rain",
                    Description = _currentWeather == "HeavyRain"
                        ? "¡Lluvia intensa! Condiciones peligrosas"
                        : "Empieza a llover ligeramente",
                    InvolvedPilotIds = new List<string>(),
                    DurationLaps = _rng.Next(5, 20)
                });
            }
            else if (_isRaining && (float)_rng.NextDouble() < 0.3f)
            {
                _isRaining = false;
                _currentWeather = "Sunny";

                _incidents.Add(new RaceIncident
                {
                    Lap = currentLap,
                    Type = "Rain",
                    Description = "La lluvia se ha detenido, la pista se seca gradualmente",
                    InvolvedPilotIds = new List<string>(),
                    DurationLaps = 0
                });
            }
        }

        /// <summary>
        /// Actualiza el estado de Safety Car / VSC
        /// </summary>
        private void UpdateSafetyCarState()
        {
            if (_isSafetyCarActive)
            {
                _safetyCarLapsRemaining--;
                if (_safetyCarLapsRemaining <= 0)
                    _isSafetyCarActive = false;
            }

            if (_isVSCActive)
            {
                _vscLapsRemaining--;
                if (_vscLapsRemaining <= 0)
                    _isVSCActive = false;
            }
        }

        /// <summary>
        /// Verifica fallos mecánicos según la fiabilidad del equipo
        /// </summary>
        private void CheckMechanicalFailures(int currentLap)
        {
            foreach (var entry in _entries)
            {
                if (entry.HasDNF) continue;

                // Probabilidad base = (100 - fiabilidad) / 5000
                float failChance = (100f - entry.Team.reliabilityRating) / 5000f;

                // Aumenta en las últimas vueltas
                if (currentLap > _circuit.totalLaps * 0.7f)
                    failChance *= 1.5f;

                if ((float)_rng.NextDouble() < failChance)
                {
                    entry.HasDNF = true;
                    string[] reasons = { "Fallo de motor", "Problema de caja de cambios",
                        "Fallo hidráulico", "Problema eléctrico", "Fallo de frenos" };
                    entry.DNFReason = reasons[_rng.Next(reasons.Length)];
                    entry.DNFLap = currentLap;

                    _incidents.Add(new RaceIncident
                    {
                        Lap = currentLap,
                        Type = "MechanicalFailure",
                        Description = $"{entry.Pilot.lastName} se retiró por {entry.DNFReason}",
                        InvolvedPilotIds = new List<string> { entry.Pilot.id },
                        DurationLaps = 0
                    });
                }
            }
        }

        // ══════════════════════════════════════════════════════
        // PIT STOPS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// La IA decide si hace pit stop (basado en estado de neumáticos)
        /// </summary>
        private void CheckAIPitStops(int currentLap)
        {
            foreach (var entry in _entries)
            {
                if (entry.HasDNF || entry.Team.isPlayerControlled) continue;

                bool shouldPit = false;

                // Bajo SC es buen momento para parar
                if (_isSafetyCarActive && entry.TireCondition < 40 && entry.PitStops < 3)
                    shouldPit = true;

                // Neumáticos muy gastados
                if (entry.TireCondition < 15)
                    shouldPit = true;

                // Cambio a lluvia (necesita intermedios)
                if (_isRaining && entry.CurrentTire != "Intermediate" && entry.CurrentTire != "Wet")
                    shouldPit = true;

                // Pista se secó (necesita slicks)
                if (!_isRaining && (entry.CurrentTire == "Intermediate" || entry.CurrentTire == "Wet"))
                    shouldPit = true;

                if (shouldPit)
                    ExecutePitStop(entry, currentLap);
            }
        }

        /// <summary>
        /// Ejecuta un pit stop para un piloto específico
        /// </summary>
        public void ExecutePitStop(RaceEntry entry, int currentLap)
        {
            if (entry.HasDNF) return;

            // Tiempo de pit stop (afectado por el rating del equipo)
            float pitQuality = entry.Team.pitStopSpeed / 100f;
            float pitTime = PIT_STOP_TIME - (pitQuality * 3f);
            pitTime += ((float)_rng.NextDouble() * PIT_STOP_VARIANCE * 2f - PIT_STOP_VARIANCE);

            // Pit stop fallido (raro)
            if ((float)_rng.NextDouble() < 0.03f)
            {
                pitTime += _rng.Next(3, 10); // Tiempo extra
                _incidents.Add(new RaceIncident
                {
                    Lap = currentLap,
                    Type = "PitStop",
                    Description = $"¡Pit stop lento para {entry.Pilot.lastName}! Problemas en el garaje",
                    InvolvedPilotIds = new List<string> { entry.Pilot.id },
                    DurationLaps = 0
                });
            }

            entry.CumulativeTime += pitTime;
            entry.PitStops++;
            entry.PitStopLaps.Add(currentLap);

            // Elegir nuevo compuesto
            if (_isRaining)
            {
                entry.CurrentTire = _currentWeather == "HeavyRain" ? "Wet" : "Intermediate";
            }
            else
            {
                // Elegir según posición en carrera y vueltas restantes
                int lapsRemaining = _circuit.totalLaps - currentLap;
                if (lapsRemaining < 15)
                    entry.CurrentTire = "Soft";
                else if (lapsRemaining < 30)
                    entry.CurrentTire = "Medium";
                else
                    entry.CurrentTire = "Hard";
            }

            entry.TireCondition = 100f;
            entry.TireDegPerLap = GetBaseDegradationRate(entry.CurrentTire);
        }

        // ══════════════════════════════════════════════════════
        // POSICIONES Y RESULTADOS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Actualiza las posiciones según el tiempo acumulado
        /// </summary>
        private void UpdatePositions()
        {
            // Separar DNFs y activos
            var active = new List<RaceEntry>();
            var dnfs = new List<RaceEntry>();

            foreach (var e in _entries)
            {
                if (e.HasDNF) dnfs.Add(e);
                else active.Add(e);
            }

            // Ordenar activos por tiempo acumulado (menor = primero)
            active.Sort((a, b) => a.CumulativeTime.CompareTo(b.CumulativeTime));

            // Asignar posiciones
            for (int i = 0; i < active.Count; i++)
            {
                active[i].CurrentPosition = i + 1;
                active[i].GapToLeader = active[i].CumulativeTime - active[0].CumulativeTime;
            }

            // DNFs al final, ordenados por vuelta de abandono (más tarde = mejor posición)
            dnfs.Sort((a, b) => b.DNFLap.CompareTo(a.DNFLap));
            for (int i = 0; i < dnfs.Count; i++)
            {
                dnfs[i].CurrentPosition = active.Count + i + 1;
            }

            // Actualizar performance rating actual
            foreach (var e in active)
            {
                float circuitMod = _circuit.GetCarModifier(e.Team);
                e.PerformanceRating = e.Behavior.CalculateRacePerformance(
                    e.Team.carPerformance, circuitMod);
            }
        }

        /// <summary>
        /// Construye el resultado final de la carrera y dispara el evento
        /// </summary>
        private RaceResultData BuildRaceResult()
        {
            // Actualizar posiciones finales
            UpdatePositions();

            // Asignar puntos
            foreach (var entry in _entries)
            {
                if (!entry.HasDNF && entry.CurrentPosition <= 10)
                {
                    entry.PointsEarned = Constants.POINTS_BY_POSITION[entry.CurrentPosition - 1];

                    // Punto extra por vuelta rápida (solo si está en top 10)
                    if (entry.HasFastestLap)
                        entry.PointsEarned += Constants.FASTEST_LAP_POINT;
                }
            }

            // Construir posiciones para el resultado
            var finalPositions = new List<RacePositionData>();
            var eventPositions = new List<EventBus.RacePositionInfo>();
            int totalOvertakes = 0;
            int safetyCars = 0;

            foreach (var entry in _entries)
            {
                totalOvertakes += entry.OvertakesMade;

                var posData = new RacePositionData
                {
                    position = entry.CurrentPosition,
                    pilotId = entry.Pilot.id,
                    teamId = entry.Team.id,
                    pitStops = entry.PitStops,
                    gapToLeader = entry.GapToLeader,
                    pointsEarned = entry.PointsEarned,
                    hasFastestLap = entry.HasFastestLap,
                    dnf = entry.HasDNF,
                    dnfReason = entry.DNFReason ?? ""
                };
                finalPositions.Add(posData);

                eventPositions.Add(new EventBus.RacePositionInfo
                {
                    Position = entry.CurrentPosition,
                    PilotId = entry.Pilot.id,
                    TeamId = entry.Team.id,
                    PointsEarned = entry.PointsEarned,
                    DNF = entry.HasDNF,
                    DNFReason = entry.DNFReason ?? ""
                });
            }

            // Contar safety cars
            foreach (var inc in _incidents)
            {
                if (inc.Type == "SafetyCar") safetyCars++;
            }

            // Generar descripciones de incidentes
            var incidentDescriptions = new List<string>();
            foreach (var inc in _incidents)
                incidentDescriptions.Add(inc.Description);

            // Construir resultado
            var result = new RaceResultData
            {
                circuitId = _circuit.id,
                roundNumber = _circuit.roundNumber,
                finalPositions = finalPositions,
                fastestLapPilotId = _fastestLapPilotId,
                fastestLapTime = _fastestLapTime,
                totalOvertakes = totalOvertakes,
                safetyCars = safetyCars,
                hadRain = _isRaining || _incidents.Exists(i => i.Type == "Rain"),
                incidentDescriptions = incidentDescriptions,
                narratorSummary = new List<string>() // Se llenará por el NarratorAI
            };

            // Encontrar al ganador
            string winnerId = "";
            foreach (var entry in _entries)
            {
                if (entry.CurrentPosition == 1 && !entry.HasDNF)
                {
                    winnerId = entry.Pilot.id;
                    break;
                }
            }

            // Disparar evento de carrera terminada
            _eventBus.FireRaceFinished(new EventBus.RaceFinishedArgs
            {
                CircuitId = _circuit.id,
                RoundNumber = _circuit.roundNumber,
                WinnerId = winnerId,
                FastestLapId = _fastestLapPilotId,
                FinalPositions = eventPositions,
                Incidents = incidentDescriptions,
                HadRain = result.hadRain,
                SafetyCars = safetyCars
            });

            // Procesar resultados en el mood de cada piloto
            ProcessRaceResultsForMood();

            return result;
        }

        /// <summary>
        /// Después de la carrera, actualiza el humor de cada piloto según su resultado
        /// </summary>
        private void ProcessRaceResultsForMood()
        {
            foreach (var entry in _entries)
            {
                // Encontrar al compañero de equipo
                RaceEntry teammate = null;
                foreach (var e in _entries)
                {
                    if (e.Pilot.id != entry.Pilot.id && e.Team.id == entry.Team.id)
                    {
                        teammate = e;
                        break;
                    }
                }

                int teammatePos = teammate?.CurrentPosition ?? entry.CurrentPosition;
                bool teammateDnf = teammate?.HasDNF ?? false;

                entry.Behavior.Mood.ProcessRaceResult(
                    entry.CurrentPosition,
                    entry.HasDNF,
                    entry.DNFReason ?? "",
                    teammatePos,
                    teammateDnf
                );
            }
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES DE NEUMÁTICOS
        // ══════════════════════════════════════════════════════

        private string ChooseStartingTire(RaceEntry entry)
        {
            // IA simple: equipos top prefieren Medium, otros Soft
            if (entry.Team.carPerformance >= 85)
                return "Medium";
            return "Soft";
        }

        private float GetBaseDegradationRate(string tireCompound)
        {
            switch (tireCompound)
            {
                case "Soft": return SOFT_DEG_RATE;
                case "Medium": return MEDIUM_DEG_RATE;
                case "Hard": return HARD_DEG_RATE;
                case "Intermediate": return MEDIUM_DEG_RATE;
                case "Wet": return HARD_DEG_RATE;
                default: return MEDIUM_DEG_RATE;
            }
        }

        private float GetTireSpeedModifier(string compound, bool isRaining)
        {
            if (isRaining)
            {
                switch (compound)
                {
                    case "Wet": return 1.0f;
                    case "Intermediate": return 0.97f;
                    default: return TIRE_WRONG_COMPOUND_RAIN;
                }
            }

            switch (compound)
            {
                case "Soft": return TIRE_SOFT_SPEED;
                case "Medium": return TIRE_MEDIUM_SPEED;
                case "Hard": return TIRE_HARD_SPEED;
                case "Intermediate": return TIRE_INTER_SPEED;
                case "Wet": return TIRE_WET_SPEED;
                default: return TIRE_MEDIUM_SPEED;
            }
        }

        private float GetCircuitDegradationModifier()
        {
            switch (_circuit.tireDegradation)
            {
                case "VeryLow": return 0.6f;
                case "Low": return 0.8f;
                case "Medium": return 1.0f;
                case "High": return 1.3f;
                case "VeryHigh": return 1.6f;
                default: return 1.0f;
            }
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES GENERALES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Convierte un rating de rendimiento a tiempo de vuelta en segundos
        /// Mayor rendimiento = menor tiempo
        /// </summary>
        private float ConvertPerformanceToLapTime(float performance)
        {
            // Performance de ~80 → ~90 segundos (base)
            // Cada punto de performance reduce ~0.1 segundos
            float lapTime = BASE_LAP_TIME - (performance - 70f) * 0.12f;

            // Agregar variación mínima
            lapTime += ((float)_rng.NextDouble() * 0.4f - 0.2f);

            return Math.Max(lapTime, 60f); // Mínimo 60 segundos
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private float ClampF(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
