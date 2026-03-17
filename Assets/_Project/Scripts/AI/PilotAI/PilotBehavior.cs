// ============================================================
// F1 Career Manager — PilotBehavior.cs
// Lógica de comportamiento y decisiones del piloto — IA 1
// ============================================================
// DEPENDENCIAS: MoodSystem.cs, EventBus.cs, PilotData.cs,
//               TeamData.cs, Constants.cs
// EVENTOS QUE USA: Lee datos del pilot, delega a MoodSystem
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.AI.PilotAI
{
    /// <summary>
    /// Controla el comportamiento de un piloto individual.
    /// Gestiona: forma actual, desarrollo por edad, decisiones de carrera,
    /// y la relación entre sus stats y el rendimiento real.
    /// Cada piloto tiene su propia instancia.
    /// </summary>
    public class PilotBehavior
    {
        // ── Datos ────────────────────────────────────────────
        private PilotData _pilot;
        private MoodSystem _moodSystem;
        private Random _rng;

        // ── Accesores ────────────────────────────────────────
        public PilotData Pilot => _pilot;
        public MoodSystem Mood => _moodSystem;

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public PilotBehavior(PilotData pilot, Random rng = null)
        {
            _pilot = pilot;
            _rng = rng ?? new Random();
            _moodSystem = new MoodSystem(pilot, _rng);
        }

        // ══════════════════════════════════════════════════════
        // FORMA SEMANAL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Actualiza la forma actual del piloto (fluctúa semanalmente).
        /// La forma varía ±10 puntos con tendencia según resultados recientes
        /// y el estado emocional.
        /// </summary>
        /// <param name="recentResults">Lista de posiciones recientes (últimas 3 carreras)</param>
        public void UpdateWeeklyForm(List<int> recentResults)
        {
            // Variación base aleatoria: ±5
            int variation = _rng.Next(-5, 6);

            // Tendencia según resultados recientes
            if (recentResults != null && recentResults.Count > 0)
            {
                float avgPosition = 0f;
                foreach (int pos in recentResults)
                    avgPosition += pos;
                avgPosition /= recentResults.Count;

                // Buenos resultados (promedio < 5) → tendencia positiva
                if (avgPosition < 5f)
                    variation += _rng.Next(1, 5);
                // Malos resultados (promedio > 15) → tendencia negativa
                else if (avgPosition > 15f)
                    variation -= _rng.Next(1, 5);
            }

            // El humor afecta la forma
            if (_pilot.mood == "Happy")
                variation += 2;
            else if (_pilot.mood == "Furious" || _pilot.mood == "WantsOut")
                variation -= 3;

            // Aplicar con límites (60-100)
            _pilot.formCurrent = Clamp(_pilot.formCurrent + variation, 60, 100);
        }

        // ══════════════════════════════════════════════════════
        // DESARROLLO POR EDAD
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Procesa el desarrollo/declive del piloto al final de cada temporada.
        /// Antes del pico: stats crecen según growthRate y potencial.
        /// Después del pico: stats decrecen según declineRate.
        /// </summary>
        public void ProcessSeasonDevelopment()
        {
            if (_pilot.age < _pilot.peakAge)
            {
                // Fase de CRECIMIENTO
                GrowStats();
            }
            else if (_pilot.age > _pilot.peakAge)
            {
                // Fase de DECLIVE
                DeclineStats();
            }
            // En la edad pico: mantiene stats (posible mejora mínima)
            else
            {
                // Pequeño crecimiento en la edad pico si no alcanzó el potencial
                int avgStat = GetAverageMainStats();
                if (avgStat < _pilot.potential - 5)
                {
                    GrowSingleStat(1, 3);
                }
            }

            // Recalcular overall y estrellas
            _pilot.CalculateOverall();
            _pilot.CalculateStars();
        }

        /// <summary>
        /// Fase de crecimiento: mejora stats según potencial y tasa de crecimiento
        /// </summary>
        private void GrowStats()
        {
            int avgStat = GetAverageMainStats();

            // Cuánto le falta para llegar al potencial
            int gap = _pilot.potential - avgStat;
            if (gap <= 0) return;

            // Cantidad de crecimiento = gap * growthRate * factor aleatorio
            float growthBase = gap * _pilot.growthRate * 0.1f;
            int pointsToDistribute = Math.Max(1, (int)Math.Round(growthBase));

            // Limitar crecimiento máximo por temporada
            pointsToDistribute = Math.Min(pointsToDistribute, 8);

            // Distribuir puntos entre stats aleatorios
            for (int i = 0; i < pointsToDistribute; i++)
            {
                GrowSingleStat(1, 3);
            }
        }

        /// <summary>
        /// Fase de declive: reduce stats gradualmente
        /// </summary>
        private void DeclineStats()
        {
            int yearsOverPeak = _pilot.age - _pilot.peakAge;

            // Cantidad de declive = años sobre pico * tasa de declive
            float declineBase = yearsOverPeak * _pilot.declineRate * 0.5f;
            int pointsToLose = Math.Max(0, (int)Math.Round(declineBase));

            // Limitar declive
            pointsToLose = Math.Min(pointsToLose, 6);

            for (int i = 0; i < pointsToLose; i++)
            {
                DeclineSingleStat(1, 3);
            }

            // Pilotos veteranos pueden decidir retirarse
            if (_pilot.age >= 38 && GetAverageMainStats() < 65)
            {
                // Probabilidad de retiro aumenta con la edad
                float retireChance = (_pilot.age - 37) * 0.10f; // 10% por año desde 38
                if ((float)_rng.NextDouble() < retireChance)
                {
                    _pilot.isRetired = true;
                }
            }
        }

        /// <summary>
        /// Mejora un stat aleatorio entre min y max puntos
        /// </summary>
        private void GrowSingleStat(int min, int max)
        {
            int amount = _rng.Next(min, max + 1);
            int statIndex = _rng.Next(0, 10);

            switch (statIndex)
            {
                case 0: _pilot.speed = Clamp(_pilot.speed + amount, 0, _pilot.potential); break;
                case 1: _pilot.consistency = Clamp(_pilot.consistency + amount, 0, _pilot.potential); break;
                case 2: _pilot.rainSkill = Clamp(_pilot.rainSkill + amount, 0, _pilot.potential); break;
                case 3: _pilot.startSkill = Clamp(_pilot.startSkill + amount, 0, _pilot.potential); break;
                case 4: _pilot.defense = Clamp(_pilot.defense + amount, 0, _pilot.potential); break;
                case 5: _pilot.attack = Clamp(_pilot.attack + amount, 0, _pilot.potential); break;
                case 6: _pilot.tireManagement = Clamp(_pilot.tireManagement + amount, 0, _pilot.potential); break;
                case 7: _pilot.fuelManagement = Clamp(_pilot.fuelManagement + amount, 0, _pilot.potential); break;
                case 8: _pilot.concentration = Clamp(_pilot.concentration + amount, 0, _pilot.potential); break;
                case 9: _pilot.adaptability = Clamp(_pilot.adaptability + amount, 0, _pilot.potential); break;
            }
        }

        /// <summary>
        /// Reduce un stat aleatorio entre min y max puntos
        /// </summary>
        private void DeclineSingleStat(int min, int max)
        {
            int amount = _rng.Next(min, max + 1);
            int statIndex = _rng.Next(0, 10);

            switch (statIndex)
            {
                case 0: _pilot.speed = Clamp(_pilot.speed - amount, 30, 100); break;
                case 1: _pilot.consistency = Clamp(_pilot.consistency - amount, 30, 100); break;
                case 2: _pilot.rainSkill = Clamp(_pilot.rainSkill - amount, 30, 100); break;
                case 3: _pilot.startSkill = Clamp(_pilot.startSkill - amount, 30, 100); break;
                case 4: _pilot.defense = Clamp(_pilot.defense - amount, 30, 100); break;
                case 5: _pilot.attack = Clamp(_pilot.attack - amount, 30, 100); break;
                case 6: _pilot.tireManagement = Clamp(_pilot.tireManagement - amount, 30, 100); break;
                case 7: _pilot.fuelManagement = Clamp(_pilot.fuelManagement - amount, 30, 100); break;
                case 8: _pilot.concentration = Clamp(_pilot.concentration - amount, 30, 100); break;
                case 9: _pilot.adaptability = Clamp(_pilot.adaptability - amount, 30, 100); break;
            }
        }

        // ══════════════════════════════════════════════════════
        // CÁLCULO DE RENDIMIENTO EN CARRERA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Calcula el rendimiento real del piloto para una vuelta/sesión.
        /// Combina: stats del piloto + forma actual + humor + factor aleatorio
        /// </summary>
        /// <param name="carPerformance">Stats del auto (0-100)</param>
        /// <param name="circuitModifier">Modificador del circuito para este auto</param>
        /// <returns>Rendimiento total (valor más alto = más rápido)</returns>
        public float CalculateRacePerformance(int carPerformance, float circuitModifier)
        {
            // Base: promedio ponderado de stats relevantes para carrera
            float pilotBase = _pilot.speed * 0.25f +
                              _pilot.consistency * 0.15f +
                              _pilot.concentration * 0.10f +
                              _pilot.tireManagement * 0.10f +
                              _pilot.fuelManagement * 0.05f +
                              _pilot.defense * 0.10f +
                              _pilot.attack * 0.10f +
                              _pilot.adaptability * 0.05f +
                              _pilot.startSkill * 0.05f +
                              _pilot.rainSkill * 0.05f;

            // Factor forma actual (60-100 normalizado a 0.6-1.0)
            float formFactor = _pilot.formCurrent / 100f;

            // Factor humor (del MoodSystem)
            float moodModifier = _moodSystem.GetPerformanceModifier();

            // Factor auto (0-100 normalizado)
            float carFactor = carPerformance / 100f;

            // Factor aleatorio (±5% variación vuelta a vuelta)
            float randomFactor = 1f + ((float)_rng.NextDouble() * 0.10f - 0.05f);

            // Rendimiento total
            float totalPerformance = (pilotBase * 0.45f + carPerformance * 0.55f)
                                     * formFactor
                                     * (1f + moodModifier)
                                     * (1f + circuitModifier * 0.01f)
                                     * randomFactor;

            return totalPerformance;
        }

        /// <summary>
        /// Calcula el rendimiento específico para clasificación.
        /// La velocidad pura y la concentración pesan más.
        /// </summary>
        public float CalculateQualifyingPerformance(int carPerformance, float circuitModifier)
        {
            float pilotBase = _pilot.speed * 0.35f +
                              _pilot.consistency * 0.10f +
                              _pilot.concentration * 0.15f +
                              _pilot.adaptability * 0.10f +
                              _pilot.attack * 0.10f +
                              _pilot.rainSkill * 0.05f +
                              _pilot.tireManagement * 0.05f +
                              _pilot.defense * 0.05f +
                              _pilot.fuelManagement * 0.03f +
                              _pilot.startSkill * 0.02f;

            float formFactor = _pilot.formCurrent / 100f;
            float moodModifier = _moodSystem.GetPerformanceModifier();
            float randomFactor = 1f + ((float)_rng.NextDouble() * 0.08f - 0.04f);

            float totalPerformance = (pilotBase * 0.40f + carPerformance * 0.60f)
                                     * formFactor
                                     * (1f + moodModifier)
                                     * (1f + circuitModifier * 0.01f)
                                     * randomFactor;

            return totalPerformance;
        }

        /// <summary>
        /// Calcula el rendimiento bajo lluvia (usa el stat de lluvia).
        /// </summary>
        public float CalculateWetPerformance(int carPerformance, float circuitModifier)
        {
            float pilotBase = _pilot.rainSkill * 0.30f +
                              _pilot.speed * 0.15f +
                              _pilot.consistency * 0.15f +
                              _pilot.concentration * 0.15f +
                              _pilot.adaptability * 0.10f +
                              _pilot.tireManagement * 0.10f +
                              _pilot.defense * 0.05f;

            float formFactor = _pilot.formCurrent / 100f;
            float moodModifier = _moodSystem.GetPerformanceModifier();
            // Mayor variación bajo lluvia (±8%)
            float randomFactor = 1f + ((float)_rng.NextDouble() * 0.16f - 0.08f);

            float totalPerformance = (pilotBase * 0.50f + carPerformance * 0.50f)
                                     * formFactor
                                     * (1f + moodModifier)
                                     * (1f + circuitModifier * 0.01f)
                                     * randomFactor;

            return totalPerformance;
        }

        /// <summary>
        /// Calcula el rendimiento específico de salida/primera vuelta
        /// </summary>
        public float CalculateStartPerformance()
        {
            float startBase = _pilot.startSkill * 0.40f +
                              _pilot.attack * 0.25f +
                              _pilot.concentration * 0.15f +
                              _pilot.speed * 0.10f +
                              _pilot.adaptability * 0.10f;

            // Las salidas tienen mucha variación
            float randomFactor = 1f + ((float)_rng.NextDouble() * 0.20f - 0.10f);

            return startBase * randomFactor;
        }

        // ══════════════════════════════════════════════════════
        // PROBABILIDAD DE ERROR
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Determina si el piloto comete un error en una vuelta determinada.
        /// La probabilidad base es inversamente proporcional a la consistencia.
        /// El humor Furioso aumenta los errores un 15%.
        /// </summary>
        /// <param name="vueltaActual">Vuelta actual</param>
        /// <param name="totalVueltas">Total de vueltas de la carrera</param>
        /// <returns>true si comete un error, false si no</returns>
        public bool RollForError(int vueltaActual, int totalVueltas)
        {
            // Probabilidad base: (100 - consistencia) / 1000
            // Ej: consistencia 95 = 0.5% chance, consistencia 60 = 4% chance
            float baseChance = (100 - _pilot.consistency) / 1000f;

            // Factor de fatiga: aumenta en las últimas vueltas
            float fatigueRatio = (float)vueltaActual / totalVueltas;
            if (fatigueRatio > 0.75f)
            {
                // Concentración reduce fatiga
                float fatiguePenalty = (1f - _pilot.concentration / 100f) * 0.02f;
                baseChance += fatiguePenalty;
            }

            // Bonus/penalización por humor
            baseChance += _moodSystem.GetErrorChanceModifier();

            // Roll
            return (float)_rng.NextDouble() < baseChance;
        }

        // ══════════════════════════════════════════════════════
        // DECISIONES DE CARRERA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// El piloto decide si intenta un adelantamiento basado en sus stats.
        /// </summary>
        /// <param name="gapToCarAhead">Diferencia de rendimiento con el auto de adelante</param>
        /// <param name="overtakingDifficulty">Dificultad de adelantar en este circuito (0-100)</param>
        /// <returns>true si intenta adelantar</returns>
        public bool DecideOvertakeAttempt(float gapToCarAhead, int overtakingDifficulty)
        {
            // Factor de agresividad basado en attack
            float aggressiveness = _pilot.attack / 100f;

            // Factor de circuito (más difícil = menos probable)
            float circuitFactor = 1f - (overtakingDifficulty / 100f);

            // ¿Está lo suficientemente cerca?
            float proximityFactor = gapToCarAhead > 0 ? 1f : 0.3f;

            // Ego alto → más agresivo
            float egoFactor = _pilot.ego > 70 ? 1.2f : 1.0f;

            // Humor furioso → más agresivo pero más propenso a errores
            float moodFactor = (_pilot.mood == "Furious") ? 1.3f : 1.0f;

            float overtakeChance = aggressiveness * circuitFactor * proximityFactor
                                   * egoFactor * moodFactor;

            return (float)_rng.NextDouble() < overtakeChance;
        }

        /// <summary>
        /// Decide si el piloto defiende agresivamente su posición
        /// </summary>
        public bool DecideAggressiveDefense()
        {
            float defenseProb = _pilot.defense / 100f;
            float egoFactor = _pilot.ego > 60 ? 1.15f : 1.0f;

            return (float)_rng.NextDouble() < (defenseProb * egoFactor);
        }

        // ══════════════════════════════════════════════════════
        // GESTIÓN DE NEUMÁTICOS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Calcula la velocidad de degradación de neumáticos del piloto.
        /// Valor bajo = cuida más los neumáticos = stints más largos.
        /// </summary>
        /// <returns>Factor de degradación (0.5 = muy cuidadoso, 1.5 = muy agresivo)</returns>
        public float GetTireDegradationRate()
        {
            // tireManagement 100 → 0.5x degradación
            // tireManagement 50 → 1.0x degradación
            // tireManagement 0 → 1.5x degradación
            float factor = 1.5f - (_pilot.tireManagement / 100f);

            // Humor afecta: furioso es más agresivo con los neumáticos
            if (_pilot.mood == "Furious" || _pilot.mood == "WantsOut")
                factor *= 1.1f;

            return factor;
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene el promedio de los stats principales
        /// </summary>
        public int GetAverageMainStats()
        {
            return (int)((_pilot.speed + _pilot.consistency + _pilot.rainSkill +
                         _pilot.startSkill + _pilot.defense + _pilot.attack +
                         _pilot.tireManagement + _pilot.fuelManagement +
                         _pilot.concentration + _pilot.adaptability) / 10f);
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
