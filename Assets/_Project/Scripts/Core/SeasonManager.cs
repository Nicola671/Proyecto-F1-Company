// ============================================================
// F1 Career Manager — SeasonManager.cs
// Director de orquesta de toda la temporada
// ============================================================
// DEPENDENCIAS: CalendarManager, todos los subsistemas,
//               EventBus, GameManager
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Data;

namespace F1CareerManager.Core
{
    /// <summary>
    /// Estado de la temporada
    /// </summary>
    public enum SeasonState
    {
        PreSeason,
        RaceWeek,
        BetweenRaces,
        PostSeason,
        SummerBreak
    }

    /// <summary>
    /// Orquesta el flujo completo de la temporada.
    /// Cada semana llama los subsistemas en orden correcto.
    /// Post-carrera ejecuta la cadena de procesamiento.
    /// Fin de temporada cierra y prepara la siguiente.
    /// </summary>
    public class SeasonManager
    {
        // ── Estado ───────────────────────────────────────────
        private SeasonState _state;
        private CalendarManager _calendar;
        private EventBus _eventBus;
        private Random _rng;

        public SeasonState State => _state;

        // ── Referencia a subsistemas (se inyectan) ───────────
        // Usamos interfaces implícitas (acceso por delegado)
        // para no tener acoplamiento duro
        public Action WeeklyStaffUpdate;
        public Action WeeklyStaffEvents;
        public Action WeeklyTransferAdvance;
        public Action WeeklyRumorGeneration;
        public Action WeeklyRandomEvent;
        public Func<bool> WeeklyShouldPressConference;
        public Action StartOfSeasonRegenCheck;

        // Post-carrera
        public Func<string, object> SimulateRace;       // circuitId → result
        public Action<string> EvaluateCrashInjury;       // circuitId
        public Action AdvanceRecovery;
        public Action<string> ProcessRaceRewards;        // circuitId
        public Action<string> PostRaceRegCheck;          // circuitId
        public Action<string> UpdateMoodPostRace;        // circuitId
        public Action<string> GeneratePostRaceNews;      // circuitId
        public Action UpdateLegacyAfterRace;

        // Fin de temporada
        public Action ProcessSeasonEndRewards;
        public Action EvaluateSponsors;
        public Action GenerateNewPilots;
        public Action EvaluateObjectives;
        public Action UpdateLegacyAfterSeason;

        // Callbacks a GameManager / UI
        public Action<SeasonState> OnStateChanged;
        public Action<string> OnRaceReady;               // circuitId
        public Action OnSeasonComplete;
        public Action OnPressConferenceTriggered;

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public SeasonManager(CalendarManager calendar)
        {
            _calendar = calendar;
            _eventBus = EventBus.Instance;
            _rng = new Random();
            _state = SeasonState.PreSeason;
        }

        // ══════════════════════════════════════════════════════
        // INICIO DE TEMPORADA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Inicia una nueva temporada.
        /// Genera calendario, regen check, y pasa a BetweenRaces.
        /// </summary>
        public void StartSeason(List<CircuitData> circuits)
        {
            ChangeState(SeasonState.PreSeason);

            _calendar.GenerateCalendar(_calendar.CurrentSeason + 1, circuits);

            // Check de nuevos regens (solo inicio de temporada)
            StartOfSeasonRegenCheck?.Invoke();

            Log($"Temporada {_calendar.CurrentSeason} iniciada — " +
                $"{circuits?.Count ?? 0} carreras");

            // Pasar a entre carreras
            ChangeState(SeasonState.BetweenRaces);
        }

        // ══════════════════════════════════════════════════════
        // AVANCE SEMANAL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Avanza una semana. Llama subsistemas en orden.
        /// Retorna true si hay evento que requiere input del jugador.
        /// </summary>
        public bool AdvanceWeek()
        {
            _calendar.AdvanceWeek();

            Log($"Semana {_calendar.CurrentWeek} — {_calendar.GetStatusSummary()}");

            // ¿Pausa de verano?
            if (_calendar.IsSummerBreak())
            {
                ChangeState(SeasonState.SummerBreak);
                Log("☀️ Pausa de verano");
                return false;
            }

            // ¿Es semana de carrera?
            if (_calendar.IsRaceWeek())
            {
                ChangeState(SeasonState.RaceWeek);

                var race = _calendar.GetCurrentRace();
                OnRaceReady?.Invoke(race?.CircuitId);
                return true; // UI debe mostrar pantalla de carrera
            }

            // Semana normal — entre carreras
            ChangeState(SeasonState.BetweenRaces);
            ExecuteWeeklyUpdates();

            return false;
        }

        /// <summary>
        /// Ejecuta todos los updates semanales en orden correcto.
        /// Orden basado en dependencias:
        /// 1. Staff primero (afecta bonos)
        /// 2. Transferencias (pueden generar noticias)
        /// 3. Rumores (usan estado actual)
        /// 4. Eventos aleatorios
        /// 5. Rueda de prensa (si corresponde)
        /// </summary>
        private void ExecuteWeeklyUpdates()
        {
            // 1. Staff
            try { WeeklyStaffUpdate?.Invoke(); }
            catch (Exception e) { LogError($"StaffUpdate: {e.Message}"); }

            // 2. Eventos de staff
            try { WeeklyStaffEvents?.Invoke(); }
            catch (Exception e) { LogError($"StaffEvents: {e.Message}"); }

            // 3. Mercado de transferencias
            try { WeeklyTransferAdvance?.Invoke(); }
            catch (Exception e) { LogError($"TransferAdvance: {e.Message}"); }

            // 4. Rumores
            try { WeeklyRumorGeneration?.Invoke(); }
            catch (Exception e) { LogError($"RumorGen: {e.Message}"); }

            // 5. Eventos aleatorios
            try { WeeklyRandomEvent?.Invoke(); }
            catch (Exception e) { LogError($"RandomEvent: {e.Message}"); }

            // 6. ¿Rueda de prensa?
            try
            {
                bool shouldPress = WeeklyShouldPressConference?.Invoke() ?? false;
                if (shouldPress)
                    OnPressConferenceTriggered?.Invoke();
            }
            catch (Exception e) { LogError($"PressConf: {e.Message}"); }
        }

