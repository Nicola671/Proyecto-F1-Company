// ============================================================
// F1 Career Manager — SponsorManager.cs
// Gestión de patrocinadores — IA 5 (Economía)
// ============================================================
// DEPENDENCIAS: EventBus.cs, TeamData.cs, SponsorData.cs,
//               BudgetManager.cs, Constants.cs
// EVENTOS QUE DISPARA: OnBudgetChanged (vía BudgetManager),
//                      OnNewsGenerated (ofertas/salidas)
// EVENTOS QUE ESCUCHA: OnSeasonEnd, OnRaceFinished
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.AI.EconomyAI
{
    /// <summary>
    /// Gestiona los patrocinadores de todos los equipos.
    /// Evalúa rendimiento post-temporada, genera nuevas ofertas,
    /// y gestiona salidas de sponsors insatisfechos.
    /// Regla: máx 1 sponsor principal + 4 secundarios por equipo.
    /// </summary>
    public class SponsorManager
    {
        // ── Datos ────────────────────────────────────────────
        private Dictionary<string, List<SponsorData>> _teamSponsors;
        private List<SponsorData> _availableSponsors;
        private BudgetManager _budgetManager;
        private EventBus _eventBus;
        private Random _rng;

        // ── Constantes ───────────────────────────────────────
        private const int MAX_SECONDARY_SPONSORS = 4;
        private const int MAIN_SPONSOR_MIN_POS = 8;     // Sponsor principal se va si > P8
        private const float VICTORY_BONUS_BASE = 0.5f;  // $500K bonus por victoria
        private const int SPONSOR_EVAL_TOLERANCE = 2;    // Margen de posiciones

        // ── Pools de sponsors (datos base para generar) ──────
        private static readonly string[] SPONSOR_NAMES_MAIN = {
            "Petronas Global", "Oracle Cloud", "Mission Pinnacle",
            "CryptoTrade Plus", "Sapphire Energy", "NovaTech Industries",
            "Apex Dynamics", "Titan Motors", "Velocity Fuels",
            "Quantum Networks", "SkyForge Aero", "PrimeVault Finance"
        };

        private static readonly string[] SPONSOR_NAMES_SECONDARY = {
            "ZipPay", "DataFlow", "SparkWater", "CoolBreeze Ice",
            "FlexFit Pro", "NeonByte", "AeroJet Cargo", "PulseGear",
            "SharpEdge Tools", "VoltCharge", "BlueShift Labs",
            "GreenGrade Eco", "SteadyState Tech", "BrightPath AI",
            "CoreSync Systems", "DriveZone", "FastLane Logistics",
            "TopShelf Brands", "UrbanRace Wear", "AlphaGrid Energy"
        };

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public SponsorManager(BudgetManager budgetManager, Random rng = null)
        {
            _budgetManager = budgetManager;
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
            _teamSponsors = new Dictionary<string, List<SponsorData>>();
            _availableSponsors = new List<SponsorData>();
        }

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera sponsors iniciales para cada equipo según su reputación.
        /// Equipos top → sponsors más generosos.
        /// </summary>
        public void InitializeSponsors(List<TeamData> teams)
        {
            _teamSponsors.Clear();

            foreach (var team in teams)
            {
                var sponsors = new List<SponsorData>();

                // Sponsor principal (basado en reputación)
                sponsors.Add(GenerateMainSponsor(team));

                // 1-4 sponsors secundarios según reputación
                int secondaryCount = 1 + (team.reputation / 30); // 1-4
                secondaryCount = Math.Min(secondaryCount, MAX_SECONDARY_SPONSORS);

                for (int i = 0; i < secondaryCount; i++)
                {
                    sponsors.Add(GenerateSecondarySponsor(team));
                }

                _teamSponsors[team.id] = sponsors;

                // Actualizar ingresos de sponsors en el equipo
                UpdateTeamSponsorIncome(team);
            }

            // Generar pool de sponsors disponibles en el mercado
            GenerateAvailableSponsorPool();
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN DE SPONSORS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera un sponsor principal para un equipo
        /// </summary>
        private SponsorData GenerateMainSponsor(TeamData team)
        {
            string name = SPONSOR_NAMES_MAIN[_rng.Next(SPONSOR_NAMES_MAIN.Length)];

            // Pago basado en la reputación del equipo
            // Rep 95 → $45M, Rep 50 → $12M, Rep 30 → $5M
            float basePay = 3f + (team.reputation * 0.45f);
            float variance = 1f + ((float)_rng.NextDouble() * 0.20f - 0.10f);

            return new SponsorData
            {
                id = $"spon_main_{team.id}_{_rng.Next(10000)}",
                name = name,
                tier = "Main",
                annualPayment = basePay * variance,
                contractYears = _rng.Next(2, 5),
                yearsRemaining = _rng.Next(2, 5),
                minimumConstructorPos = MAIN_SPONSOR_MIN_POS,
                performanceBonus = VICTORY_BONUS_BASE * (team.reputation / 50f),
                isHappy = true
            };
        }

        /// <summary>
        /// Genera un sponsor secundario
        /// </summary>
        private SponsorData GenerateSecondarySponsor(TeamData team)
        {
            string name = SPONSOR_NAMES_SECONDARY[
                _rng.Next(SPONSOR_NAMES_SECONDARY.Length)];

            // Sponsors secundarios pagan menos
            float basePay = 1f + (team.reputation * 0.10f);
            float variance = 1f + ((float)_rng.NextDouble() * 0.30f - 0.15f);

            // Requisito de posición menos estricto
            int minPos = Math.Min(10, 5 + (100 - team.reputation) / 15);

            return new SponsorData
            {
                id = $"spon_sec_{team.id}_{_rng.Next(10000)}",
                name = name,
                tier = "Secondary",
                annualPayment = basePay * variance,
                contractYears = _rng.Next(1, 4),
                yearsRemaining = _rng.Next(1, 4),
                minimumConstructorPos = minPos,
                performanceBonus = VICTORY_BONUS_BASE * 0.3f,
                isHappy = true
            };
        }

        /// <summary>
        /// Genera el pool de sponsors disponibles en el mercado
        /// </summary>
        private void GenerateAvailableSponsorPool()
        {
            _availableSponsors.Clear();

            // 3-5 sponsors principales disponibles
            for (int i = 0; i < _rng.Next(3, 6); i++)
            {
                _availableSponsors.Add(new SponsorData
                {
                    id = $"spon_avail_main_{_rng.Next(10000)}",
                    name = SPONSOR_NAMES_MAIN[_rng.Next(SPONSOR_NAMES_MAIN.Length)],
                    tier = "Main",
                    annualPayment = 8f + (float)_rng.NextDouble() * 30f,
                    contractYears = _rng.Next(2, 5),
                    yearsRemaining = _rng.Next(2, 5),
                    minimumConstructorPos = _rng.Next(5, 10),
                    performanceBonus = VICTORY_BONUS_BASE,
                    isHappy = true
                });
            }

            // 8-12 sponsors secundarios disponibles
            for (int i = 0; i < _rng.Next(8, 13); i++)
            {
                _availableSponsors.Add(new SponsorData
                {
                    id = $"spon_avail_sec_{_rng.Next(10000)}",
                    name = SPONSOR_NAMES_SECONDARY[
                        _rng.Next(SPONSOR_NAMES_SECONDARY.Length)],
                    tier = "Secondary",
                    annualPayment = 1f + (float)_rng.NextDouble() * 8f,
                    contractYears = _rng.Next(1, 4),
                    yearsRemaining = _rng.Next(1, 4),
                    minimumConstructorPos = _rng.Next(6, 11),
                    performanceBonus = VICTORY_BONUS_BASE * 0.2f,
                    isHappy = true
                });
            }
        }

        // ══════════════════════════════════════════════════════
        // EVALUACIÓN POST-TEMPORADA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Evalúa la satisfacción de todos los sponsors al final de la temporada.
        /// Los sponsors insatisfechos se van, los satisfechos renuevan.
        /// Retorna la lista de sponsors que se fueron.
        /// </summary>
        public List<SponsorDepartureInfo> EvaluateEndOfSeason(List<TeamData> teams)
        {
            var departures = new List<SponsorDepartureInfo>();

            foreach (var team in teams)
            {
                if (!_teamSponsors.ContainsKey(team.id)) continue;

                var sponsors = _teamSponsors[team.id];
                var toRemove = new List<SponsorData>();

                foreach (var sponsor in sponsors)
                {
                    sponsor.yearsRemaining--;

                    // ¿El equipo cumplió el requisito?
                    bool metRequirement = team.constructorPosition <=
                        (sponsor.minimumConstructorPos + SPONSOR_EVAL_TOLERANCE);

                    if (!metRequirement)
                    {
                        sponsor.isHappy = false;

                        // Si no cumple Y es sponsor principal fuera del top 8
                        if (sponsor.tier == "Main" &&
                            team.constructorPosition > MAIN_SPONSOR_MIN_POS)
                        {
                            toRemove.Add(sponsor);
                            departures.Add(new SponsorDepartureInfo
                            {
                                SponsorName = sponsor.name,
                                TeamId = team.id,
                                Reason = $"Fuera del top {MAIN_SPONSOR_MIN_POS} constructores",
                                LostIncome = sponsor.annualPayment
                            });
                        }
                        // Sponsor secundario insatisfecho: 60% de chance de irse
                        else if (sponsor.tier == "Secondary" &&
                                 (float)_rng.NextDouble() < 0.60f)
                        {
                            toRemove.Add(sponsor);
                            departures.Add(new SponsorDepartureInfo
                            {
                                SponsorName = sponsor.name,
                                TeamId = team.id,
                                Reason = "Rendimiento por debajo de expectativas",
                                LostIncome = sponsor.annualPayment
                            });
                        }
                    }
                    else
                    {
                        sponsor.isHappy = true;
                    }

                    // Contrato expirado
                    if (sponsor.yearsRemaining <= 0 && !toRemove.Contains(sponsor))
                    {
                        // 70% de renovar si está contento, 20% si no
                        float renewChance = sponsor.isHappy ? 0.70f : 0.20f;
                        if ((float)_rng.NextDouble() > renewChance)
                        {
                            toRemove.Add(sponsor);
                            departures.Add(new SponsorDepartureInfo
                            {
                                SponsorName = sponsor.name,
                                TeamId = team.id,
                                Reason = "Contrato expirado, no renovaron",
                                LostIncome = sponsor.annualPayment
                            });
                        }
                        else
                        {
                            // Renovar con nuevo contrato
                            sponsor.yearsRemaining = _rng.Next(1, 4);
                            // Ajustar pago según rendimiento
                            float adjustment = sponsor.isHappy ? 1.05f : 0.85f;
                            sponsor.annualPayment *= adjustment;
                        }
                    }
                }

                // Remover los que se fueron
                foreach (var s in toRemove)
                    sponsors.Remove(s);

                // Actualizar ingresos del equipo
                UpdateTeamSponsorIncome(team);
            }

            // Regenerar pool disponible
            GenerateAvailableSponsorPool();

            return departures;
        }

        // ══════════════════════════════════════════════════════
        // OFERTAS NUEVAS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera ofertas de sponsors para un equipo basado en su rendimiento.
        /// Equipos con mejor rendimiento reciben mejores ofertas.
        /// </summary>
        public List<SponsorData> GenerateOffersForTeam(TeamData team)
        {
            var offers = new List<SponsorData>();
            if (!_teamSponsors.ContainsKey(team.id)) return offers;

            var currentSponsors = _teamSponsors[team.id];

            // ¿Necesita sponsor principal?
            bool needsMain = !currentSponsors.Exists(s => s.tier == "Main");
            int currentSecondary = 0;
            foreach (var s in currentSponsors)
                if (s.tier == "Secondary") currentSecondary++;
            bool canAddSecondary = currentSecondary < MAX_SECONDARY_SPONSORS;

            // Probabilidad de recibir ofertas basada en reputación
            float offerChance = team.reputation / 100f;

            if (needsMain && (float)_rng.NextDouble() < offerChance)
            {
                // Buscar sponsor principal disponible
                var mainOffers = _availableSponsors.FindAll(s => s.tier == "Main");
                if (mainOffers.Count > 0)
                {
                    var offer = mainOffers[_rng.Next(mainOffers.Count)];
                    // Ajustar pago según reputación del equipo
                    offer.annualPayment *= (team.reputation / 80f);
                    offers.Add(offer);
                }
            }

            if (canAddSecondary && (float)_rng.NextDouble() < offerChance * 0.8f)
            {
                var secOffers = _availableSponsors.FindAll(s => s.tier == "Secondary");
                if (secOffers.Count > 0)
                {
                    int numOffers = Math.Min(_rng.Next(1, 3), secOffers.Count);
                    for (int i = 0; i < numOffers; i++)
                    {
                        int idx = _rng.Next(secOffers.Count);
                        offers.Add(secOffers[idx]);
                        secOffers.RemoveAt(idx);
                    }
                }
            }

            return offers;
        }

        /// <summary>
        /// Firma un nuevo sponsor para un equipo
        /// </summary>
        public bool SignSponsor(TeamData team, SponsorData sponsor)
        {
            if (!_teamSponsors.ContainsKey(team.id))
                _teamSponsors[team.id] = new List<SponsorData>();

            var currentSponsors = _teamSponsors[team.id];

            // Verificar límites
            if (sponsor.tier == "Main" && currentSponsors.Exists(s => s.tier == "Main"))
                return false;

            int secondaryCount = 0;
            foreach (var s in currentSponsors)
                if (s.tier == "Secondary") secondaryCount++;
            if (sponsor.tier == "Secondary" && secondaryCount >= MAX_SECONDARY_SPONSORS)
                return false;

            currentSponsors.Add(sponsor);
            _availableSponsors.Remove(sponsor);
            UpdateTeamSponsorIncome(team);

            // Generar noticia
            _eventBus.FireNewsGenerated(new EventBus.NewsGeneratedArgs
            {
                NewsId = $"news_sponsor_{_rng.Next(100000)}",
                Headline = $"¡{team.shortName} firma con {sponsor.name}!",
                Body = $"Acuerdo de {sponsor.yearsRemaining} años por ${sponsor.annualPayment:F1}M anuales.",
                Type = "Praise",
                MediaOutlet = "F1 Financial Times",
                IsRumor = false,
                IsTrue = true,
                RelatedPilotIds = new List<string>(),
                RelatedTeamIds = new List<string> { team.id }
            });

            return true;
        }

        // ══════════════════════════════════════════════════════
        // BONOS POR VICTORIA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Procesa bonos de victoria de sponsors para un equipo
        /// </summary>
        public void ProcessVictoryBonus(TeamData team)
        {
            if (!_teamSponsors.ContainsKey(team.id)) return;

            float totalBonus = 0f;
            foreach (var sponsor in _teamSponsors[team.id])
            {
                totalBonus += sponsor.performanceBonus;
            }

            if (totalBonus > 0 && _budgetManager != null)
            {
                _budgetManager.AddIncome(team, totalBonus,
                    $"Bonus victoria de sponsors (${totalBonus:F1}M)");
            }
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        /// <summary>Actualiza el ingreso total por sponsors del equipo</summary>
        private void UpdateTeamSponsorIncome(TeamData team)
        {
            float total = 0f;
            if (_teamSponsors.ContainsKey(team.id))
            {
                foreach (var s in _teamSponsors[team.id])
                    total += s.annualPayment;
            }
            team.sponsorIncome = total;
        }

        /// <summary>Obtiene los sponsors de un equipo</summary>
        public List<SponsorData> GetTeamSponsors(string teamId)
        {
            if (_teamSponsors.ContainsKey(teamId))
                return _teamSponsors[teamId];
            return new List<SponsorData>();
        }

        /// <summary>Obtiene los sponsors disponibles en el mercado</summary>
        public List<SponsorData> GetAvailableSponsors()
        {
            return _availableSponsors;
        }
    }

    /// <summary>Info sobre un sponsor que se fue</summary>
    public class SponsorDepartureInfo
    {
        public string SponsorName;
        public string TeamId;
        public string Reason;
        public float LostIncome;
    }
}
