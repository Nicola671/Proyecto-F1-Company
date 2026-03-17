// ============================================================
// F1 Career Manager — PressConference.cs
// Ruedas de prensa — 3-4 por temporada, 3 preguntas cada una
// ============================================================
// DEPENDENCIAS: EventBus.cs, PilotData.cs, TeamData.cs
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.AI.PressAI
{
    /// <summary>Opción de respuesta en rueda de prensa</summary>
    public class PressResponse
    {
        public string Label;           // Texto corto del botón
        public string FullResponse;    // Respuesta completa del jugador
        public int PilotMoodEffect;    // Efecto en humor del piloto mencionado
        public int PressRelationEffect; // Efecto en relación con prensa
        public int FIAAttentionEffect;  // Efecto en atención de la FIA
        public int TeamMoraleEffect;   // Efecto en moral del equipo
        public string ToneTag;         // "Diplomatic", "Aggressive", "Evasive", "Honest"
    }

    /// <summary>Una pregunta de la rueda de prensa</summary>
    public class PressQuestion
    {
        public string QuestionId;
        public string JournalistName;
        public string MediaOutlet;
        public string QuestionText;
        public string Context;         // Contexto de por qué se pregunta
        public List<PressResponse> Responses;
    }

    /// <summary>Una rueda de prensa completa</summary>
    public class PressConferenceEvent
    {
        public string ConferenceId;
        public int Season;
        public int Week;
        public string Occasion;        // "PreSeason", "MidSeason", "PostRace", "CrisisSpecial"
        public List<PressQuestion> Questions;
        public bool IsCompleted;
        public List<string> SelectedResponses; // IDs de respuestas elegidas
    }

    /// <summary>
    /// Genera y procesa ruedas de prensa.
    /// 3-4 por temporada, 3 preguntas cada una.
    /// Las respuestas afectan humor, prensa, FIA y equipo.
    /// El historial de respuestas cambia el tono futuro.
    /// </summary>
    public class PressConference
    {
        // ── Datos ────────────────────────────────────────────
        private EventBus _eventBus;
        private Random _rng;
        private List<PressConferenceEvent> _history;
        private Dictionary<string, int> _toneHistory; // Acumulado por tono

        // ── Periodistas ficticios ────────────────────────────
        private static readonly string[] JOURNALIST_NAMES = {
            "Sarah Mitchell", "Marco Pellegrini", "David Chen",
            "Ana Sánchez", "James Whitworth", "Yuki Tanabe",
            "Pierre Lefèvre", "Linda Grosjean", "Oliver Krebs",
            "Roberto Álvarez", "Thomas Bradshaw", "Ingrid Holm"
        };

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public PressConference(Random rng = null)
        {
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
            _history = new List<PressConferenceEvent>();
            _toneHistory = new Dictionary<string, int>
            {
                { "Diplomatic", 0 },
                { "Aggressive", 0 },
                { "Evasive", 0 },
                { "Honest", 0 }
            };
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN DE CONFERENCIA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera una rueda de prensa completa con 3 preguntas.
        /// Elegir ocasión según contexto del juego.
        /// </summary>
        public PressConferenceEvent GenerateConference(
            string occasion, TeamData playerTeam,
            List<PilotData> teamPilots, List<TeamData> allTeams,
            int week, int season)
        {
            var conference = new PressConferenceEvent
            {
                ConferenceId = $"press_{_rng.Next(100000)}",
                Season = season,
                Week = week,
                Occasion = occasion,
                Questions = new List<PressQuestion>(),
                IsCompleted = false,
                SelectedResponses = new List<string>()
            };

            // Generar 3 preguntas basadas en contexto
            var availableQuestions = GenerateContextualQuestions(
                occasion, playerTeam, teamPilots, allTeams);

            // Seleccionar 3
            int count = Math.Min(3, availableQuestions.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = _rng.Next(availableQuestions.Count);
                conference.Questions.Add(availableQuestions[idx]);
                availableQuestions.RemoveAt(idx);
            }

            // Si historial agresivo → preguntas más duras
            if (_toneHistory.ContainsKey("Aggressive") && _toneHistory["Aggressive"] > 3)
            {
                conference.Questions.Add(GenerateAggregatedToneQuestion(playerTeam));
            }

            // Si historial evasivo → prensa insiste más
            if (_toneHistory.ContainsKey("Evasive") && _toneHistory["Evasive"] > 3)
            {
                conference.Questions.Add(GenerateInsistentQuestion(playerTeam));
            }

            return conference;
        }

        // ══════════════════════════════════════════════════════
        // PREGUNTAS CONTEXTUALES
        // ══════════════════════════════════════════════════════

        private List<PressQuestion> GenerateContextualQuestions(
            string occasion, TeamData team, List<PilotData> pilots,
            List<TeamData> allTeams)
        {
            var questions = new List<PressQuestion>();

            // General: siempre aplicable
            questions.Add(GenerateTeamPerformanceQuestion(team));
            questions.Add(GenerateFutureStrategyQuestion(team));

            // Basadas en pilotos del equipo
            foreach (var pilot in pilots)
            {
                if (pilot.mood == "Furious" || pilot.mood == "WantsOut")
                    questions.Add(GeneratePilotUnhappyQuestion(pilot, team));

                if (pilot.contractYearsLeft <= 1)
                    questions.Add(GenerateContractQuestion(pilot, team));
            }

            // Sobre rivales
            if (allTeams.Count > 1)
            {
                var rival = allTeams[_rng.Next(allTeams.Count)];
                while (rival.id == team.id && allTeams.Count > 1)
                    rival = allTeams[_rng.Next(allTeams.Count)];
                questions.Add(GenerateRivalQuestion(team, rival));
            }

            // Boxes/Estrategia (simulando el ejemplo del GDD)
            questions.Add(GeneratePitStopQuestion(team));

            // R&D
            questions.Add(GenerateRnDQuestion(team));

            // Financiera
            if (team.financialStatus == "Crisis")
                questions.Add(GenerateFinancialQuestion(team));

            return questions;
        }

        // ══════════════════════════════════════════════════════
        // GENERADORES DE PREGUNTAS
        // ══════════════════════════════════════════════════════

        private PressQuestion GenerateTeamPerformanceQuestion(TeamData team)
        {
            return new PressQuestion
            {
                QuestionId = $"q_perf_{_rng.Next(10000)}",
                JournalistName = RandomJournalist(),
                MediaOutlet = "PitWall Post",
                QuestionText = $"Su equipo está P{team.constructorPosition} en el campeonato. " +
                    "¿Está satisfecho con el rendimiento hasta ahora?",
                Context = "Rendimiento general del equipo",
                Responses = new List<PressResponse>
                {
                    new PressResponse {
                        Label = "Satisfecho",
                        FullResponse = "Estamos contentos con el progreso. El equipo trabaja duro.",
                        PilotMoodEffect = 3, PressRelationEffect = 5,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 5,
                        ToneTag = "Diplomatic"
                    },
                    new PressResponse {
                        Label = "Exigir más",
                        FullResponse = "No es suficiente. Esperamos más de todos, incluido yo mismo.",
                        PilotMoodEffect = -5, PressRelationEffect = 3,
                        FIAAttentionEffect = 0, TeamMoraleEffect = -3,
                        ToneTag = "Honest"
                    },
                    new PressResponse {
                        Label = "Culpar al auto",
                        FullResponse = "El rendimiento del auto no está donde debería. Necesitamos mejoras.",
                        PilotMoodEffect = 5, PressRelationEffect = -2,
                        FIAAttentionEffect = 0, TeamMoraleEffect = -5,
                        ToneTag = "Aggressive"
                    },
                    new PressResponse {
                        Label = "Sin comentarios",
                        FullResponse = "Prefiero no hablar de eso. Siguiente pregunta.",
                        PilotMoodEffect = 0, PressRelationEffect = -8,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 0,
                        ToneTag = "Evasive"
                    }
                }
            };
        }

        private PressQuestion GeneratePitStopQuestion(TeamData team)
        {
            return new PressQuestion
            {
                QuestionId = $"q_pit_{_rng.Next(10000)}",
                JournalistName = RandomJournalist(),
                MediaOutlet = "GrandPrix Weekly",
                QuestionText = "Su piloto perdió posiciones en boxes. ¿Fue un fallo estratégico?",
                Context = "Ejemplo del GDD: pregunta sobre error en pit stop",
                Responses = new List<PressResponse>
                {
                    new PressResponse {
                        Label = "Decisión calculada",
                        FullResponse = "Fue una decisión calculada que no salió como esperábamos.",
                        PilotMoodEffect = 5, PressRelationEffect = 0,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 3,
                        ToneTag = "Diplomatic"
                    },
                    new PressResponse {
                        Label = "Culpar al piloto",
                        FullResponse = "El piloto tardó demasiado en entrar. No fue culpa del muro.",
                        PilotMoodEffect = -15, PressRelationEffect = 5,
                        FIAAttentionEffect = 0, TeamMoraleEffect = -5,
                        ToneTag = "Aggressive"
                    },
                    new PressResponse {
                        Label = "Sin comentarios",
                        FullResponse = "No voy a hablar de estrategia en público.",
                        PilotMoodEffect = 0, PressRelationEffect = -8,
                        FIAAttentionEffect = 3, TeamMoraleEffect = 0,
                        ToneTag = "Evasive"
                    },
                    new PressResponse {
                        Label = "Admitir error",
                        FullResponse = "Fue un error nuestro. Aprendemos y seguimos adelante.",
                        PilotMoodEffect = 8, PressRelationEffect = 10,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 2,
                        ToneTag = "Honest"
                    }
                }
            };
        }

        private PressQuestion GeneratePilotUnhappyQuestion(PilotData pilot,
            TeamData team)
        {
            return new PressQuestion
            {
                QuestionId = $"q_unhappy_{_rng.Next(10000)}",
                JournalistName = RandomJournalist(),
                MediaOutlet = "Paddock Rumors",
                QuestionText = $"Se rumorea que {pilot.lastName} no está contento. " +
                    "¿Hay tensión interna?",
                Context = $"Piloto {pilot.mood}: {pilot.lastName}",
                Responses = new List<PressResponse>
                {
                    new PressResponse {
                        Label = "Negar todo",
                        FullResponse = $"No hay ningún problema con {pilot.lastName}. Todo está bien.",
                        PilotMoodEffect = -3, PressRelationEffect = -3,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 0,
                        ToneTag = "Evasive"
                    },
                    new PressResponse {
                        Label = "Apoyar al piloto",
                        FullResponse = $"{pilot.lastName} es clave para nosotros. Haremos lo necesario para que esté cómodo.",
                        PilotMoodEffect = 10, PressRelationEffect = 5,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 5,
                        ToneTag = "Diplomatic"
                    },
                    new PressResponse {
                        Label = "Ser directo",
                        FullResponse = "Si no está contento, la puerta siempre está abierta. Nadie es más grande que el equipo.",
                        PilotMoodEffect = -20, PressRelationEffect = 8,
                        FIAAttentionEffect = 0, TeamMoraleEffect = -3,
                        ToneTag = "Aggressive"
                    },
                    new PressResponse {
                        Label = "Reconocer situación",
                        FullResponse = "Es verdad que hay aspectos por mejorar. Estamos trabajando en ello juntos.",
                        PilotMoodEffect = 5, PressRelationEffect = 8,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 3,
                        ToneTag = "Honest"
                    }
                }
            };
        }

        private PressQuestion GenerateContractQuestion(PilotData pilot,
            TeamData team)
        {
            return new PressQuestion
            {
                QuestionId = $"q_contract_{_rng.Next(10000)}",
                JournalistName = RandomJournalist(),
                MediaOutlet = "Formula Insider",
                QuestionText = $"El contrato de {pilot.lastName} está por vencer. " +
                    "¿Hay planes de renovación?",
                Context = $"Contrato de {pilot.lastName}: {pilot.contractYearsLeft} años restantes",
                Responses = new List<PressResponse>
                {
                    new PressResponse {
                        Label = "Confirmar interés",
                        FullResponse = $"Queremos renovar a {pilot.lastName}. Las conversaciones avanzan.",
                        PilotMoodEffect = 8, PressRelationEffect = 5,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 5,
                        ToneTag = "Diplomatic"
                    },
                    new PressResponse {
                        Label = "Ser ambiguo",
                        FullResponse = "Evaluamos todas las opciones. Aún no hay decisión.",
                        PilotMoodEffect = -5, PressRelationEffect = -3,
                        FIAAttentionEffect = 0, TeamMoraleEffect = -2,
                        ToneTag = "Evasive"
                    },
                    new PressResponse {
                        Label = "Negar renovación",
                        FullResponse = "Estamos explorando el mercado. No hay nada cerrado con nadie.",
                        PilotMoodEffect = -15, PressRelationEffect = 5,
                        FIAAttentionEffect = 0, TeamMoraleEffect = -5,
                        ToneTag = "Honest"
                    },
                    new PressResponse {
                        Label = "Desviar la pregunta",
                        FullResponse = "No es momento de hablar de contratos. Nos enfocamos en la pista.",
                        PilotMoodEffect = -3, PressRelationEffect = -5,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 0,
                        ToneTag = "Evasive"
                    }
                }
            };
        }

        private PressQuestion GenerateRivalQuestion(TeamData team, TeamData rival)
        {
            return new PressQuestion
            {
                QuestionId = $"q_rival_{_rng.Next(10000)}",
                JournalistName = RandomJournalist(),
                MediaOutlet = "Racing Tribune",
                QuestionText = $"¿Cómo evalúa el rendimiento de {rival.shortName} esta temporada?",
                Context = $"Rival: {rival.shortName} en P{rival.constructorPosition}",
                Responses = new List<PressResponse>
                {
                    new PressResponse {
                        Label = "Elogiarlos",
                        FullResponse = $"{rival.shortName} está haciendo un gran trabajo. Son competidores dignos.",
                        PilotMoodEffect = 0, PressRelationEffect = 8,
                        FIAAttentionEffect = 0, TeamMoraleEffect = -2,
                        ToneTag = "Diplomatic"
                    },
                    new PressResponse {
                        Label = "Minimizarlos",
                        FullResponse = "Nos enfocamos en nosotros mismos. No miramos a los costados.",
                        PilotMoodEffect = 0, PressRelationEffect = 3,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 3,
                        ToneTag = "Evasive"
                    },
                    new PressResponse {
                        Label = "Provocar",
                        FullResponse = $"Veremos si el rendimiento de {rival.shortName} es realmente legítimo.",
                        PilotMoodEffect = 0, PressRelationEffect = -5,
                        FIAAttentionEffect = 5, TeamMoraleEffect = 5,
                        ToneTag = "Aggressive"
                    },
                    new PressResponse {
                        Label = "Análisis técnico",
                        FullResponse = $"Han mejorado en algunas áreas. Tomamos nota y nos adaptamos.",
                        PilotMoodEffect = 0, PressRelationEffect = 10,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 3,
                        ToneTag = "Honest"
                    }
                }
            };
        }

        private PressQuestion GenerateFutureStrategyQuestion(TeamData team)
        {
            return new PressQuestion
            {
                QuestionId = $"q_strategy_{_rng.Next(10000)}",
                JournalistName = RandomJournalist(),
                MediaOutlet = "F1 Analytics",
                QuestionText = "¿Cuál es la estrategia del equipo para el resto de la temporada?",
                Context = "Planificación a futuro",
                Responses = new List<PressResponse>
                {
                    new PressResponse {
                        Label = "Ser ambicioso",
                        FullResponse = "Vamos a por todo. Queremos ganar y vamos a pelear cada carrera.",
                        PilotMoodEffect = 5, PressRelationEffect = 5,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 8,
                        ToneTag = "Aggressive"
                    },
                    new PressResponse {
                        Label = "Ser realista",
                        FullResponse = "Paso a paso. Primero consolidar, luego atacar.",
                        PilotMoodEffect = 0, PressRelationEffect = 5,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 3,
                        ToneTag = "Honest"
                    },
                    new PressResponse {
                        Label = "No revelar",
                        FullResponse = "Preferimos no dar detalles. Nuestros rivales estarán atentos.",
                        PilotMoodEffect = 0, PressRelationEffect = -3,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 0,
                        ToneTag = "Evasive"
                    },
                    new PressResponse {
                        Label = "Focus en R&D",
                        FullResponse = "Nuestra prioridad es el desarrollo del auto. Los resultados vendrán.",
                        PilotMoodEffect = -3, PressRelationEffect = 5,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 5,
                        ToneTag = "Diplomatic"
                    }
                }
            };
        }

        private PressQuestion GenerateRnDQuestion(TeamData team)
        {
            return new PressQuestion
            {
                QuestionId = $"q_rnd_{_rng.Next(10000)}",
                JournalistName = RandomJournalist(),
                MediaOutlet = "GrandPrix Weekly",
                QuestionText = "¿Están preparando actualizaciones importantes para las próximas carreras?",
                Context = "Desarrollo técnico",
                Responses = new List<PressResponse>
                {
                    new PressResponse {
                        Label = "Confirmar mejoras",
                        FullResponse = "Sí, traeremos un paquete importante pronto.",
                        PilotMoodEffect = 5, PressRelationEffect = 5,
                        FIAAttentionEffect = 3, TeamMoraleEffect = 5,
                        ToneTag = "Honest"
                    },
                    new PressResponse {
                        Label = "Mantener secreto",
                        FullResponse = "No puedo revelar detalles técnicos. Nuestro trabajo habla por sí solo.",
                        PilotMoodEffect = 0, PressRelationEffect = -3,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 3,
                        ToneTag = "Evasive"
                    },
                    new PressResponse {
                        Label = "Desinformar",
                        FullResponse = "Estamos satisfechos con el paquete actual. Sin grandes cambios previstos.",
                        PilotMoodEffect = -3, PressRelationEffect = 0,
                        FIAAttentionEffect = -2, TeamMoraleEffect = 0,
                        ToneTag = "Diplomatic"
                    },
                    new PressResponse {
                        Label = "Presionar rivales",
                        FullResponse = "Lo que traemos va a sorprender a más de uno. Prepárense.",
                        PilotMoodEffect = 5, PressRelationEffect = 3,
                        FIAAttentionEffect = 5, TeamMoraleEffect = 8,
                        ToneTag = "Aggressive"
                    }
                }
            };
        }

        private PressQuestion GenerateFinancialQuestion(TeamData team)
        {
            return new PressQuestion
            {
                QuestionId = $"q_finance_{_rng.Next(10000)}",
                JournalistName = RandomJournalist(),
                MediaOutlet = "Racing Tribune",
                QuestionText = "Se habla de dificultades financieras en su equipo. ¿Es verdad?",
                Context = "Crisis financiera activa",
                Responses = new List<PressResponse>
                {
                    new PressResponse {
                        Label = "Negar firmemente",
                        FullResponse = "Absolutamente falso. Nuestras finanzas son sólidas.",
                        PilotMoodEffect = 3, PressRelationEffect = -5,
                        FIAAttentionEffect = 3, TeamMoraleEffect = 3,
                        ToneTag = "Aggressive"
                    },
                    new PressResponse {
                        Label = "Ser transparente",
                        FullResponse = "Es verdad que hay desafíos, pero estamos gestionándolos con responsabilidad.",
                        PilotMoodEffect = -5, PressRelationEffect = 10,
                        FIAAttentionEffect = 0, TeamMoraleEffect = -3,
                        ToneTag = "Honest"
                    },
                    new PressResponse {
                        Label = "Desviar",
                        FullResponse = "No voy a discutir las finanzas del equipo en público.",
                        PilotMoodEffect = -3, PressRelationEffect = -8,
                        FIAAttentionEffect = 5, TeamMoraleEffect = -2,
                        ToneTag = "Evasive"
                    },
                    new PressResponse {
                        Label = "Buscar apoyo",
                        FullResponse = "Estamos abiertos a nuevos inversores que crean en el proyecto.",
                        PilotMoodEffect = -5, PressRelationEffect = 5,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 0,
                        ToneTag = "Diplomatic"
                    }
                }
            };
        }

        private PressQuestion GenerateAggregatedToneQuestion(TeamData team)
        {
            return new PressQuestion
            {
                QuestionId = $"q_tone_{_rng.Next(10000)}",
                JournalistName = RandomJournalist(),
                MediaOutlet = "Racing Tribune",
                QuestionText = "Su estilo agresivo con la prensa ha generado controversia. " +
                    "¿Va a cambiar de actitud?",
                Context = "Historial de respuestas agresivas",
                Responses = new List<PressResponse>
                {
                    new PressResponse {
                        Label = "Doblar la apuesta",
                        FullResponse = "Digo lo que pienso. Si eso molesta, no es mi problema.",
                        PilotMoodEffect = 0, PressRelationEffect = -15,
                        FIAAttentionEffect = 3, TeamMoraleEffect = 5,
                        ToneTag = "Aggressive"
                    },
                    new PressResponse {
                        Label = "Disculparse",
                        FullResponse = "Quizás fui demasiado duro. Intentaré ser más constructivo.",
                        PilotMoodEffect = 0, PressRelationEffect = 15,
                        FIAAttentionEffect = -3, TeamMoraleEffect = 0,
                        ToneTag = "Diplomatic"
                    }
                }
            };
        }

        private PressQuestion GenerateInsistentQuestion(TeamData team)
        {
            return new PressQuestion
            {
                QuestionId = $"q_insist_{_rng.Next(10000)}",
                JournalistName = RandomJournalist(),
                MediaOutlet = "Formula Insider",
                QuestionText = "Usted suele evitar las preguntas difíciles. ¿Qué nos oculta?",
                Context = "Historial de respuestas evasivas",
                Responses = new List<PressResponse>
                {
                    new PressResponse {
                        Label = "Dar la cara",
                        FullResponse = "No oculto nada. Haré un esfuerzo por ser más abierto.",
                        PilotMoodEffect = 0, PressRelationEffect = 15,
                        FIAAttentionEffect = 0, TeamMoraleEffect = 0,
                        ToneTag = "Honest"
                    },
                    new PressResponse {
                        Label = "Seguir evadiendo",
                        FullResponse = "Mi trabajo es dirigir el equipo, no dar entrevistas.",
                        PilotMoodEffect = 0, PressRelationEffect = -10,
                        FIAAttentionEffect = 5, TeamMoraleEffect = 0,
                        ToneTag = "Evasive"
                    }
                }
            };
        }

        // ══════════════════════════════════════════════════════
        // RESOLUCIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Resuelve una pregunta de la conferencia con la respuesta elegida.
        /// Aplica todos los efectos de la respuesta.
        /// </summary>
        public void ResolveQuestion(PressConferenceEvent conference,
            int questionIndex, int responseIndex,
            List<PilotData> teamPilots, TeamData team)
        {
            if (questionIndex >= conference.Questions.Count) return;
            var question = conference.Questions[questionIndex];
            if (responseIndex >= question.Responses.Count) return;
            var response = question.Responses[responseIndex];

            // Registrar tono en historial
            if (_toneHistory.ContainsKey(response.ToneTag))
                _toneHistory[response.ToneTag]++;

            // Aplicar efectos a pilotos del equipo
            foreach (var pilot in teamPilots)
            {
                pilot.moodValue += response.PilotMoodEffect;
                pilot.pressRelation += response.PressRelationEffect;
            }

            // Guardar respuesta
            conference.SelectedResponses.Add(
                $"Q{questionIndex}:{response.ToneTag}");
        }

        /// <summary>
        /// Marca la conferencia como completada
        /// </summary>
        public void CompleteConference(PressConferenceEvent conference)
        {
            conference.IsCompleted = true;
            _history.Add(conference);
        }

        /// <summary>¿Debe haber conferencia esta semana?</summary>
        public bool ShouldHaveConference(int currentWeek, int season)
        {
            // 3-4 por temporada: semanas ~5, ~15, ~25, ~35 (aprox)
            int thisSeasonConferences = 0;
            foreach (var c in _history)
                if (c.Season == season) thisSeasonConferences++;

            if (thisSeasonConferences >= 4) return false;

            // Cada ~8-10 semanas
            int targetWeeks = thisSeasonConferences * 10 + 5;
            return Math.Abs(currentWeek - targetWeeks) <= 1;
        }

        /// <summary>Obtiene historial de conferencias</summary>
        public List<PressConferenceEvent> GetHistory() => _history;

        private string RandomJournalist()
        {
            return JOURNALIST_NAMES[_rng.Next(JOURNALIST_NAMES.Length)];
        }
    }
}
