// ============================================================
// F1 Career Manager — AcademyManager.cs
// Gestión de la Academia de Pilotos Junior
// Desarrolla regens antes de su ascenso a la F1.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using F1CareerManager.Core;
using F1CareerManager.Data;
using F1CareerManager.Regen;

namespace F1CareerManager.Academy
{
    public class AcademyManager : MonoBehaviour
    {
        public static AcademyManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxJuniorPilots = 4;
        [SerializeField] private long baseDevelopmentCostPerPilot = 500000; // $500k anual

        // ── Estado ───────────────────────────────────────────
        private List<JuniorPilotInfo> academyPilots = new List<JuniorPilotInfo>();
        private long currentAcademyBudget;

        [Serializable]
        public class JuniorPilotInfo
        {
            public PilotData pilotData;
            public string currentSeries; // F2, F3, Karting
            public int seasonsInAcademy;
            public int championshipPos; // Resultado simulado fuera de F1
            public float developmentRate; // 1.0f base, afectado por presupuesto
            public bool isReadyForF1;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
            Debug.Log("[AcademyManager] ✅ Inicializado");
            // Cargar estado inicial si existe
        }

        // ══════════════════════════════════════════════════════
        // GESTIÓN DE PILOTOS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Agrega un nuevo prospecto a la academia (usualmente generado por RegenGenerator)
        /// </summary>
        public bool AddToAcademy(PilotData pilot)
        {
            if (academyPilots.Count >= maxJuniorPilots)
            {
                Debug.LogWarning("[AcademyManager] Academia llena");
                return false;
            }

            var junior = new JuniorPilotInfo
            {
                pilotData = pilot,
                currentSeries = pilot.age < 16 ? "Karting" : (pilot.age < 18 ? "F3" : "F2"),
                seasonsInAcademy = 0,
                championshipPos = UnityEngine.Random.Range(1, 20),
                developmentRate = 1.0f,
                isReadyForF1 = false
            };

            academyPilots.Add(junior);
            Debug.Log($"[AcademyManager] {pilot.firstName} {pilot.lastName} (Age: {pilot.age}) se une a la Academia.");
            return true;
        }

        /// <summary>
        /// Actualización semanal de desarrollo
        /// </summary>
        public void WeeklyUpdate()
        {
            foreach (var junior in academyPilots)
            {
                ApplyPartialDevelopment(junior);
            }
        }

        private void ApplyPartialDevelopment(JuniorPilotInfo junior)
        {
            // El desarrollo en la academia es más rápido que en F1 (bonus de regen)
            float speedFactor = junior.developmentRate * 0.05f; 
            
            // Mejorar stats aleatoriamente basadas en potencial
            if (UnityEngine.Random.value < junior.pilotData.potential / 100f)
            {
                junior.pilotData.speed += (int)(UnityEngine.Random.Range(0, 2) * speedFactor);
                junior.pilotData.consistency += (int)(UnityEngine.Random.Range(0, 2) * speedFactor);
                // Limitar a stats máximos de junior
                junior.pilotData.speed = Mathf.Min(junior.pilotData.speed, 75); 
                junior.pilotData.consistency = Mathf.Min(junior.pilotData.consistency, 75);
            }
        }

        /// <summary>
        /// Simulación de fin de temporada para juniors
        /// </summary>
        public void ProcessSeasonEnd()
        {
            foreach (var junior in academyPilots)
            {
                junior.seasonsInAcademy++;
                junior.pilotData.age++;
                
                // Simular resultado en su categoría
                junior.championshipPos = UnityEngine.Random.Range(1, 22);
                
                // Promoción de categoría interna
                if (junior.currentSeries == "Karting" && junior.pilotData.age >= 16) junior.currentSeries = "F3";
                else if (junior.currentSeries == "F3" && junior.pilotData.age >= 18) junior.currentSeries = "F2";
                
                // Verificar si está listo para F1 (Superlicencia simulada)
                if (junior.pilotData.age >= 18 && (junior.championshipPos <= 5 || junior.seasonsInAcademy >= 3))
                {
                    junior.isReadyForF1 = true;
                    Debug.Log($"[AcademyManager] 🎓 {junior.pilotData.firstName} {junior.pilotData.lastName} está listo para dar el salto a F1.");
                }
            }
        }

        // ══════════════════════════════════════════════════════
        // ECONOMÍA
        // ══════════════════════════════════════════════════════

        public void SetAcademyBudget(long budget)
        {
            currentAcademyBudget = budget;
            // Afecta el developmentRate de todos los pilotos
            float targetRate = (float)budget / (Math.Max(1, academyPilots.Count) * baseDevelopmentCostPerPilot);
            foreach (var j in academyPilots) j.developmentRate = Mathf.Clamp(targetRate, 0.5f, 2.0f);
        }

        // ══════════════════════════════════════════════════════
        // API PÚBLICA
        // ══════════════════════════════════════════════════════

        public List<JuniorPilotInfo> GetAcademyPilots() => academyPilots;

        public void PromoteToF1(string pilotId, string teamId)
        {
            var junior = academyPilots.FirstOrDefault(p => p.pilotData.id == pilotId);
            if (junior != null && junior.isReadyForF1)
            {
                // Aquí se llamaría a TransferManager para asignarlo a un asiento real
                Debug.Log($"[AcademyManager] Promocionando a {junior.pilotData.firstName} {junior.pilotData.lastName} al equipo {teamId}");
                academyPilots.Remove(junior);
            }
        }
    }
}
