// ============================================================
// F1 Career Manager — LegacyTracker.cs
// Sistema de puntos de legado — Hall of Fame
// ============================================================
// DEPENDENCIAS: Constants.cs, EventBus.cs, GameManager.cs
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Data;

namespace F1CareerManager.Core
{
    /// <summary>
    /// Acumula puntos de legado según logros del GDD.
    /// Guarda historial completo por temporada.
    /// HallOfFame con los mejores momentos.
    /// </summary>
    public class LegacyTracker
    {
        // ── Estado ───────────────────────────────────────────
        private LegacyData _legacy;
        private List<SeasonSummary> _seasonSummaries;
        private GameManager _gm;

        // Acceso público
        public LegacyData Legacy => _legacy;
        public IReadOnlyList<SeasonSummary> SeasonHistory
            => _seasonSummaries?.AsReadOnly();

        // ── Puntos exactos del GDD ───────────────────────────
        private static class LegacyPoints
        {
            public const int CONSTRUCTORS_CHAMP = 500;
            public const int DRIVERS_CHAMP = 300;
            public const int UNDEFEATED_SEASON = 200;
            public const int REGEN_TO_CHAMP = 400;
            public const int LAST_TO_FIRST = 350;
            public const int MONACO_WIN = 100;
            public const int YEAR_BONUS = 50;
            public const int PER_WIN = 10;
            public const int PER_PODIUM = 5;
        }

        // ── Rangos del GDD ───────────────────────────────────
        private static readonly (int min, string rank)[] LEGACY_RANKS = {
            (5000, "Dinastía"),
            (3000, "Leyenda"),
            (2000, "Gran Equipo"),
            (1000, "Fuerza Establecida"),
            (500,  "Contendiente"),
            (0,    "Equipo Nuevo")
        };

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public LegacyTracker(GameManager gm)
        {
            _gm = gm;
            _legacy = gm.Legacy ?? new LegacyData();
            _seasonSummaries = new List<SeasonSummary>();
        }

        /// <summary>Inicializa con datos cargados (save)</summary>
        public void LoadFromSave(LegacyData saved, List<SeasonSummary> summaries)
        {
            _legacy = saved ?? new LegacyData();
            _seasonSummaries = summaries ?? new List<SeasonSummary>();
        }

        // ══════════════════════════════════════════════════════
        // POST-CARRERA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Actualizar legado después de cada carrera.
        /// Llamado por SeasonManager.ProcessPostRace().
        /// </summary>
        public void UpdateAfterRace()
        {
            var playerTeam = _gm.GetPlayerTeam();
            if (playerTeam == null) return;

            string playerTeamId = _gm.PlayerTeamId;

            // Contar victorias y podios del equipo en la última carrera
            // (GameManager ya actualizó los datos en HandleRaceFinished)
        }

        // ══════════════════════════════════════════════════════
        // FIN DE TEMPORADA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Actualizar legado al terminar la temporada.
        /// Evalúa todos los logros posibles.
        /// </summary>
        public void UpdateAfterSeason()
        {
            var playerTeam = _gm.GetPlayerTeam();
            if (playerTeam == null) return;

            string playerTeamId = _gm.PlayerTeamId;
            var driverChamp = _gm.GetDriverChampion();
            var constructorChamp = _gm.GetConstructorChampion();
            var drivers = _gm.GetDriverStandings();

            int pointsThisSeason = 0;
            var moments = new List<string>();

            // 1. Campeonato de constructores (+500)
            if (constructorChamp?.id == playerTeamId)
            {
                pointsThisSeason += LegacyPoints.CONSTRUCTORS_CHAMP;
                _legacy.constructorChampionships++;
                moments.Add($"🏆 ¡Campeón de Constructores Temporada {_gm.CurrentSeason}!");
            }

            // 2. Campeonato de pilotos (+300)
            if (driverChamp?.currentTeamId == playerTeamId)
            {
                pointsThisSeason += LegacyPoints.DRIVERS_CHAMP;
                _legacy.driverChampionships++;
                moments.Add($"🏆 {driverChamp.firstName} {driverChamp.lastName} " +
                    $"campeón del mundo T{_gm.CurrentSeason}!");

                // 3. Regen a campeón (+400)
                if (driverChamp.isRegen)
                {
                    pointsThisSeason += LegacyPoints.REGEN_TO_CHAMP;
                    _legacy.regensToChampion++;
                    moments.Add($"🌟 ¡Desarrollaste a {driverChamp.lastName} " +
                        "de la academia al campeonato!");
                }
            }

            // 4. Temporada invicta (+200)
            if (IsUndefeatedSeason(playerTeamId))
            {
                pointsThisSeason += LegacyPoints.UNDEFEATED_SEASON;
                moments.Add("🔥 ¡Temporada invicta — ganaste TODAS las carreras!");
            }

            // 5. Remontar de último a primero (+350)
            if (IsLastToFirst(playerTeamId))
            {
                pointsThisSeason += LegacyPoints.LAST_TO_FIRST;
                moments.Add("📈 ¡Remontada histórica de último a primero!");
            }

            // 6. Bonus por permanencia (+50)
            pointsThisSeason += LegacyPoints.YEAR_BONUS;

            // Actualizar legacy
            _legacy.totalLegacyPoints += pointsThisSeason;
            _legacy.totalSeasons++;

            // Guardar momentos memorables
            foreach (var m in moments)
                _legacy.hallOfFameEntries.Add(m);

            // Crear resumen de temporada
            var summary = new SeasonSummary
            {
                Season = _gm.CurrentSeason,
                DriverChampion = driverChamp != null
                    ? $"{driverChamp.firstName} {driverChamp.lastName}" : "N/A",
                ConstructorChampion = constructorChamp?.fullName ?? "N/A",
                PlayerPosition = playerTeam.constructorPosition,
                PlayerWins = playerTeam.totalWins,
                PlayerPodiums = playerTeam.totalPodiums,
                BestPilot = GetBestPlayerPilot(playerTeamId, drivers),
                LegacyPointsEarned = pointsThisSeason,
                MemorableMoments = moments
            };
            _seasonSummaries.Add(summary);

            Log($"Legado T{_gm.CurrentSeason}: +{pointsThisSeason} pts " +
                $"(Total: {_legacy.totalLegacyPoints})");
        }

