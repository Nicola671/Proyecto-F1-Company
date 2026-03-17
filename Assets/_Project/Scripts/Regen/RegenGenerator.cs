// ============================================================
// F1 Career Manager — RegenGenerator.cs
// Generador de pilotos futuros (regens)
// ============================================================
// DEPENDENCIAS: NameGenerator.cs, PilotData.cs, Constants.cs
// EJECUTAR: Automáticamente al inicio de cada temporada
// Genera 8-12 pilotos nuevos con distribución de potencial del GDD
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Data;

namespace F1CareerManager.Regen
{
    /// <summary>
    /// Genera pilotos nuevos (regens) cada temporada.
    /// Distribución de potencial exacta del GDD:
    /// Generacional 5%, Elite 8%, Muy bueno 17%, Sólido 30%,
    /// Promedio 28%, Relleno 12%.
    /// Los regens aparecen en F2/F3 y pueden ser fichados.
    /// </summary>
    public class RegenGenerator
    {
        // ── Datos ────────────────────────────────────────────
        private NameGenerator _nameGenerator;
        private Random _rng;
        private int _regenCounter;

        // ── Distribución de potencial exacta del GDD ────────
        // Formato: (nombre, minPotential, maxPotential, weight)
        private static readonly PotentialTier[] POTENTIAL_DISTRIBUTION =
            new PotentialTier[]
        {
            new PotentialTier("Generacional", 90, 99, 5),   //  5%
            new PotentialTier("Elite", 80, 89, 8),           //  8%
            new PotentialTier("MuyBueno", 70, 79, 17),       // 17%
            new PotentialTier("Solido", 55, 69, 30),         // 30%
            new PotentialTier("Promedio", 40, 54, 28),       // 28%
            new PotentialTier("Relleno", 20, 39, 12),        // 12%
        };

        // ── Pistas visibles para el jugador ──────────────────
        private static readonly Dictionary<string, string> POTENTIAL_LABELS =
            new Dictionary<string, string>
        {
            { "Generacional", "Talento generacional" },
            { "Elite", "Excepcional" },
            { "MuyBueno", "Muy prometedor" },
            { "Solido", "Prometedor" },
            { "Promedio", "Decente" },
            { "Relleno", "Inconsistente" }
        };

        // ── Rango de edad para regens ────────────────────────
        private const int MIN_AGE = 17;
        private const int MAX_AGE = 21;

        // ── Stats iniciales: % del potencial final ───────────
        private const float INITIAL_STAT_MIN_RATIO = 0.55f;  // 55% del potencial
        private const float INITIAL_STAT_MAX_RATIO = 0.75f;  // 75% del potencial

