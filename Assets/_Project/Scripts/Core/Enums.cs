// ============================================================
// F1 Career Manager — Enums.cs
// Todas las enumeraciones del juego
// ============================================================

namespace F1CareerManager.Core
{
    // ── Pilotos ──────────────────────────────────────────────

    public enum PilotMood
    {
        Happy,      // +5% rendimiento
        Neutral,    // Sin efecto
        Upset,      // -3% rendimiento
        Furious,    // -8% rendimiento, +15% errores
        WantsOut    // -10% rendimiento, busca otros equipos
    }

    public enum PilotRole
    {
        First,      // Piloto #1
        Second,     // Piloto #2
        Reserve,    // Piloto de reserva
        Junior      // Piloto de academia/cantera
    }

    public enum PotentialLabel
    {
        Generational,   // "Talento generacional" — 90-99
        Exceptional,    // "Excepcional" — 80-89
        VeryPromising,  // "Muy prometedor" — 70-79
        Promising,      // "Prometedor" — 55-69
        Decent,         // "Decente" — 40-54
        Limited         // "Inconsistente / Limitado" — 20-39
    }

    // ── Staff ────────────────────────────────────────────────

    public enum StaffRole
    {
        TechnicalDirector,  // Director Técnico
        AeroChief,          // Jefe Aerodinámica
        EngineChief,        // Jefe de Motor
        RaceEngineer,       // Ingeniero de Carrera
        DataAnalyst,        // Analista de Datos
        TeamDoctor,         // Médico del Equipo
        CommsChief,         // Jefe de Comunicaciones
        AcademyDirector,    // Director de Academia
        FinanceDirector,    // Jefe Financiero
        Spy                 // Espía Industrial (ILEGAL)
    }

    // ── R&D ──────────────────────────────────────────────────

    public enum ComponentArea
    {
        Aerodynamics,
        Engine,
        Chassis,
        Reliability
    }

    public enum ComponentLegality
    {
        Legal,              // 100% legal
        GreySubtle,         // Zona gris sutil
        GreyAggressive,     // Zona gris agresiva
        Illegal             // Claramente ilegal
    }

    public enum InstallResult
    {
        BetterThanExpected, // +rendimiento
        AsExpected,         // Normal
        WorseThanExpected,  // -rendimiento
        Failed              // Daña el auto
    }

    public enum ComponentStatus
    {
        InDevelopment,      // En desarrollo
        Available,          // Disponible para instalar
        Installed,          // Instalado en el auto
        Banned              // Prohibido por la FIA
    }

    // ── Circuitos ────────────────────────────────────────────

    public enum CircuitType
    {
        Street,             // Urbano
        HighSpeed,          // Alta velocidad
        UltraHighSpeed,     // Ultra rápido
        Technical,          // Técnico
        Mixed,              // Mixto
        SemiStreet,         // Semi-urbano
        Reference,          // Referencia (Barcelona)
        Altitude            // Altitud (México)
    }

    public enum TireDegradation
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum TireCompound
    {
        Soft,
        Medium,
        Hard,
        Intermediate,
        Wet
    }

    // ── Carrera ──────────────────────────────────────────────

    public enum WeatherCondition
    {
        Sunny,
        Cloudy,
        LightRain,
        HeavyRain,
        Changing           // Puede cambiar durante la carrera
    }

    public enum RaceEventType
    {
        SafetyCar,
        VirtualSafetyCar,
        RedFlag,
        Rain,
        MechanicalFailure,
        Crash,
        Penalty,
        Overtake,
        PitStop,
        FastestLap,
        DRSOvertake
    }

    public enum MotorMode
    {
        Conservation,       // Conservar
        Normal,             // Normal
        Attack              // Máximo ataque
    }

    public enum TeamOrder
    {
        LetPass,            // "Déjalo pasar"
        FreeFight,          // "Pelea libre"
        HoldPosition        // "Conserva posición"
    }

    public enum SessionType
    {
        FP1,
        FP2,
        FP3,
        Q1,
        Q2,
        Q3,
        Race
    }

    // ── FIA ──────────────────────────────────────────────────

    public enum SanctionType
    {
        Warning,
        Fine,
        ComponentBan,
        RaceExclusion,
        PointsPenalty,
        SeasonDisqualification
    }

    public enum SanctionSeverity
    {
        Light,
        Moderate,
        Severe,
        Extreme
    }

    // ── Prensa ───────────────────────────────────────────────

    public enum NewsType
    {
        PostRaceHeadline,   // Resultado de carrera
        TransferRumor,      // Rumor de mercado
        InternalLeak,       // Filtración interna
        Criticism,          // Crítica al jugador
        Praise,             // Elogio
        Controversy,        // Controversia
        PilotDrama          // Drama entre pilotos
    }

    public enum MediaBias
    {
        Technical,          // Objetivo, técnico
        Sensationalist,     // Sensacionalista
        Balanced,           // Equilibrado
        Rumors,             // Se enfoca en rumores
        DataDriven          // Datos y estadísticas
    }

    public enum PressResponseEffect
    {
        PressNeutral,
        PressPositive,
        PressNegative,
        PressDramatic
    }

    // ── Lesiones ─────────────────────────────────────────────

    public enum InjurySeverity
    {
        Light,              // 1-2 carreras
        Moderate,           // 3-6 carreras
        Severe,             // Resto de temporada
        CareerEnding        // Retiro forzoso
    }

    public enum InjuryCause
    {
        RaceAccident,
        Training,
        Fatigue,
        Illness
    }

    // ── Eventos aleatorios ───────────────────────────────────

    public enum RandomEventType
    {
        Positive,
        Negative,
        Neutral
    }

    // ── Economía ─────────────────────────────────────────────

    public enum FinancialStatus
    {
        Thriving,           // Excelente
        Healthy,            // Saludable
        Tight,              // Ajustado
        Struggling,         // En problemas
        Crisis              // Crisis, riesgo de game over
    }

    // ── Dificultad ───────────────────────────────────────────

    public enum DifficultyLevel
    {
        Narrative,          // Fácil
        Standard,           // Normal
        Demanding,          // Difícil
        Legend              // Extremo
    }

    // ── Market / Transferencias ──────────────────────────────

    public enum TransferWindowType
    {
        Main,               // Entre temporadas
        Emergency,          // Por lesión grave
        OutOfWindow         // Fuera de ventana (+30% costo)
    }

    public enum NegotiationResult
    {
        Accepted,
        Rejected,
        CounterOffer,
        TiredOfNegotiating  // Tras 3 rondas
    }

    // ── Guardado ─────────────────────────────────────────────

    public enum SaveType
    {
        AutoSave,
        Manual,
        Emergency           // Al cerrar la app
    }

    // ── Objetivos ────────────────────────────────────────────

    public enum ObjectiveLevel
    {
        Minimum,            // Si no se cumple → presión
        Standard,           // Lo esperado → bonus $5M
        Elite               // Superación → bonus $15M
    }
}
