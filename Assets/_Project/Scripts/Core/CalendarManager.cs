// ============================================================
// F1 Career Manager — CalendarManager.cs
// Gestiona el calendario de 24 carreras por temporada
// ============================================================
// DEPENDENCIAS: DataLoader.cs, CircuitData.cs, Constants.cs
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Data;

namespace F1CareerManager.Core
{
    /// <summary>
    /// Genera y administra el calendario de la temporada.
    /// 24 carreras, gaps reales entre GPs, sprints, pausas.
    /// </summary>
    public class CalendarManager
    {
        // ── Estado ───────────────────────────────────────────
        private List<CalendarEntry> _calendar;
        private int _currentSeason;
        private int _currentWeek;
        private int _currentRound;
        private int _totalWeeksInSeason;

        // Acceso público
        public int CurrentSeason => _currentSeason;
        public int CurrentWeek => _currentWeek;
        public int CurrentRound => _currentRound;
        public int TotalWeeks => _totalWeeksInSeason;
        public IReadOnlyList<CalendarEntry> Calendar => _calendar?.AsReadOnly();

        // ── Sprints reales F1 2025 ───────────────────────────
        private static readonly HashSet<string> SPRINT_CIRCUITS_2025 = new HashSet<string>
        {
            "shanghai", "miami", "spa", "austin", "interlagos", "lusail"
        };

        // ── Pausa de verano ──────────────────────────────────
        private const int SUMMER_BREAK_START = 30;
        private const int SUMMER_BREAK_END = 32;

        // ══════════════════════════════════════════════════════
        // ENTRADA DEL CALENDARIO
        // ══════════════════════════════════════════════════════

        [Serializable]
        public class CalendarEntry
        {
            public int Round;
            public int Week;
            public string CircuitId;
            public string CircuitName;
            public string Country;
            public bool IsSprint;
            public bool IsSummerBreak;
            public bool IsLastRace;
            public int WeeksUntilNext;

            // Sprint otorga puntos extra
            public static readonly int[] SPRINT_POINTS = { 8, 7, 6, 5, 4, 3, 2, 1 };
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN DEL CALENDARIO
        // ══════════════════════════════════════════════════════

        /// <summary>Genera calendario para una temporada</summary>
        public void GenerateCalendar(int season, List<CircuitData> circuits)
        {
            _currentSeason = season;
            _currentWeek = 1;
            _currentRound = 0;
            _calendar = new List<CalendarEntry>();

            if (circuits == null || circuits.Count == 0)
            {
                LogError("No hay circuitos cargados para generar calendario");
                return;
            }

            // Ordenar por roundNumber
            var sorted = new List<CircuitData>(circuits);
            sorted.Sort((a, b) => a.roundNumber.CompareTo(b.roundNumber));

            // Gaps entre carreras (semanas reales F1 2025 aproximados)
            int[] gapWeeks = {
                2, 1, 2, 1, 2, 3, 1, 2, 2, 1, 3, 2,  // Rondas 1-12
                3, 1, 2, 2, 1, 3, 1, 2, 1, 2, 1, 1    // Rondas 13-24
            };

            int weekCounter = 3; // La temporada empieza semana 3

            for (int i = 0; i < sorted.Count && i < Constants.TOTAL_CIRCUITS; i++)
            {
                var circuit = sorted[i];
                bool isSprint = SPRINT_CIRCUITS_2025.Contains(circuit.id);

                // Verificar pausa de verano
                if (weekCounter >= SUMMER_BREAK_START &&
                    weekCounter <= SUMMER_BREAK_END && i > 0)
                {
                    // Insertar semanas de pausa
                    _calendar.Add(new CalendarEntry
                    {
                        Round = -1,
                        Week = SUMMER_BREAK_START,
                        CircuitId = "",
                        CircuitName = "Pausa de Verano",
                        Country = "",
                        IsSummerBreak = true,
                        WeeksUntilNext = SUMMER_BREAK_END - SUMMER_BREAK_START + 1
                    });
                    weekCounter = SUMMER_BREAK_END + 1;
                }

                int gap = i < gapWeeks.Length ? gapWeeks[i] : 2;

                var entry = new CalendarEntry
                {
                    Round = i + 1,
                    Week = weekCounter,
                    CircuitId = circuit.id,
                    CircuitName = circuit.name,
                    Country = circuit.country,
                    IsSprint = isSprint,
                    IsSummerBreak = false,
                    IsLastRace = (i == sorted.Count - 1),
                    WeeksUntilNext = gap
                };

                _calendar.Add(entry);
                weekCounter += gap;
            }

            _totalWeeksInSeason = weekCounter;
        }