        // ── Cantidad de regens por temporada ─────────────────
        private const int MIN_REGENS_PER_SEASON = 8;
        private const int MAX_REGENS_PER_SEASON = 12;

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public RegenGenerator(NameGenerator nameGenerator, Random rng = null)
        {
            _nameGenerator = nameGenerator;
            _rng = rng ?? new Random();
            _regenCounter = 0;
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN DE TEMPORADA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera una nueva camada de regens para la temporada.
        /// Llamar al inicio de cada temporada.
        /// </summary>
        /// <param name="currentSeason">Número de temporada actual</param>
        /// <returns>Lista de 8-12 pilotos regen nuevos</returns>
        public List<PilotData> GenerateSeasonRegens(int currentSeason)
        {
            var regens = new List<PilotData>();
            int count = _rng.Next(MIN_REGENS_PER_SEASON, MAX_REGENS_PER_SEASON + 1);

            for (int i = 0; i < count; i++)
            {
                var regen = GenerateSingleRegen(currentSeason);
                regens.Add(regen);
            }

            return regens;
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN INDIVIDUAL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera un piloto regen individual con stats, personalidad y contrato
        /// </summary>
        public PilotData GenerateSingleRegen(int currentSeason)
        {
            _regenCounter++;

            // ── Nombre y nacionalidad ────────────────────────
            var name = _nameGenerator.GenerateRandomName();

            // ── Potencial (distribución del GDD) ─────────────
            var tier = SelectPotentialTier();
            int potential = _rng.Next(tier.MinPotential, tier.MaxPotential + 1);

            // ── Edad ─────────────────────────────────────────
            int age = _rng.Next(MIN_AGE, MAX_AGE + 1);

            // ── Peak age (cuándo alcanza su máximo) ──────────
            int peakAge = 26 + _rng.Next(-2, 4); // 24-29

            // ── Stats iniciales (60-70% del potencial) ───────
            int speed = GenerateInitialStat(potential);
            int consistency = GenerateInitialStat(potential);
            int rainSkill = GenerateInitialStat(potential);
            int startSkill = GenerateInitialStat(potential);
            int defense = GenerateInitialStat(potential);
            int attack = GenerateInitialStat(potential);
            int tireManagement = GenerateInitialStat(potential);
            int fuelManagement = GenerateInitialStat(potential);
            int concentration = GenerateInitialStat(potential);
            int adaptability = GenerateInitialStat(potential);

            // ── Personalidad aleatoria ───────────────────────
            int ego = GeneratePersonalityTrait(tier.Name);
            int loyalty = GeneratePersonalityTrait(tier.Name);
            int aggression = _rng.Next(20, 90);

            // ── Tasas de desarrollo ──────────────────────────
            float growthRate = CalculateGrowthRate(tier.Name);
            float declineRate = 0.3f + (float)_rng.NextDouble() * 0.4f;

            // ── Pista visible del potencial ──────────────────
            string potentialLabel = POTENTIAL_LABELS.ContainsKey(tier.Name)
                ? POTENTIAL_LABELS[tier.Name] : "Desconocido";

            // ── Construir PilotData ──────────────────────────
            var pilot = new PilotData
            {
                // Identidad
                id = $"regen_{currentSeason}_{_regenCounter}_{name.CountryCode.ToLower()}",
                firstName = name.FirstName,
                lastName = name.LastName,
                shortName = name.LastName.Length >= 3
                    ? name.LastName.Substring(0, 3).ToUpper()
                    : name.LastName.ToUpper(),
                nationality = name.Nationality,
                countryCode = name.CountryCode,
                age = age,
                number = 50 + _regenCounter, // Números temporales altos
                isRegen = true,
                spriteId = "regen_default",

                // Stats visibles
                speed = speed,
                consistency = consistency,
                rainSkill = rainSkill,
                startSkill = startSkill,
                defense = defense,
                attack = attack,
                tireManagement = tireManagement,
                fuelManagement = fuelManagement,
                concentration = concentration,
                adaptability = adaptability,

                // Potencial oculto
                potential = potential,
                potentialLabel = potentialLabel,
                growthRate = growthRate,
                declineRate = declineRate,
                peakAge = peakAge,

                // Emocional
                mood = "Neutral",
                moodValue = 30, // Los jóvenes empiezan optimistas
                ego = ego,
                loyalty = loyalty,
                aggression = aggression,
                formCurrent = 70 + _rng.Next(0, 15),
                teammateRelation = 50,
                pressRelation = 50,

                // Contrato (sin equipo, en F2/F3)
                currentTeamId = "",
                contractYearsLeft = 0,
                salary = 0.3f + (float)_rng.NextDouble() * 0.5f, // $0.3-0.8M
                role = "JuniorDriver",
                marketValue = CalculateMarketValue(potential, age),

                // Carrera
                totalWins = 0,
                totalPodiums = 0,
                totalPoints = 0,
                totalRaces = 0,
                totalChampionships = 0,
                bestFinish = 0,

                // Estado
                isInjured = false,
                isRetired = false,
                isAvailable = true,

                // Categoría actual
                currentCategory = age <= 18 ? "F3" : "F2"
            };

            // Calcular overall y estrellas
            pilot.CalculateOverall();
            pilot.CalculateStars();

            return pilot;
        }

        // ══════════════════════════════════════════════════════
        // DISTRIBUCIÓN DE POTENCIAL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Selecciona un tier de potencial según la distribución del GDD
        /// </summary>
        private PotentialTier SelectPotentialTier()
        {
            int totalWeight = 0;
            foreach (var tier in POTENTIAL_DISTRIBUTION)
                totalWeight += tier.Weight;

            int roll = _rng.Next(0, totalWeight);
            int cumulative = 0;

            foreach (var tier in POTENTIAL_DISTRIBUTION)
            {
                cumulative += tier.Weight;
                if (roll < cumulative)
                    return tier;
            }

            return POTENTIAL_DISTRIBUTION[3]; // Sólido como fallback
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN DE STATS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera un stat inicial basado en el potencial.
        /// Los jóvenes empiezan entre 55-75% de su potencial final.
        /// </summary>
        private int GenerateInitialStat(int potential)
        {
            float ratio = INITIAL_STAT_MIN_RATIO +
                (float)_rng.NextDouble() * (INITIAL_STAT_MAX_RATIO - INITIAL_STAT_MIN_RATIO);
            int stat = (int)(potential * ratio);

            // Variación adicional ±5
            stat += _rng.Next(-5, 6);

            return Clamp(stat, 20, 85); // Nunca empieza arriba de 85
        }

        /// <summary>
        /// Genera un rasgo de personalidad.
        /// Talentos generacionales tienden a tener más ego.
        /// </summary>
        private int GeneratePersonalityTrait(string tierName)
        {
            int baseTrait = _rng.Next(20, 80);

            switch (tierName)
            {
                case "Generacional":
                    // Alto ego, lealtad variable
                    return Clamp(baseTrait + _rng.Next(10, 25), 0, 100);
                case "Elite":
                    return Clamp(baseTrait + _rng.Next(5, 15), 0, 100);
                case "Relleno":
                    // Bajo ego, alta lealtad (agradecidos de estar ahí)
                    return Clamp(baseTrait - _rng.Next(5, 20), 0, 100);
                default:
                    return baseTrait;
            }
        }

        /// <summary>
        /// Calcula la tasa de crecimiento según el tier
        /// </summary>
        private float CalculateGrowthRate(string tierName)
        {
            switch (tierName)
            {
                case "Generacional":
                    return 0.8f + (float)_rng.NextDouble() * 0.2f; // 0.8-1.0
                case "Elite":
                    return 0.7f + (float)_rng.NextDouble() * 0.2f; // 0.7-0.9
                case "MuyBueno":
                    return 0.5f + (float)_rng.NextDouble() * 0.3f; // 0.5-0.8
                case "Solido":
                    return 0.4f + (float)_rng.NextDouble() * 0.2f; // 0.4-0.6
                case "Promedio":
                    return 0.3f + (float)_rng.NextDouble() * 0.2f; // 0.3-0.5
                case "Relleno":
                    return 0.2f + (float)_rng.NextDouble() * 0.2f; // 0.2-0.4
                default:
                    return 0.5f;
            }
        }

        /// <summary>
        /// Calcula el valor de mercado inicial del regen
        /// </summary>
        private float CalculateMarketValue(int potential, int age)
        {
            // Base = potencial × factor
            float value = potential * 0.05f;

            // Más joven = más valioso
            if (age <= 18)
                value *= 1.3f;
            else if (age <= 19)
                value *= 1.1f;

            // Variación
            value *= 1f + ((float)_rng.NextDouble() * 0.3f - 0.15f);

            return (float)Math.Round(value, 1);
        }

        // ══════════════════════════════════════════════════════
        // DESARROLLO ANUAL DE REGENS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Procesa el desarrollo anual de pilotos regen en F2/F3.
        /// Los que están en equipos F1 se desarrollan vía PilotBehavior.
        /// Este método es para los que aún no fueron fichados.
        /// </summary>
        public void ProcessAnnualDevelopment(List<PilotData> regens)
        {
            foreach (var regen in regens)
            {
                if (!regen.isRegen) continue;
                if (regen.isRetired) continue;
                if (!string.IsNullOrEmpty(regen.currentTeamId)) continue;

                // Crecimiento de stats según potencial
                int gap = regen.potential - regen.CalculateOverall();
                if (gap <= 0) continue;

                float growth = gap * regen.growthRate * 0.08f;
                int pointsToGrow = Math.Max(1, (int)Math.Round(growth));
                pointsToGrow = Math.Min(pointsToGrow, 5);

                for (int i = 0; i < pointsToGrow; i++)
                {
                    int statIdx = _rng.Next(0, 10);
                    int amount = _rng.Next(1, 3);

                    GrowRegenStat(regen, statIdx, amount);
                }

                // Incrementar edad
                regen.age++;

                // Categoría: 17-18 en F3, 19-21 en F2
                if (regen.age >= 19 && regen.currentCategory == "F3")
                    regen.currentCategory = "F2";

                // Demasiado viejo sin fichar: algunos se retiran de F2
                if (regen.age > 24 && string.IsNullOrEmpty(regen.currentTeamId))
                {
                    float retireChance = (regen.age - 23) * 0.15f;
                    if ((float)_rng.NextDouble() < retireChance)
                    {
                        regen.isRetired = true;
                        regen.isAvailable = false;
                    }
                }

                // Recalcular
                regen.CalculateOverall();
                regen.CalculateStars();
            }
        }

        /// <summary>
        /// Mejora un stat específico del regen
        /// </summary>
        private void GrowRegenStat(PilotData regen, int statIndex, int amount)
        {
            int maxVal = regen.potential;
            switch (statIndex)
            {
                case 0: regen.speed = Clamp(regen.speed + amount, 0, maxVal); break;
                case 1: regen.consistency = Clamp(regen.consistency + amount, 0, maxVal); break;
                case 2: regen.rainSkill = Clamp(regen.rainSkill + amount, 0, maxVal); break;
                case 3: regen.startSkill = Clamp(regen.startSkill + amount, 0, maxVal); break;
                case 4: regen.defense = Clamp(regen.defense + amount, 0, maxVal); break;
                case 5: regen.attack = Clamp(regen.attack + amount, 0, maxVal); break;
                case 6: regen.tireManagement = Clamp(regen.tireManagement + amount, 0, maxVal); break;
                case 7: regen.fuelManagement = Clamp(regen.fuelManagement + amount, 0, maxVal); break;
                case 8: regen.concentration = Clamp(regen.concentration + amount, 0, maxVal); break;
                case 9: regen.adaptability = Clamp(regen.adaptability + amount, 0, maxVal); break;
            }
        }

        // ══════════════════════════════════════════════════════
        // CONSULTAS
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene regens disponibles para fichaje (F2/F3, sin equipo)</summary>
        public List<PilotData> GetAvailableRegens(List<PilotData> allPilots)
        {
            return allPilots.FindAll(p =>
                p.isRegen &&
                !p.isRetired &&
                string.IsNullOrEmpty(p.currentTeamId) &&
                p.isAvailable);
        }

        /// <summary>Obtiene los top regens por potencial</summary>
        public List<PilotData> GetTopProspects(List<PilotData> allPilots, int count)
        {
            var available = GetAvailableRegens(allPilots);
            available.Sort((a, b) => b.potential.CompareTo(a.potential));
            return available.GetRange(0, Math.Min(count, available.Count));
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    /// <summary>
    /// Tier de potencial con rango y peso
    /// </summary>
    public class PotentialTier
    {
        public string Name;
        public int MinPotential;
        public int MaxPotential;
        public int Weight;

        public PotentialTier(string name, int min, int max, int weight)
        {
            Name = name;
            MinPotential = min;
            MaxPotential = max;
            Weight = weight;
        }
    }
}
