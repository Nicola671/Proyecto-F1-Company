// ============================================================
// F1 Career Manager — RandomEventManager.cs
// Eventos aleatorios semanales — 15% por semana
// ============================================================
// DEPENDENCIAS: EventBus.cs, TeamData.cs, PilotData.cs,
//               BudgetManager.cs, TechTreeManager.cs
// TIENES: Eventos positivos, negativos y de decisión del GDD
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.Events
{
    /// <summary>Categoría de evento aleatorio</summary>
    public enum RandomEventCategory
    {
        Positive,
        Negative,
        Decision
    }

    /// <summary>Opción de respuesta a un evento</summary>
    public class EventOption
    {
        public string Label;
        public string Description;
        public string EffectDescription;
        public Action ApplyEffect;
    }

    /// <summary>Evento aleatorio del juego</summary>
    public class RandomEvent
    {
        public string EventId;
        public RandomEventCategory Category;
        public string Title;
        public string Narrative;       // Texto narrativo inmersivo
        public string IconEmoji;
        public List<EventOption> Options;
        public bool RequiresDecision;
        public bool IsResolved;
        public int WeekOccurred;
        public int SeasonOccurred;
    }

    /// <summary>
    /// Gestiona eventos aleatorios semanales.
    /// 15% base por semana. Distribuye entre positivos,
    /// negativos y de decisión.
    /// Cada evento tiene texto narrativo + opciones + efectos.
    /// </summary>
    public class RandomEventManager
    {
        // ── Datos ────────────────────────────────────────────
        private EventBus _eventBus;
        private Random _rng;
        private List<RandomEvent> _eventHistory;
        private HashSet<string> _usedEventTypes; // Evitar repetidos

        // ── Probabilidades ───────────────────────────────────
        private const float BASE_EVENT_CHANCE = 0.15f;      // 15% por semana
        private const float POSITIVE_WEIGHT = 0.30f;         // 30%
        private const float NEGATIVE_WEIGHT = 0.45f;         // 45%
        private const float DECISION_WEIGHT = 0.25f;         // 25%
        private const float LEGEND_NEGATIVE_BOOST = 0.50f;   // +50% en Leyenda

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public RandomEventManager(Random rng = null)
        {
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
            _eventHistory = new List<RandomEvent>();
            _usedEventTypes = new HashSet<string>();
        }

        // ══════════════════════════════════════════════════════
        // CHECK SEMANAL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si ocurre un evento esta semana.
        /// Llamar al inicio de cada semana de juego.
        /// </summary>
        /// <returns>Evento generado o null si no ocurrió nada</returns>
        public RandomEvent CheckWeeklyEvent(TeamData playerTeam,
            List<PilotData> teamPilots, List<TeamData> allTeams,
            List<PilotData> allPilots, string difficulty,
            int week, int season)
        {
            float eventChance = BASE_EVENT_CHANCE;

            // Dificultad Leyenda: +50% eventos negativos
            if (difficulty == "Legend")
                eventChance *= (1f + LEGEND_NEGATIVE_BOOST * 0.3f);

            if ((float)_rng.NextDouble() >= eventChance)
                return null;

            // Seleccionar categoría
            RandomEventCategory category = SelectCategory(difficulty);

            // Generar evento
            RandomEvent evt = null;
            switch (category)
            {
                case RandomEventCategory.Positive:
                    evt = GeneratePositiveEvent(playerTeam, teamPilots,
                        allTeams, allPilots, week, season);
                    break;
                case RandomEventCategory.Negative:
                    evt = GenerateNegativeEvent(playerTeam, teamPilots,
                        allTeams, week, season);
                    break;
                case RandomEventCategory.Decision:
                    evt = GenerateDecisionEvent(playerTeam, teamPilots,
                        allTeams, allPilots, week, season);
                    break;
            }

            if (evt != null)
            {
                _eventHistory.Add(evt);
            }

            return evt;
        }

        // ══════════════════════════════════════════════════════
        // SELECCIÓN DE CATEGORÍA
        // ══════════════════════════════════════════════════════

        private RandomEventCategory SelectCategory(string difficulty)
        {
            float positiveW = POSITIVE_WEIGHT;
            float negativeW = NEGATIVE_WEIGHT;
            float decisionW = DECISION_WEIGHT;

            // Dificultad modifica pesos
            if (difficulty == "Hard")
            {
                positiveW -= 0.05f;
                negativeW += 0.05f;
            }
            else if (difficulty == "Legend")
            {
                positiveW -= 0.10f;
                negativeW += 0.10f;
            }
            else if (difficulty == "Narrative")
            {
                positiveW += 0.10f;
                negativeW -= 0.10f;
            }

            float roll = (float)_rng.NextDouble();
            if (roll < positiveW) return RandomEventCategory.Positive;
            if (roll < positiveW + negativeW) return RandomEventCategory.Negative;
            return RandomEventCategory.Decision;
        }

        // ══════════════════════════════════════════════════════
        // EVENTOS POSITIVOS
        // ══════════════════════════════════════════════════════

        private RandomEvent GeneratePositiveEvent(TeamData team,
            List<PilotData> pilots, List<TeamData> allTeams,
            List<PilotData> allPilots, int week, int season)
        {
            int type = _rng.Next(0, 4);
            switch (type)
            {
                case 0: return CreateSponsorSurpriseEvent(team, week, season);
                case 1: return CreateEurekaEvent(team, week, season);
                case 2: return CreateRivalPilotInterestEvent(team, allTeams,
                             allPilots, week, season);
                case 3: return CreateFavorableRegulationEvent(team, week, season);
                default: return CreateSponsorSurpriseEvent(team, week, season);
            }
        }

        private RandomEvent CreateSponsorSurpriseEvent(TeamData team,
            int week, int season)
        {
            float sponsorAmount = 2f + (float)_rng.NextDouble() * 8f; // $2-10M

            return new RandomEvent
            {
                EventId = $"evt_pos_{_rng.Next(100000)}",
                Category = RandomEventCategory.Positive,
                Title = "💰 ¡Sponsor inesperado!",
                IconEmoji = "💰",
                Narrative = $"Una empresa multinacional se ha acercado a {team.shortName} " +
                    $"mostrando gran interés en patrocinar al equipo. Ofrecen un acuerdo " +
                    $"de ${sponsorAmount:F1}M que podría impulsar significativamente " +
                    "el presupuesto del equipo.",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Aceptar el acuerdo",
                        Description = $"Recibir ${sponsorAmount:F1}M de ingresos extra",
                        EffectDescription = $"+${sponsorAmount:F1}M al presupuesto",
                        ApplyEffect = () => {
                            team.budget += sponsorAmount;
                        }
                    },
                    new EventOption
                    {
                        Label = "Negociar mejores términos",
                        Description = "50% de chance de lograr +30%, sino pierdes la oferta",
                        EffectDescription = "Riesgo/Recompensa",
                        ApplyEffect = () => {
                            if (_rng.NextDouble() < 0.5)
                                team.budget += sponsorAmount * 1.3f;
                            // Si falla, no recibe nada
                        }
                    }
                }
            };
        }

        private RandomEvent CreateEurekaEvent(TeamData team, int week, int season)
        {
            return new RandomEvent
            {
                EventId = $"evt_pos_{_rng.Next(100000)}",
                Category = RandomEventCategory.Positive,
                Title = "🔬 ¡Eureka en R&D!",
                IconEmoji = "🔬",
                Narrative = "Un ingeniero del departamento de desarrollo ha tenido una idea " +
                    "brillante durante los tests en el túnel de viento. El concepto " +
                    "podría acelerar significativamente el desarrollo actual.",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Implementar inmediatamente",
                        Description = "+15% velocidad R&D esta temporada",
                        EffectDescription = "Bonus de desarrollo acelerado",
                        ApplyEffect = () => {
                            team.rndSpeedBonus += 0.15f;
                        }
                    },
                    new EventOption
                    {
                        Label = "Investigar a fondo",
                        Description = "+8% rendimiento al próximo componente instalado",
                        EffectDescription = "Componente mejorado",
                        ApplyEffect = () => {
                            team.nextComponentBonus += 8f;
                        }
                    }
                }
            };
        }

        private RandomEvent CreateRivalPilotInterestEvent(TeamData team,
            List<TeamData> allTeams, List<PilotData> allPilots,
            int week, int season)
        {
            // Encontrar piloto rival con contrato corto
            var candidate = allPilots.Find(p =>
                p.currentTeamId != team.id &&
                !string.IsNullOrEmpty(p.currentTeamId) &&
                p.contractYearsLeft <= 1 &&
                !p.isRetired &&
                p.overallRating >= 60);

            string pilotName = candidate != null
                ? $"{candidate.firstName} {candidate.lastName}"
                : "Un piloto rival destacado";

            return new RandomEvent
            {
                EventId = $"evt_pos_{_rng.Next(100000)}",
                Category = RandomEventCategory.Positive,
                Title = "🏎️ Piloto rival pide venirse",
                IconEmoji = "🏎️",
                Narrative = $"{pilotName} ha contactado de forma privada a la dirección " +
                    $"de {team.shortName}. Está interesado en unirse al proyecto y " +
                    "estaría dispuesto a negociar condiciones favorables.",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Abrir negociaciones",
                        Description = "Iniciar conversaciones con el piloto",
                        EffectDescription = "Se habilita en TransferManager",
                        ApplyEffect = () => {
                            if (candidate != null)
                                candidate.isAvailable = true;
                        }
                    },
                    new EventOption
                    {
                        Label = "Declinar amablemente",
                        Description = "No necesitamos cambios de pilotos ahora",
                        EffectDescription = "Sin efecto",
                        ApplyEffect = () => { }
                    }
                }
            };
        }

        private RandomEvent CreateFavorableRegulationEvent(TeamData team,
            int week, int season)
        {
            return new RandomEvent
            {
                EventId = $"evt_pos_{_rng.Next(100000)}",
                Category = RandomEventCategory.Positive,
                Title = "📜 Cambio reglamentario favorable",
                IconEmoji = "📜",
                Narrative = "La FIA ha anunciado un cambio en las regulaciones técnicas que " +
                    "beneficia el concepto aerodinámico de tu auto. Los ingenieros " +
                    "estiman que ganaremos rendimiento sin hacer modificaciones.",
                RequiresDecision = false,
                IsResolved = true,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "¡Excelente noticia!",
                        Description = "+3% rendimiento aerodinámico",
                        EffectDescription = "+3 performance auto",
                        ApplyEffect = () => {
                            team.aeroPerformance += 3f;
                            team.carPerformance += 1.5f;
                        }
                    }
                }
            };
        }

        // ══════════════════════════════════════════════════════
        // EVENTOS NEGATIVOS
        // ══════════════════════════════════════════════════════

        private RandomEvent GenerateNegativeEvent(TeamData team,
            List<PilotData> pilots, List<TeamData> allTeams,
            int week, int season)
        {
            int type = _rng.Next(0, 5);
            switch (type)
            {
                case 0: return CreateMediaScandalEvent(team, pilots, week, season);
                case 1: return CreateFactoryFireEvent(team, week, season);
                case 2: return CreateMechanicStrikeEvent(team, week, season);
                case 3: return CreateTechLeakEvent(team, allTeams, week, season);
                case 4: return CreateStarDriverThreatEvent(team, pilots, week, season);
                default: return CreateFactoryFireEvent(team, week, season);
            }
        }

        private RandomEvent CreateMediaScandalEvent(TeamData team,
            List<PilotData> pilots, int week, int season)
        {
            var pilot = pilots.Count > 0 ? pilots[_rng.Next(pilots.Count)] : null;
            string pilotName = pilot != null ? pilot.lastName : "tu piloto";

            return new RandomEvent
            {
                EventId = $"evt_neg_{_rng.Next(100000)}",
                Category = RandomEventCategory.Negative,
                Title = "📰 Escándalo mediático",
                IconEmoji = "📰",
                Narrative = $"Imágenes comprometedoras de {pilotName} se han filtrado " +
                    "a la prensa. Los tabloides están teniendo un día de campo y " +
                    "los sponsors no están contentos.",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Comunicado oficial",
                        Description = "Emitir declaración profesional, minimizar daños",
                        EffectDescription = "Humor piloto -5, prensa +3",
                        ApplyEffect = () => {
                            if (pilot != null) pilot.moodValue -= 5;
                        }
                    },
                    new EventOption
                    {
                        Label = "Multar al piloto",
                        Description = "Multa interna de $500K, señal de disciplina",
                        EffectDescription = "Humor piloto -15, sponsors calmados",
                        ApplyEffect = () => {
                            if (pilot != null)
                            {
                                pilot.moodValue -= 15;
                                team.budget -= 0.5f;
                            }
                        }
                    },
                    new EventOption
                    {
                        Label = "Apoyar públicamente",
                        Description = "Defender al piloto, arriesgar con sponsors",
                        EffectDescription = "Humor piloto +10, reputación -3",
                        ApplyEffect = () => {
                            if (pilot != null) pilot.moodValue += 10;
                            team.reputation -= 3;
                        }
                    }
                }
            };
        }

        private RandomEvent CreateFactoryFireEvent(TeamData team,
            int week, int season)
        {
            return new RandomEvent
            {
                EventId = $"evt_neg_{_rng.Next(100000)}",
                Category = RandomEventCategory.Negative,
                Title = "🔥 Incendio en la fábrica",
                IconEmoji = "🔥",
                Narrative = "Un incendio se ha declarado en el ala de producción de la fábrica. " +
                    "Afortunadamente no hay heridos, pero el daño material es considerable. " +
                    "El departamento de R&D estará paralizado 3 semanas mientras se repara.",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Reparación normal (3 semanas)",
                        Description = "R&D paralizado 3 semanas, sin costo extra",
                        EffectDescription = "R&D detenido 3 semanas",
                        ApplyEffect = () => {
                            team.rndPausedWeeks = 3;
                        }
                    },
                    new EventOption
                    {
                        Label = "Reparación urgente ($3M)",
                        Description = "R&D paralizado solo 1 semana, coste $3M",
                        EffectDescription = "-$3M, R&D solo 1 semana parado",
                        ApplyEffect = () => {
                            team.budget -= 3f;
                            team.rndPausedWeeks = 1;
                        }
                    }
                }
            };
        }

        private RandomEvent CreateMechanicStrikeEvent(TeamData team,
            int week, int season)
        {
            return new RandomEvent
            {
                EventId = $"evt_neg_{_rng.Next(100000)}",
                Category = RandomEventCategory.Negative,
                Title = "🛠️ Huelga de mecánicos",
                IconEmoji = "🛠️",
                Narrative = "Los mecánicos del equipo están descontentos con las condiciones " +
                    "laborales. Amenazan con una huelga que afectaría los pit stops " +
                    "en las próximas 2 carreras (+2 segundos por parada).",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Negociar aumento ($1.5M)",
                        Description = "Resolver la huelga con aumento salarial",
                        EffectDescription = "-$1.5M, sin penalización pit stops",
                        ApplyEffect = () => {
                            team.budget -= 1.5f;
                        }
                    },
                    new EventOption
                    {
                        Label = "Ignorar las quejas",
                        Description = "Pit stops +2s por 2 carreras, moral baja",
                        EffectDescription = "+2s pit stops, moral del equipo -10",
                        ApplyEffect = () => {
                            team.pitStopPenalty = 2.0f;
                            team.pitStopPenaltyRaces = 2;
                        }
                    },
                    new EventOption
                    {
                        Label = "Reunión intermedia",
                        Description = "60% se calman con +$0.5M, 40% siguen en huelga",
                        EffectDescription = "-$0.5M, riesgo",
                        ApplyEffect = () => {
                            team.budget -= 0.5f;
                            if (_rng.NextDouble() >= 0.6)
                            {
                                team.pitStopPenalty = 2.0f;
                                team.pitStopPenaltyRaces = 1;
                            }
                        }
                    }
                }
            };
        }

        private RandomEvent CreateTechLeakEvent(TeamData team,
            List<TeamData> allTeams, int week, int season)
        {
            var rival = allTeams.Count > 1
                ? allTeams[_rng.Next(allTeams.Count)] : team;
            while (rival.id == team.id && allTeams.Count > 1)
                rival = allTeams[_rng.Next(allTeams.Count)];

            return new RandomEvent
            {
                EventId = $"evt_neg_{_rng.Next(100000)}",
                Category = RandomEventCategory.Negative,
                Title = "🔍 Filtración técnica",
                IconEmoji = "🔍",
                Narrative = $"Información técnica confidencial de {team.shortName} ha sido " +
                    $"filtrada. Se sospecha que {rival.shortName} tiene acceso a los " +
                    "datos de tu último componente desarrollado.",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Denunciar a la FIA",
                        Description = "Investigación FIA contra el rival, proceso largo",
                        EffectDescription = "FIA investiga al rival (4 semanas)",
                        ApplyEffect = () => {
                            // Efecto externo: RegulationChecker investiga rival
                        }
                    },
                    new EventOption
                    {
                        Label = "Reforzar seguridad ($1M)",
                        Description = "Prevenir futuras filtraciones",
                        EffectDescription = "-$1M, sin más filteraciones",
                        ApplyEffect = () => {
                            team.budget -= 1f;
                        }
                    },
                    new EventOption
                    {
                        Label = "Desinformar",
                        Description = "Plantar datos falsos para confundir al rival",
                        EffectDescription = "70% rival pierde rendimiento, 30% sin efecto",
                        ApplyEffect = () => {
                            if (_rng.NextDouble() < 0.7)
                            {
                                rival.carPerformance -= 2f;
                            }
                        }
                    }
                }
            };
        }

        private RandomEvent CreateStarDriverThreatEvent(TeamData team,
            List<PilotData> pilots, int week, int season)
        {
            var bestPilot = pilots.Count > 0 ? pilots[0] : null;
            if (pilots.Count > 1 && pilots[1].overallRating > pilots[0].overallRating)
                bestPilot = pilots[1];

            string pilotName = bestPilot != null ? bestPilot.lastName : "Tu piloto estrella";

            return new RandomEvent
            {
                EventId = $"evt_neg_{_rng.Next(100000)}",
                Category = RandomEventCategory.Negative,
                Title = "😤 Piloto estrella amenaza irse",
                IconEmoji = "😤",
                Narrative = $"{pilotName} no está satisfecho con la dirección del equipo. " +
                    "Ha filtrado a la prensa que está \"evaluando opciones\" para la " +
                    "próxima temporada. La relación se ha deteriorado.",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Reunión privada",
                        Description = "Hablar cara a cara, mostrar compromiso",
                        EffectDescription = "Humor +10, lealtad +5",
                        ApplyEffect = () => {
                            if (bestPilot != null)
                            {
                                bestPilot.moodValue += 10;
                                bestPilot.loyalty += 5;
                            }
                        }
                    },
                    new EventOption
                    {
                        Label = "Subir salario ($2M extra)",
                        Description = "Poner dinero sobre la mesa",
                        EffectDescription = "-$2M, humor +15, lealtad +10",
                        ApplyEffect = () => {
                            team.budget -= 2f;
                            if (bestPilot != null)
                            {
                                bestPilot.moodValue += 15;
                                bestPilot.loyalty += 10;
                                bestPilot.salary += 2f;
                            }
                        }
                    },
                    new EventOption
                    {
                        Label = "Ignorar la amenaza",
                        Description = "Dejar que se calme solo",
                        EffectDescription = "50% se calma, 50% pide transfer",
                        ApplyEffect = () => {
                            if (bestPilot != null)
                            {
                                if (_rng.NextDouble() < 0.5)
                                    bestPilot.moodValue += 5;
                                else
                                {
                                    bestPilot.mood = "WantsOut";
                                    bestPilot.moodValue -= 20;
                                }
                            }
                        }
                    }
                }
            };
        }

        // ══════════════════════════════════════════════════════
        // EVENTOS DE DECISIÓN
        // ══════════════════════════════════════════════════════

        private RandomEvent GenerateDecisionEvent(TeamData team,
            List<PilotData> pilots, List<TeamData> allTeams,
            List<PilotData> allPilots, int week, int season)
        {
            int type = _rng.Next(0, 4);
            switch (type)
            {
                case 0: return CreateMergerOfferEvent(team, allTeams, week, season);
                case 1: return CreateEngineSharingEvent(team, allTeams, week, season);
                case 2: return CreateAmbassadorEvent(team, pilots, week, season);
                case 3: return CreateFIAVoteEvent(team, week, season);
                default: return CreateFIAVoteEvent(team, week, season);
            }
        }

        private RandomEvent CreateMergerOfferEvent(TeamData team,
            List<TeamData> allTeams, int week, int season)
        {
            var smallTeam = allTeams.Find(t => t.id != team.id && t.budget < 60f);
            string smallName = smallTeam != null ? smallTeam.shortName : "un equipo pequeño";

            return new RandomEvent
            {
                EventId = $"evt_dec_{_rng.Next(100000)}",
                Category = RandomEventCategory.Decision,
                Title = "🤝 Oferta de fusión",
                IconEmoji = "🤝",
                Narrative = $"{smallName} propone una fusión con {team.shortName}. " +
                    "Combinando recursos podrían crear un equipo más competitivo, " +
                    "pero perdería sautonomía y habría restructuración.",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Aceptar fusión",
                        Description = "+$20M presupuesto, +5 reputación, pierdes 1 staff",
                        EffectDescription = "Gran inyección + restructuración",
                        ApplyEffect = () => {
                            team.budget += 20f;
                            team.reputation += 5;
                        }
                    },
                    new EventOption
                    {
                        Label = "Rechazar",
                        Description = "Mantener independencia total",
                        EffectDescription = "Sin cambios",
                        ApplyEffect = () => { }
                    },
                    new EventOption
                    {
                        Label = "Contrapropuesta: solo tecnología",
                        Description = "Compartir datos técnicos sin fusión completa",
                        EffectDescription = "+$5M + bonus R&D temporario",
                        ApplyEffect = () => {
                            team.budget += 5f;
                            team.rndSpeedBonus += 0.05f;
                        }
                    }
                }
            };
        }

        private RandomEvent CreateEngineSharingEvent(TeamData team,
            List<TeamData> allTeams, int week, int season)
        {
            var partner = allTeams.Find(t => t.id != team.id);
            string partnerName = partner != null ? partner.shortName : "un constructor";

            return new RandomEvent
            {
                EventId = $"evt_dec_{_rng.Next(100000)}",
                Category = RandomEventCategory.Decision,
                Title = "🔧 Programa motor compartido",
                IconEmoji = "🔧",
                Narrative = $"{partnerName} ofrece un programa de motor compartido. " +
                    "Accederías a su unidad de potencia a cambio de datos aerodinámicos. " +
                    "Podría mejorar fiabilidad pero expone tus secretos técnicos.",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Aceptar intercambio",
                        Description = "+8% fiabilidad motor, rival gana +3% aero",
                        EffectDescription = "Mejora fiabilidad, rival mejora aero",
                        ApplyEffect = () => {
                            team.engineReliability += 8f;
                            if (partner != null) partner.aeroPerformance += 3f;
                        }
                    },
                    new EventOption
                    {
                        Label = "Solo comprar motor ($5M)",
                        Description = "+5% fiabilidad sin dar datos",
                        EffectDescription = "-$5M, +5% fiabilidad",
                        ApplyEffect = () => {
                            team.budget -= 5f;
                            team.engineReliability += 5f;
                        }
                    },
                    new EventOption
                    {
                        Label = "Rechazar",
                        Description = "Mantener programa propio",
                        EffectDescription = "Sin cambios",
                        ApplyEffect = () => { }
                    }
                }
            };
        }

        private RandomEvent CreateAmbassadorEvent(TeamData team,
            List<PilotData> pilots, int week, int season)
        {
            var pilot = pilots.Count > 0 ? pilots[_rng.Next(pilots.Count)] : null;
            string pilotName = pilot != null ? pilot.lastName : "Tu piloto";

            return new RandomEvent
            {
                EventId = $"evt_dec_{_rng.Next(100000)}",
                Category = RandomEventCategory.Decision,
                Title = "🌟 Piloto embajador de sponsor",
                IconEmoji = "🌟",
                Narrative = $"Un sponsor importante quiere que {pilotName} sea su " +
                    "embajador de marca. Pagarían muy bien, pero el piloto tendría " +
                    "compromisos comerciales que podrían restar tiempo de entrenamiento.",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Aceptar",
                        Description = "+$3M ingresos sponsor, piloto -3 concentración",
                        EffectDescription = "Dinero vs rendimiento",
                        ApplyEffect = () => {
                            team.budget += 3f;
                            if (pilot != null)
                                pilot.concentration = Math.Max(20, pilot.concentration - 3);
                        }
                    },
                    new EventOption
                    {
                        Label = "Rechazar",
                        Description = "Priorizar el rendimiento en pista",
                        EffectDescription = "Piloto enfocado",
                        ApplyEffect = () => {
                            if (pilot != null) pilot.concentration += 1;
                        }
                    },
                    new EventOption
                    {
                        Label = "Negociar agenda limitada",
                        Description = "+$1.5M ingresos, sin penalización",
                        EffectDescription = "Balance entre dinero y rendimiento",
                        ApplyEffect = () => {
                            team.budget += 1.5f;
                        }
                    }
                }
            };
        }

        private RandomEvent CreateFIAVoteEvent(TeamData team,
            int week, int season)
        {
            return new RandomEvent
            {
                EventId = $"evt_dec_{_rng.Next(100000)}",
                Category = RandomEventCategory.Decision,
                Title = "🗳️ Voto cambio reglamento FIA",
                IconEmoji = "🗳️",
                Narrative = "La FIA propone un cambio reglamentario para la próxima temporada. " +
                    "Se reduce el budget cap en $10M y se permite un componente adicional " +
                    "por equipo. Tu voto podría ser decisivo.",
                RequiresDecision = true,
                IsResolved = false,
                WeekOccurred = week,
                SeasonOccurred = season,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Label = "Votar a favor",
                        Description = "Apoya la reducción del cap (beneficia equipos pequeños)",
                        EffectDescription = "Reputación +5, cap reducido próxima temp",
                        ApplyEffect = () => {
                            team.reputation += 5;
                        }
                    },
                    new EventOption
                    {
                        Label = "Votar en contra",
                        Description = "Mantener el status quo (beneficia equipos top)",
                        EffectDescription = "Reputación -3 si equipo top, +3 si pequeño",
                        ApplyEffect = () => {
                            if (team.budget > 100f)
                                team.reputation -= 3;
                            else
                                team.reputation += 3;
                        }
                    },
                    new EventOption
                    {
                        Label = "Abstenerse",
                        Description = "No tomar posición, mantener buenas relaciones",
                        EffectDescription = "Sin efecto significativo",
                        ApplyEffect = () => { }
                    }
                }
            };
        }

        // ══════════════════════════════════════════════════════
        // RESOLUCIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Resuelve un evento con la opción elegida por el jugador
        /// </summary>
        public void ResolveEvent(string eventId, int optionIndex)
        {
            var evt = _eventHistory.Find(e => e.EventId == eventId);
            if (evt == null || evt.IsResolved) return;

            if (optionIndex >= 0 && optionIndex < evt.Options.Count)
            {
                evt.Options[optionIndex].ApplyEffect?.Invoke();
            }

            evt.IsResolved = true;
        }

        // ══════════════════════════════════════════════════════
        // CONSULTAS
        // ══════════════════════════════════════════════════════

        /// <summary>Eventos pendientes de resolución</summary>
        public List<RandomEvent> GetPendingEvents()
        {
            return _eventHistory.FindAll(e => !e.IsResolved && e.RequiresDecision);
        }

        /// <summary>Historial completo de eventos</summary>
        public List<RandomEvent> GetEventHistory() => _eventHistory;

        /// <summary>Eventos de una temporada</summary>
        public List<RandomEvent> GetSeasonEvents(int season)
        {
            return _eventHistory.FindAll(e => e.SeasonOccurred == season);
        }
    }
}
