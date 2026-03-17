// ============================================================
// F1 Career Manager — IllegalPartDetector.cs
// Sistema de detección de trampas técnicas (Gray Areas)
// ============================================================

using System;
using UnityEngine;
using F1CareerManager.Core;
using F1CareerManager.AI.FIAAI;

namespace F1CareerManager.AI.RnDAI
{
    public class IllegalPartDetector : MonoBehaviour
    {
        public static IllegalPartDetector Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float baseDetectionChance = 10f; // 10% base por carrera

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Chequea si las piezas instaladas en un equipo son legales
        /// </summary>
        public bool CheckLegality(string teamId, float partRisk)
        {
            // El riesgo de detección sube si la pieza es muy agresiva (partRisk 0-100)
            float finalChance = baseDetectionChance + (partRisk * 0.5f);
            
            bool detected = UnityEngine.Random.Range(0f, 100f) < finalChance;
            
            if (detected && partRisk > 0)
            {
                Debug.LogWarning($"[FIA] 🚔 Piezas ilegales detectadas en el equipo {teamId}!");
                // SanctionSystem.Instance.ApplyFine(teamId, 10000000, "Violación técnica - Pieza ilegal");
                return false; // Ilegal
            }

            return true; // Pasa el escrutinio
        }
    }
}