        // ══════════════════════════════════════════════════════
        // RANGOS
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene el rango actual según puntos</summary>
        public string GetLegacyRank()
        {
            return GetRankForPoints(_legacy.totalLegacyPoints);
        }

        /// <summary>Obtiene rango para X puntos</summary>
        public static string GetRankForPoints(int points)
        {
            foreach (var (min, rank) in LEGACY_RANKS)
            {
                if (points >= min) return rank;
            }
            return "Equipo Nuevo";
        }

        /// <summary>Puntos necesarios para el siguiente rango</summary>
        public int GetPointsToNextRank()
        {
            for (int i = LEGACY_RANKS.Length - 1; i >= 0; i--)
            {
                if (_legacy.totalLegacyPoints < LEGACY_RANKS[i].min)
                    return LEGACY_RANKS[i].min - _legacy.totalLegacyPoints;
            }
            return 0; // Ya es Dinastía
        }

        // ══════════════════════════════════════════════════════
        // HALL OF FAME
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene los mejores momentos de toda la carrera</summary>
        public List<string> GetHallOfFame()
        {
            return _legacy.hallOfFameEntries ?? new List<string>();
        }

        /// <summary>Agrega un momento memorable manualmente</summary>
        public void AddMemorableMoment(string moment)
        {
            if (_legacy.hallOfFameEntries == null)
                _legacy.hallOfFameEntries = new List<string>();
            _legacy.hallOfFameEntries.Add(moment);
        }

        // ══════════════════════════════════════════════════════
        // VERIFICACIONES
        // ══════════════════════════════════════════════════════

        private bool IsUndefeatedSeason(string teamId)
        {
            var team = _gm.GetTeamById(teamId);
            if (team == null) return false;

            // Si ganaste todas las carreras de la temporada
            return team.totalWins >= Constants.TOTAL_CIRCUITS;
        }

        private bool IsLastToFirst(string teamId)
        {
            // Verificar si empezaste la temporada como último (P10)
            // y terminaste primero (P1) en constructores
            var team = _gm.GetTeamById(teamId);
            if (team == null) return false;

            // Simplificación: si eras P10 de reputación y ahora P1
            return team.constructorPosition == 1 && team.reputation < 60;
        }

        private string GetBestPlayerPilot(string teamId, List<PilotData> standings)
        {
            foreach (var p in standings)
            {
                if (p.currentTeamId == teamId)
                    return $"{p.firstName} {p.lastName}";
            }
            return "N/A";
        }

        private void Log(string msg)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            UnityEngine.Debug.Log($"[LegacyTracker] {msg}");
            #else
            Console.WriteLine($"[LegacyTracker] {msg}");
            #endif
        }
    }

    // ══════════════════════════════════════════════════════
    // RESUMEN DE TEMPORADA
    // ══════════════════════════════════════════════════════

    [Serializable]
    public class SeasonSummary
    {
        public int Season;
        public string DriverChampion;
        public string ConstructorChampion;
        public int PlayerPosition;
        public int PlayerWins;
        public int PlayerPodiums;
        public string BestPilot;
        public int LegacyPointsEarned;
        public List<string> MemorableMoments;
    }
}