        // ══════════════════════════════════════════════════════
        // CONSULTAS
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene la próxima carrera</summary>
        public CalendarEntry GetNextRace()
        {
            if (_calendar == null) return null;

            foreach (var entry in _calendar)
            {
                if (!entry.IsSummerBreak && entry.Round > _currentRound)
                    return entry;
            }
            return null;
        }

        /// <summary>Obtiene la carrera actual (si es semana de carrera)</summary>
        public CalendarEntry GetCurrentRace()
        {
            if (_calendar == null) return null;

            return _calendar.Find(e =>
                !e.IsSummerBreak && e.Week == _currentWeek);
        }

        /// <summary>Semanas hasta la próxima carrera</summary>
        public int GetWeeksUntilRace()
        {
            var next = GetNextRace();
            if (next == null) return -1;
            return next.Week - _currentWeek;
        }

        /// <summary>¿Es semana de carrera?</summary>
        public bool IsRaceWeek()
        {
            return GetCurrentRace() != null;
        }

        /// <summary>¿Estamos en pausa de verano?</summary>
        public bool IsSummerBreak()
        {
            return _currentWeek >= SUMMER_BREAK_START &&
                   _currentWeek <= SUMMER_BREAK_END;
        }

        /// <summary>¿Es sprint weekend?</summary>
        public bool IsSprintWeekend()
        {
            var current = GetCurrentRace();
            return current?.IsSprint ?? false;
        }

        /// <summary>¿Es la última carrera de la temporada?</summary>
        public bool IsLastRace()
        {
            var current = GetCurrentRace();
            return current?.IsLastRace ?? false;
        }

        /// <summary>¿El campeonato está apretado? (3 pilotos a ≤25 pts)</summary>
        public bool IsChampionshipTight(List<PilotData> standings)
        {
            if (standings == null || standings.Count < 3) return false;

            int leaderPoints = standings[0].totalPoints;
            int thirdPoints = standings[2].totalPoints;
            return (leaderPoints - thirdPoints) <= 25;
        }

        /// <summary>Obtiene todas las carreras sprint de la temporada</summary>
        public List<CalendarEntry> GetSprintRaces()
        {
            if (_calendar == null) return new List<CalendarEntry>();
            return _calendar.FindAll(e => e.IsSprint);
        }

        /// <summary>Carreras completadas</summary>
        public int GetCompletedRaces()
        {
            return _currentRound;
        }

        /// <summary>Carreras restantes</summary>
        public int GetRemainingRaces()
        {
            int total = _calendar?.FindAll(e => !e.IsSummerBreak).Count ?? 0;
            return total - _currentRound;
        }

        // ══════════════════════════════════════════════════════
        // AVANCE DE TIEMPO
        // ══════════════════════════════════════════════════════

        /// <summary>Avanza una semana</summary>
        public void AdvanceWeek()
        {
            _currentWeek++;
        }

        /// <summary>Registra que se completó una carrera</summary>
        public void CompleteRace()
        {
            _currentRound++;
        }

        /// <summary>Prepara la siguiente temporada</summary>
        public void PrepareNextSeason(List<CircuitData> circuits)
        {
            _currentSeason++;
            GenerateCalendar(_currentSeason, circuits);
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene resumen del estado actual</summary>
        public string GetStatusSummary()
        {
            var next = GetNextRace();
            string nextRaceStr = next != null
                ? $"{next.CircuitName} (Semana {next.Week})"
                : "Fin de temporada";

            return $"Temporada {_currentSeason} | Semana {_currentWeek} | " +
                   $"Ronda {_currentRound}/{Constants.TOTAL_CIRCUITS} | " +
                   $"Siguiente: {nextRaceStr}";
        }

        private void LogError(string msg)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            UnityEngine.Debug.LogError($"[CalendarManager] {msg}");
            #else
            Console.Error.WriteLine($"[CalendarManager] {msg}");
            #endif
        }
    }
}
