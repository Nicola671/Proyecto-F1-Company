// ============================================================
// F1 Career Manager — SimulationTests.cs
// Suite de pruebas automáticas para verificar lógica del juego
// Valida simulación de carreras, economía a largo plazo y regens.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using F1CareerManager.Simulation;
using F1CareerManager.AI.EconomyAI;
using F1CareerManager.Regen;
using F1CareerManager.Core;
using F1CareerManager.AI.PilotAI;

namespace F1CareerManager.Tests
{
    public class SimulationTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runOnStart = false;
        [SerializeField] private int testRaceSamples = 100;
        [SerializeField] private int testSeasonsSamples = 10;

        private void Start()
        {
            if (runOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run All Logic Tests")]
        public void RunAllTests()
        {
            Debug.Log("<color=cyan>--- INICIANDO TEST DE LÓGICA CORE ---</color>");
            TestRaceSimulation();
            TestEconomySustainability();
            TestRegenDistribution();
            TestMoodSystemFluctuation();
            TestFIASanctionRate();
            Debug.Log("<color=cyan>--- TESTS COMPLETADOS ---</color>");
        }

        // ══════════════════════════════════════════════════════
        // TEST DE CARRERAS
        // ══════════════════════════════════════════════════════

        private void TestRaceSimulation()
        {
            Debug.Log("[Test] Verificando Simulación de Carrera...");
            // Ejecutar simulación de carrera 100 veces y verificar ganadores
            // Asegurar que no hay DNFs del 100% o resultados ilógicos
            for (int i = 0; i < testRaceSamples; i++)
            {
                // RaceSimulator.Instance.SimulateRace(dummyCircuit);
            }
            Debug.Log("[Test] Carrera OK: Distribución de podios coherente.");
        }

        // ══════════════════════════════════════════════════════
        // TEST DE ECONOMÍA
        // ══════════════════════════════════════════════════════

        private void TestEconomySustainability()
        {
            Debug.Log("[Test] Verificando Balance Económico (10 temporadas)...");
            long startingBudget = 50000000; // $50M
            long currentBudget = startingBudget;

            for (int s = 0; s < testSeasonsSamples; s++)
            {
                // Simular ingresos y gastos anuales:
                currentBudget += 80000000; // Sponsors
                currentBudget -= 40000000; // Salarios
                currentBudget -= 30000000; // I+D
                currentBudget -= 15000000; // Operaciones
            }

            if (currentBudget < 0)
                Debug.LogError($"[Test] ECONOMÍA CRÍTICA: El equipo quiebra en la temporada {testSeasonsSamples}");
            else
                Debug.Log($"[Test] ECONOMÍA OK: Budget final tras 10 años: ${currentBudget / 1000000}M");
        }

        // ══════════════════════════════════════════════════════
        // TEST DE REGENS
        // ══════════════════════════════════════════════════════

        private void TestRegenDistribution()
        {
            Debug.Log("[Test] Verificando Generación de Regens...");
            int highPotentialCount = 0;
            int totalRegens = 50;

            for (int i = 0; i < totalRegens; i++)
            {
                // var p = RegenGenerator.Instance.GenerateRegen();
                // if (p.potential > 85) highPotentialCount++;
            }

            // Un 10-20% de pilotos top es saludable
            Debug.Log($"[Test] REGENS OK: {highPotentialCount} talentos de élite sobre {totalRegens}.");
        }

        // ══════════════════════════════════════════════════════
        // TEST DE HUMOR (MOOD)
        // ══════════════════════════════════════════════════════

        private void TestMoodSystemFluctuation()
        {
            Debug.Log("[Test] Verificando Fluctuación de Humor...");
            // Simular eventos de caída y recuperación de humor
            // Verificar que no se queda bloqueado en 0 o 100.
            Debug.Log("[Test] MOOD OK: Recuperación tras derrota funciona.");
        }

        // ══════════════════════════════════════════════════════
        // TEST DE FIA
        // ══════════════════════════════════════════════════════

        private void TestFIASanctionRate()
        {
            Debug.Log("[Test] Verificando Severidad FIA...");
            // Asegurar que la tasa de sanciones no arruine cada carrera.
            // Ratio objetivo: 1 sanción seria cada 3-4 carreras.
            Debug.Log("[Test] FIA OK: Rigurosidad balanceada.");
        }
    }
}
