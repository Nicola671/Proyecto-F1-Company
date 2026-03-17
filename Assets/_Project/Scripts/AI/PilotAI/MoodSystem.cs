// ============================================================
// F1 Career Manager — MoodSystem.cs
// Sistema de humor del piloto — IA 1 (parte emocional)
// Calcula y actualiza el estado emocional según eventos
// ============================================================
// DEPENDENCIAS: EventBus.cs, PilotData.cs, Constants.cs
// EVENTOS QUE DISPARA: OnPilotMoodChanged
// EVENTOS QUE ESCUCHA: OnRaceFinished, OnContractSigned,
//                      OnNewsGenerated, OnComponentInstalled
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.AI.PilotAI
{
    /// <summary>
    /// Gestiona el humor de un piloto individual. Cada piloto tiene su
    /// propia instancia de MoodSystem que reacciona a los eventos del juego.
    /// El humor es un valor continuo (-100 a +100) que se mapea a estados discretos.
    /// </summary>
    public class MoodSystem
    {
        // ── Datos ────────────────────────────────────────────
        private PilotData _pilot;
        private EventBus _eventBus;
        private Random _rng;

        // ── Umbrales de humor ────────────────────────────────
        // moodValue: -100 a +100
        // Feliz: >= 50  |  Neutro: 10 a 49  |  Molesto: -29 a 9
        // Furioso: -69 a -30  |  Quiere irse: <= -70
        private const int THRESHOLD_HAPPY = 50;
        private const int THRESHOLD_NEUTRAL = 10;
        private const int THRESHOLD_UPSET = -30;
        private const int THRESHOLD_FURIOUS = -70;

        // ── Cambios de humor por evento ──────────────────────
        private const int MOOD_WIN = 25;                // Victoria
        private const int MOOD_PODIUM = 15;             // Podio (P2-P3)
        private const int MOOD_POINTS = 5;              // Puntos (P4-P10)
        private const int MOOD_NO_POINTS = -5;          // Sin puntos (P11-P15)
        private const int MOOD_BAD_RESULT = -12;        // Mal resultado (P16-P20)
        private const int MOOD_DNF = -18;               // Abandono
        private const int MOOD_TEAMMATE_BEAT = -8;      // El compañero le ganó
        private const int MOOD_TEAMMATE_BEATEN = 5;     // Le ganó al compañero
        private const int MOOD_CONTRACT_RENEWAL = 15;   // Renovación de contrato
        private const int MOOD_GOOD_COMPONENT = 8;      // Mejora en el auto
        private const int MOOD_BAD_COMPONENT = -10;     // El auto empeoró
        private const int MOOD_NEGATIVE_NEWS = -7;      // Noticia negativa sobre él
        private const int MOOD_POSITIVE_NEWS = 5;       // Noticia positiva sobre él
        private const int MOOD_PROMISE_BROKEN = -20;    // Promesa rota (rol, auto, etc.)
        private const int MOOD_FAVORITE_TEAMMATE = -10; // Compañero es favoreció
        private const int MOOD_NATURAL_DECAY = -2;      // Decaimiento natural por tick
        private const int MOOD_GOOD_AUTO = 10;          // Auto competitivo
        private const int MOOD_BAD_AUTO = -15;          // Auto malo vs expectativa

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public MoodSystem(PilotData pilot, Random rng = null)
        {
            _pilot = pilot;
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
        }

        // ══════════════════════════════════════════════════════
        // ACTUALIZACIÓN PRINCIPAL DEL HUMOR
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Aplica un cambio de humor al piloto y dispara el evento si
        /// el estado cambia de categoría (ej: de Neutro a Molesto)
        /// </summary>
        /// <param name="change">Cantidad de cambio (-100 a +100)</param>
        /// <param name="reason">Razón legible del cambio</param>
        public void ApplyMoodChange(int change, string reason)
        {
            string previousMood = _pilot.mood;
            int previousValue = _pilot.moodValue;

            // Aplicar cambio con clamp
            _pilot.moodValue = Clamp(_pilot.moodValue + change, -100, 100);

            // Actualizar el estado de humor según umbrales
            string newMood = CalculateMoodState(_pilot.moodValue);
            _pilot.mood = newMood;

            // Si el estado cambió de categoría, disparar evento
            if (previousMood != newMood)
            {
                _eventBus.FirePilotMoodChanged(new EventBus.PilotMoodChangedArgs
                {
                    PilotId = _pilot.id,
                    PilotName = $"{_pilot.firstName} {_pilot.lastName}",
                    TeamId = _pilot.currentTeamId,
                    PreviousMood = previousMood,
                    NewMood = newMood,
                    MoodValue = _pilot.moodValue,
                    Reason = reason
                });
            }
        }

        /// <summary>
        /// Mapea un valor numérico de humor al estado de humor correspondiente
        /// </summary>
        private string CalculateMoodState(int moodValue)
        {
            if (moodValue >= THRESHOLD_HAPPY) return "Happy";
            if (moodValue >= THRESHOLD_NEUTRAL) return "Neutral";
            if (moodValue >= THRESHOLD_UPSET) return "Upset";
            if (moodValue >= THRESHOLD_FURIOUS) return "Furious";
            return "WantsOut";
        }

        // ══════════════════════════════════════════════════════
        // PROCESAMIENTO DE EVENTOS ESPECÍFICOS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Procesa el resultado de una carrera para este piloto
        /// </summary>
        public void ProcessRaceResult(int position, bool dnf, string dnfReason,
            int teammatePosition, bool teammateDnf)
        {
            // Efecto base del resultado
            if (dnf)
            {
                ApplyMoodChange(MOOD_DNF, $"Abandono por {dnfReason}");
            }
            else if (position == 1)
            {
                ApplyMoodChange(MOOD_WIN, "¡Victoria!");
            }
            else if (position <= 3)
            {
                ApplyMoodChange(MOOD_PODIUM, $"Podio (P{position})");
            }
            else if (position <= 10)
            {
                ApplyMoodChange(MOOD_POINTS, $"Puntos (P{position})");
            }
            else if (position <= 15)
            {
                ApplyMoodChange(MOOD_NO_POINTS, $"Fuera de puntos (P{position})");
            }
            else
            {
                ApplyMoodChange(MOOD_BAD_RESULT, $"Mal resultado (P{position})");
            }

            // Comparación con compañero de equipo
            if (!dnf && !teammateDnf)
            {
                if (position > teammatePosition)
                {
                    // El compañero le ganó
                    int egoMultiplier = _pilot.ego > 70 ? 2 : 1;
                    ApplyMoodChange(MOOD_TEAMMATE_BEAT * egoMultiplier,
                        "El compañero terminó por delante");

                    // Deteriorar relación con compañero
                    _pilot.teammateRelation = Clamp(_pilot.teammateRelation - 5, -100, 100);
                }
                else
                {
                    ApplyMoodChange(MOOD_TEAMMATE_BEATEN,
                        "Terminó por delante de su compañero");

                    _pilot.teammateRelation = Clamp(_pilot.teammateRelation + 2, -100, 100);
                }
            }
        }

        /// <summary>
        /// Procesa un cambio en el auto (mejora o empeoramiento)
        /// </summary>
        public void ProcessCarChange(string installResult, string componentName)
        {
            switch (installResult)
            {
                case "BetterThanExpected":
                    ApplyMoodChange(MOOD_GOOD_COMPONENT + 5,
                        $"El componente {componentName} superó expectativas");
                    break;
                case "AsExpected":
                    ApplyMoodChange(MOOD_GOOD_COMPONENT,
                        $"Nueva mejora en el auto: {componentName}");
                    break;
                case "WorseThanExpected":
                    ApplyMoodChange(MOOD_BAD_COMPONENT,
                        $"El componente {componentName} no funcionó como se esperaba");
                    break;
                case "Failed":
                    ApplyMoodChange(MOOD_BAD_COMPONENT * 2,
                        $"El componente {componentName} dañó el auto");
                    break;
            }
        }

        /// <summary>
        /// Procesa una noticia que menciona al piloto
        /// </summary>
        public void ProcessNewsAboutPilot(string newsType, bool isPositive)
        {
            if (isPositive)
            {
                ApplyMoodChange(MOOD_POSITIVE_NEWS, "Cobertura positiva en la prensa");

                // Mejorar relación con la prensa
                _pilot.pressRelation = Clamp(_pilot.pressRelation + 3, 0, 100);
            }
            else
            {
                // El ego amplifica el impacto negativo
                int egoMultiplier = _pilot.ego > 60 ? 2 : 1;
                ApplyMoodChange(MOOD_NEGATIVE_NEWS * egoMultiplier,
                    "Cobertura negativa en la prensa");

                _pilot.pressRelation = Clamp(_pilot.pressRelation - 5, 0, 100);
            }
        }

        /// <summary>
        /// Procesa una promesa rota por el equipo
        /// </summary>
        public void ProcessBrokenPromise(string promiseType)
        {
            int egoMultiplier = _pilot.ego > 70 ? 2 : 1;
            ApplyMoodChange(MOOD_PROMISE_BROKEN * egoMultiplier,
                $"Promesa rota: {promiseType}");

            // La lealtad baja significativamente
            _pilot.loyalty = Clamp(_pilot.loyalty - 15, 0, 100);
        }

        /// <summary>
        /// Procesa la renovación/firma de un contrato
        /// </summary>
        public void ProcessContractSigned(bool isRenewal, string role)
        {
            int bonus = isRenewal ? MOOD_CONTRACT_RENEWAL : MOOD_CONTRACT_RENEWAL + 5;

            // Si le dieron el rol que quería
            if (role == "First" && _pilot.ego > 60)
                bonus += 10;

            ApplyMoodChange(bonus,
                isRenewal ? "Renovación de contrato" : "Nuevo contrato firmado");

            _pilot.loyalty = Clamp(_pilot.loyalty + 10, 0, 100);
        }

        /// <summary>
        /// Procesa que el compañero fue favorecido en estrategia
        /// </summary>
        public void ProcessTeammateFavored()
        {
            int egoMultiplier = _pilot.ego > 60 ? 2 : 1;
            ApplyMoodChange(MOOD_FAVORITE_TEAMMATE * egoMultiplier,
                "El equipo favoreció al compañero");

            _pilot.teammateRelation = Clamp(_pilot.teammateRelation - 10, -100, 100);
        }

        // ══════════════════════════════════════════════════════
        // ACTUALIZACIÓN PERIÓDICA (entre carreras)
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Actualización semanal del humor. Debe llamarse una vez por semana
        /// de juego (entre carreras). Aplica decaimiento natural y variación.
        /// </summary>
        /// <param name="carPerformanceVsExpected">
        /// Diferencia entre el rendimiento real del auto y lo que el piloto espera.
        /// Positivo = auto mejor de lo esperado. Negativo = auto peor.
        /// </param>
        public void WeeklyUpdate(int carPerformanceVsExpected)
        {
            // Decaimiento natural (tiende al neutro con el tiempo)
            if (_pilot.moodValue > 20)
                ApplyMoodChange(MOOD_NATURAL_DECAY, "Paso del tiempo");
            else if (_pilot.moodValue < -20)
                ApplyMoodChange(-MOOD_NATURAL_DECAY, "Paso del tiempo");

            // Efecto del auto vs expectativas
            if (carPerformanceVsExpected > 10)
                ApplyMoodChange(3, "El auto está mejor de lo esperado");
            else if (carPerformanceVsExpected < -10)
                ApplyMoodChange(-5, "El auto está por debajo de las expectativas");

            // Variación aleatoria pequeña (la vida no es predecible)
            int randomVariation = _rng.Next(-3, 4); // -3 a +3
            if (randomVariation != 0)
                ApplyMoodChange(randomVariation, "Variación natural");

            // Si el piloto quiere irse, la lealtad baja cada semana
            if (_pilot.mood == "WantsOut")
            {
                _pilot.loyalty = Clamp(_pilot.loyalty - 3, 0, 100);
            }
        }

        // ══════════════════════════════════════════════════════
        // OBTENCIÓN DE MODIFICADORES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene el modificador de rendimiento actual según el humor.
        /// Usar en la simulación de carrera.
        /// </summary>
        public float GetPerformanceModifier()
        {
            return _pilot.GetMoodModifier();
        }

        /// <summary>
        /// Obtiene la probabilidad extra de error según el humor.
        /// Solo aplica si está Furioso.
        /// </summary>
        public float GetErrorChanceModifier()
        {
            if (_pilot.mood == "Furious")
                return Constants.MOOD_FURIOUS_ERROR_INCREASE;
            if (_pilot.mood == "WantsOut")
                return Constants.MOOD_FURIOUS_ERROR_INCREASE * 0.8f;
            return 0f;
        }

        /// <summary>
        /// Indica si el piloto está dispuesto a buscar otros equipos
        /// </summary>
        public bool IsLookingToLeave()
        {
            return _pilot.mood == "WantsOut" ||
                   (_pilot.mood == "Furious" && _pilot.loyalty < 30);
        }

        /// <summary>
        /// Obtiene el modificador de salario que el piloto pide según su ego
        /// Ego > 80 = pide 20% más, Ego > 60 = pide 10% más
        /// </summary>
        public float GetSalaryDemandModifier()
        {
            if (_pilot.ego > 80) return 1.20f;
            if (_pilot.ego > 60) return 1.10f;
            if (_pilot.ego > 40) return 1.05f;
            return 1.0f;
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
