// ============================================================
// F1 Career Manager — EventBus.cs (Versión Final)
// Centraliza todos los eventos del juego para desacoplamiento.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace F1CareerManager.Core
{
    public class EventBus : MonoBehaviour
    {
        public static EventBus Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Eventos de Carrera ────────────────────────────────
        public event EventHandler<RaceFinishedArgs> OnRaceFinished;
        public event EventHandler<SprintFinishedArgs> OnSprintFinished;
        public event Action OnQualifyingFinished;

        // ── Eventos de Temporada ──────────────────────────────
        public event EventHandler<SeasonEndArgs> OnSeasonEnd;
        public event Action OnWeekAdvanced;
        public event Action OnGameReady;

        // ── Eventos de Personal & Mercado ────────────────────
        public event EventHandler<ContractSignedArgs> OnContractSigned;
        public event EventHandler<ContractSignedArgs> OnTransferCompleted;
        public event EventHandler<StaffChangedArgs> OnStaffChanged;
        public event Action<string> OnAcademyPilotPromoted; // pilotId

        // ── Eventos de IA & Alianzas ──────────────────────────
        public event Action<string, string> OnRivalryChanged; // pilot1, pilot2
        public event Action<string, string> OnEngineSupplierChanged; // teamId, supplierId

        // ── Eventos de R&D ────────────────────────────────────
        public event EventHandler<ComponentInstalledArgs> OnComponentInstalled;
        public event Action<string> OnComponentRiskDetected; // componentId

        // ── Eventos de Economía ───────────────────────────────
        public event EventHandler<BudgetChangedArgs> OnBudgetChanged;
        public event Action<string> OnAchievementUnlocked; // achievementId
        public event EventHandler<NewsGeneratedArgs> OnNewsGenerated;

        // ══════════════════════════════════════════════════════
        // CLASES DE ARGUMENTOS
        // ══════════════════════════════════════════════════════

        public class RaceFinishedArgs : EventArgs
        {
            public string CircuitId;
            public int RoundNumber;
            public string WinnerId;
            public string FastestLapId;
            public List<RacePositionInfo> FinalPositions;
            public List<string> Incidents;
            public bool HadRain;
            public int SafetyCars;
        }

        public class RacePositionInfo
        {
            public int Position;
            public string PilotId;
            public string TeamId;
            public int PointsEarned;
            public bool DNF;
            public string DNFReason;
        }

        public class QualifyingFinishedArgs : EventArgs
        {
            public string CircuitId;
            public int RoundNumber;
            public List<RacePositionInfo> Grid;
            public string PoleSitterId;
        }

        public class NewsGeneratedArgs : EventArgs
        {
            public string NewsId;
            public string Headline;
            public string Body;
            public string Type;
            public string MediaOutlet;
            public bool IsRumor;
            public bool IsTrue;
            public List<string> RelatedPilotIds;
            public List<string> RelatedTeamIds;
        }

        public class SprintFinishedArgs : EventArgs
        {
            public string CircuitId;
            public List<string> ScoredPointsPilotIds;
        }

        public class SeasonEndArgs : EventArgs
        {
            public int FinalPosition;
            public bool ObjectiveMet;
        }

        public class ContractSignedArgs : EventArgs
        {
            public string PilotId;
            public string TeamId;
            public bool IsSuccess;
        }

        public class StaffChangedArgs : EventArgs
        {
            public string Role;
            public string NewStaffName;
        }

        public class ComponentInstalledArgs : EventArgs
        {
            public string ComponentId;
            public float PerformanceGain;
        }

        public class BudgetChangedArgs : EventArgs
        {
            public long NewBalance;
            public long ChangeAmount;
            public string Reason;
        }

        // ══════════════════════════════════════════════════════
        // DISPARADORES (FIRE METHODS)
        // ══════════════════════════════════════════════════════

        public void FireGameReady() => OnGameReady?.Invoke();
        public void FireRaceFinished(RaceFinishedArgs args) => OnRaceFinished?.Invoke(this, args);
        public void FireSprintFinished(SprintFinishedArgs args) => OnSprintFinished?.Invoke(this, args);
        public void FireSeasonEnd(SeasonEndArgs args) => OnSeasonEnd?.Invoke(this, args);
        public void FireTransferCompleted(ContractSignedArgs args) => OnTransferCompleted?.Invoke(this, args);
        public void FireBudgetChanged(long balance, long change, string reason) => 
            OnBudgetChanged?.Invoke(this, new BudgetChangedArgs { NewBalance = balance, ChangeAmount = change, Reason = reason });
        
        public void FireQualifyingFinished(QualifyingFinishedArgs args) => OnQualifyingFinished?.Invoke(); // Mantengo el Action por compatibilidad o cambio a EventHandler si prefiero
        public void FireComponentInstalled(string compId, float gain) => OnComponentInstalled?.Invoke(this, new ComponentInstalledArgs { ComponentId = compId, PerformanceGain = gain });
        public void FireStaffChanged(string role, string name) => OnStaffChanged?.Invoke(this, new StaffChangedArgs { Role = role, NewStaffName = name });
        public void FireNewsGenerated(NewsGeneratedArgs args) => OnNewsGenerated?.Invoke(this, args);
        
        public void FireAcademyPromoted(string pilotId) => OnAcademyPilotPromoted?.Invoke(pilotId);
        public void FireRivalryChanged(string p1, string p2) => OnRivalryChanged?.Invoke(p1, p2);
        public void FireEngineChanged(string teamId, string supplierId) => OnEngineSupplierChanged?.Invoke(teamId, supplierId);
        public void FireAchievementUnlocked(string achievementId) => OnAchievementUnlocked?.Invoke(achievementId);

        public void ClearAllListeners()
        {
            OnRaceFinished = null;
            OnSprintFinished = null;
            OnQualifyingFinished = null;
            OnSeasonEnd = null;
            OnWeekAdvanced = null;
            OnGameReady = null;
            OnContractSigned = null;
            OnTransferCompleted = null;
            OnStaffChanged = null;
            OnAcademyPilotPromoted = null;
            OnRivalryChanged = null;
            OnEngineSupplierChanged = null;
            OnComponentInstalled = null;
            OnComponentRiskDetected = null;
            OnBudgetChanged = null;
            OnAchievementUnlocked = null;
        }
    }
}
