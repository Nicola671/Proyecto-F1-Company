// ============================================================
// F1 Career Manager — EngineSupplierSystem.cs
// Gestión de suministradores de motores entre equipos
// Afecta potencia, fiabilidad y costo de la unidad de potencia.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.AI.EconomyAI
{
    public class EngineSupplierSystem : MonoBehaviour
    {
        public static EngineSupplierSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private long baseEngineCost = 15000000; // $15M anual

        // ── Estado ───────────────────────────────────────────
        private Dictionary<string, string> suppliers = new Dictionary<string, string>(); // teamId, supplierId
        private List<string> engineManufacturers = new List<string> { "mercedes", "ferrari", "honda", "alpine", "audi", "ford" };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeSuppliers();
        }

        private void InitializeSuppliers()
        {
            // Mapeo inicial F1 2025 (Fiel a la realidad)
            suppliers["mercedes"] = "mercedes";
            suppliers["mclaren"] = "mercedes";
            suppliers["aston_martin"] = "mercedes";
            suppliers["williams"] = "mercedes";
            
            suppliers["ferrari"] = "ferrari";
            suppliers["haas"] = "ferrari";
            
            suppliers["red_bull"] = "honda";
            suppliers["racing_bulls"] = "honda";
            
            suppliers["alpine"] = "alpine";
            suppliers["audi"] = "audi";
        }

        // ══════════════════════════════════════════════════════
        // GESTIÓN DE SUMINISTRO
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene el ID del fabricante que suministra al equipo
        /// </summary>
        public string GetSupplierName(string teamId)
        {
            return suppliers.ContainsKey(teamId) ? suppliers[teamId] : "Independiente";
        }

        /// <summary>
        /// Calcula el bono de rendimiento del motor basado en el fabricante
        /// </summary>
        public int GetEnginePowerBonus(string teamId)
        {
            string supplier = GetSupplierName(teamId);
            // El fabricante siempre tiene +5 de potencia que el cliente
            bool isManufacturer = supplier == teamId;
            
            // Stats base por fabricante (Simulado)
            int power = supplier switch
            {
                "mercedes" => 92,
                "ferrari" => 95,
                "honda" => 94,
                "audi" => 90,
                "alpine" => 85,
                _ => 80
            };

            return isManufacturer ? power : power - 5;
        }

        /// <summary>
        /// Costo anual del suministro de motores para el equipo
        /// </summary>
        public long GetAnnualSupplyCost(string teamId)
        {
            string supplier = GetSupplierName(teamId);
            // El fabricante no paga suministro (ya paga I+D), el cliente sí.
            if (supplier == teamId) return 0;
            
            return baseEngineCost;
        }

        /// <summary>
        /// Cambia de suministrador de motores (Ej: Red Bull ficha a Ford para 2026)
        /// </summary>
        public void ChangeSupplier(string teamId, string newSupplierId)
        {
            if (engineManufacturers.Contains(newSupplierId))
            {
                suppliers[teamId] = newSupplierId;
                Debug.Log($"[EngineSystem] {teamId} ahora usa motores {newSupplierId}.");
            }
        }
    }
}