        // ══════════════════════════════════════════════════════
        // POST-CARRERA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Ejecuta toda la cadena post-carrera en orden.
        /// Llamar DESPUÉS de que RaceScreen termine la simulación.
        /// </summary>
        public void ProcessPostRace(string circuitId)
        {
            Log($"--- Post-carrera: {circuitId} ---");

            // 1. Simular carrera (si no se simuló en RaceScreen)
            try { SimulateRace?.Invoke(circuitId); }
            catch (Exception e) { LogError($"SimulateRace: {e.Message}"); }

            // 2. Evaluar lesiones por choque
            try { EvaluateCrashInjury?.Invoke(circuitId); }
            catch (Exception e) { LogError($"CrashInjury: {e.Message}"); }

            // 3. Avanzar recuperación de lesionados
            try { AdvanceRecovery?.Invoke(); }
            catch (Exception e) { LogError($"Recovery: {e.Message}"); }

            // 4. Procesar premios económicos
            try { ProcessRaceRewards?.Invoke(circuitId); }
            catch (Exception e) { LogError($"RaceRewards: {e.Message}"); }

            // 5. Verificación FIA post-carrera
            try { PostRaceRegCheck?.Invoke(circuitId); }
            catch (Exception e) { LogError($"RegCheck: {e.Message}"); }

            // 6. Actualizar mood de pilotos
            try { UpdateMoodPostRace?.Invoke(circuitId); }
            catch (Exception e) { LogError($"MoodUpdate: {e.Message}"); }

            // 7. Generar noticias post-carrera
            try { GeneratePostRaceNews?.Invoke(circuitId); }
            catch (Exception e) { LogError($"PostRaceNews: {e.Message}"); }

            // 8. Actualizar legado
            try { UpdateLegacyAfterRace?.Invoke(); }
            catch (Exception e) { LogError($"LegacyRace: {e.Message}"); }

            // Marcar carrera completada en calendario
            _calendar.CompleteRace();

            // ¿Fin de temporada?
            if (_calendar.GetRemainingRaces() <= 0)
            {
                ProcessSeasonEnd();
            }
            else
            {
                ChangeState(SeasonState.BetweenRaces);
            }
        }

        // ══════════════════════════════════════════════════════
        // FIN DE TEMPORADA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Ejecuta toda la cadena de fin de temporada.
        /// </summary>
        private void ProcessSeasonEnd()
        {
            ChangeState(SeasonState.PostSeason);
            Log("=== FIN DE TEMPORADA ===");

            // 1. Premios de fin de temporada
            try { ProcessSeasonEndRewards?.Invoke(); }
            catch (Exception e) { LogError($"SeasonRewards: {e.Message}"); }

            // 2. Evaluar sponsors
            try { EvaluateSponsors?.Invoke(); }
            catch (Exception e) { LogError($"Sponsors: {e.Message}"); }

            // 3. Generar pilotos nuevos para siguiente temporada
            try { GenerateNewPilots?.Invoke(); }
            catch (Exception e) { LogError($"NewPilots: {e.Message}"); }

            // 4. Evaluar objetivos
            try { EvaluateObjectives?.Invoke(); }
            catch (Exception e) { LogError($"Objectives: {e.Message}"); }

            // 5. Actualizar legado
            try { UpdateLegacyAfterSeason?.Invoke(); }
            catch (Exception e) { LogError($"LegacySeason: {e.Message}"); }

            // Notificar al juego
            OnSeasonComplete?.Invoke();
        }

        /// <summary>
        /// Prepara la siguiente temporada (llamar después de la UI de fin).
        /// </summary>
        public void PrepareNextSeason(List<CircuitData> circuits)
        {
            _calendar.PrepareNextSeason(circuits);
            ChangeState(SeasonState.PreSeason);

            // Check de nuevos regens
            StartOfSeasonRegenCheck?.Invoke();

            ChangeState(SeasonState.BetweenRaces);
            Log($"Temporada {_calendar.CurrentSeason} preparada");
        }

        // ══════════════════════════════════════════════════════
        // ESTADO
        // ══════════════════════════════════════════════════════

        private void ChangeState(SeasonState newState)
        {
            _state = newState;
            OnStateChanged?.Invoke(newState);
        }

        /// <summary>Info del calendario actual</summary>
        public CalendarManager GetCalendar() => _calendar;

        // ══════════════════════════════════════════════════════
        // LOG
        // ══════════════════════════════════════════════════════

        private void Log(string msg)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            UnityEngine.Debug.Log($"[SeasonManager] {msg}");
            #else
            Console.WriteLine($"[SeasonManager] {msg}");
            #endif
        }

        private void LogError(string msg)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            UnityEngine.Debug.LogError($"[SeasonManager] {msg}");
            #else
            Console.Error.WriteLine($"[SeasonManager] ERROR: {msg}");
            #endif
        }
    }
}
