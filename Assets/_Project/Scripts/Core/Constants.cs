// ============================================================
// F1 Career Manager — Constants.cs
// Constantes globales del juego
// ============================================================

namespace F1CareerManager.Core
{
    public static class Constants
    {
        // ── General ──────────────────────────────────────────
        public const int MAX_TEAMS = 10;
        public const int PILOTS_PER_TEAM = 2;
        public const int TOTAL_PILOTS = MAX_TEAMS * PILOTS_PER_TEAM;
        public const int TOTAL_CIRCUITS = 24;
        public const int REGENS_PER_SEASON_MIN = 8;
        public const int REGENS_PER_SEASON_MAX = 12;

        // ── Stats ────────────────────────────────────────────
        public const int STAT_MIN = 0;
        public const int STAT_MAX = 100;
        public const int STARS_MIN = 1;
        public const int STARS_MAX = 5;
        public const int POTENTIAL_MIN = 0;
        public const int POTENTIAL_MAX = 100;

        // ── Pilotos — Humor ──────────────────────────────────
        public const float MOOD_HAPPY_BONUS = 0.05f;           // +5% rendimiento
        public const float MOOD_UPSET_PENALTY = -0.03f;        // -3% rendimiento
        public const float MOOD_FURIOUS_PENALTY = -0.08f;      // -8% rendimiento
        public const float MOOD_FURIOUS_ERROR_INCREASE = 0.15f; // +15% errores
        public const float MOOD_LEAVING_PENALTY = -0.10f;      // -10% rendimiento

        // ── Pilotos — Edad ───────────────────────────────────
        public const int PILOT_MIN_AGE = 17;
        public const int PILOT_MAX_AGE = 42;
        public const int PILOT_PEAK_AGE_MIN = 25;
        public const int PILOT_PEAK_AGE_MAX = 33;
        public const int REGEN_MIN_AGE = 17;
        public const int REGEN_MAX_AGE = 21;

        // ── Contratos ────────────────────────────────────────
        public const int CONTRACT_MIN_YEARS = 1;
        public const int CONTRACT_MAX_YEARS = 5;
        public const float SALARY_MIN = 0.3f;                  // $300K (en millones)
        public const float SALARY_MAX = 50f;                   // $50M

        // ── R&D — Probabilidades de resultado ────────────────
        public const float RND_RESULT_BETTER = 0.20f;          // 20% mejor de lo esperado
        public const float RND_RESULT_EXPECTED = 0.45f;        // 45% como se esperaba
        public const float RND_RESULT_WORSE = 0.25f;           // 25% peor de lo esperado
        public const float RND_RESULT_FAIL = 0.10f;            // 10% sale mal

        // ── FIA — Detección ──────────────────────────────────
        public const float FIA_DETECT_ILLEGAL = 0.85f;         // 85%
        public const float FIA_DETECT_GREY_AGGRESSIVE = 0.575f; // 50-65%
        public const float FIA_DETECT_GREY_SUBTLE = 0.325f;    // 25-40%
        public const float FIA_DETECT_SUSPICIOUS = 0.225f;     // 15-30%

        // ── Carrera — Eventos ────────────────────────────────
        public const float RACE_SAFETY_CAR_CHANCE = 0.35f;
        public const float RACE_VSC_CHANCE = 0.25f;
        public const float RACE_RED_FLAG_CHANCE = 0.05f;
        public const float RACE_RAIN_BASE_CHANCE = 0.15f;
        public const float RACE_CRASH_CHANCE = 0.15f;
        public const float RACE_PENALTY_CHANCE = 0.15f;

        // ── Economía ─────────────────────────────────────────
        public const float BUDGET_CAP = 145f;                  // $145M
        public const float CRISIS_THRESHOLD = 0f;              // Presupuesto = 0
        public const int CRISIS_MAX_SEASONS = 2;               // Temporadas antes de game over

        // ── Prensa ───────────────────────────────────────────
        public const float RUMOR_REAL_CHANCE = 0.60f;           // 60% rumor con base real
        public const float RUMOR_FAKE_CHANCE = 0.40f;           // 40% rumor falso
        public const float COMMS_CHIEF_RUMOR_REDUCE = 0.30f;    // -30% rumores negativos

        // ── Regens — Distribución de potencial ───────────────
        public const float REGEN_GENERATIONAL = 0.05f;         // 5%
        public const float REGEN_ELITE = 0.08f;                // 8%
        public const float REGEN_VERY_GOOD = 0.17f;            // 17%
        public const float REGEN_SOLID = 0.30f;                // 30%
        public const float REGEN_AVERAGE = 0.28f;              // 28%
        public const float REGEN_FILLER = 0.12f;               // 12%

        // ── Staff ────────────────────────────────────────────
        public const float SPY_DETECTION_CHANCE = 0.25f;       // Prob de que descubran al espía
        public const float STAFF_BURNOUT_THRESHOLD = 80f;      // >80% carga = riesgo burnout

        // ── Lesiones ─────────────────────────────────────────
        public const float INJURY_TRAINING_CHANCE = 0.02f;     // 2%
        public const float INJURY_ILLNESS_CHANCE = 0.03f;      // 3%
        public const float INJURY_CAREER_ENDING_CHANCE = 0.01f; // <1%

        // ── Dificultad — Modificadores ───────────────────────
        public const float DIFF_NARRATIVE_ECONOMY = 1.25f;     // +25% ingresos
        public const float DIFF_NARRATIVE_MOOD = 15f;          // +15 humor base
        public const float DIFF_NARRATIVE_FIA = 0.70f;         // -30% detección
        public const float DIFF_HARD_BUDGET = 0.85f;           // -15% presupuesto
        public const float DIFF_HARD_FIA = 1.20f;              // +20% detección
        public const float DIFF_LEGEND_NEGATIVE_EVENTS = 1.50f; // +50% eventos negativos

        // ── Negociación ──────────────────────────────────────
        public const int MAX_NEGOTIATION_ROUNDS = 3;
        public const float OUT_OF_WINDOW_PENALTY = 0.30f;      // +30% costo fuera de ventana

        // ── Legado — Puntos ──────────────────────────────────
        public const int LEGACY_CONSTRUCTORS_CHAMP = 500;
        public const int LEGACY_DRIVERS_CHAMP = 300;
        public const int LEGACY_REGEN_TO_CHAMP = 400;
        public const int LEGACY_LAST_TO_FIRST = 350;
        public const int LEGACY_UNDEFEATED_SEASON = 200;
        public const int LEGACY_MONACO_WIN = 100;
        public const int LEGACY_YEAR_BONUS = 50;
        public const int LEGACY_TRIPLE_CROWN = 1000;
        public const int LEGACY_PER_WIN = 10;
        public const int LEGACY_PER_PODIUM = 5;

        // ── Puntos F1 por posición ───────────────────────────
        public static readonly int[] POINTS_BY_POSITION = new int[]
        {
            25, 18, 15, 12, 10, 8, 6, 4, 2, 1, // P1-P10
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0        // P11-P20
        };

        public const int FASTEST_LAP_POINT = 1; // Solo si termina top 10
    }
}
