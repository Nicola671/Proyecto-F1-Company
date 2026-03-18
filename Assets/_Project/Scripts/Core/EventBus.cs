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

        // ── Eventos de Pilotos ────────────────────────────────
        public event EventHandler<PilotMoodChangedArgs> OnPilotMoodChanged;
        public event EventHandler<InjuryOccurredArgs> OnInjuryOccurred;

        // ── Eventos de FIA ────────────────────────────────────
        public event EventHandler<FIAInvestigationArgs> OnFIAInvestigation;

        // ── Eventos de Mercado de Rivales ─────────────────────
        public event EventHandler<RivalTransferArgs> OnRivalTransfer;

        // ── Eventos de Sistema / Estado ───────────────────────
        public event EventHandler<GameStateChangedArgs> OnGameStateChanged;
        public event EventHandler<RandomEventArgs> OnRandomEvent;

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
            public List<SeasonPilotResult> PilotResults;
        }

        public class SeasonPilotResult
        {
            public string PilotId;
            public int FinalPosition;
            public int Points;
        }

        public class ContractSignedArgs : EventArgs
        {
            public string PilotId;
            public string PilotName;
            public string TeamId;
            public string TeamName;
            public bool IsSuccess;
            public bool IsRenewal;
            public int Years;
        }

        public class StaffChangedArgs : EventArgs
        {
            public string Role;
            public string NewStaffName;
            public string StaffName;   // alias para compatibilidad con UIManager
            public string TeamId;
            public string ChangeType;  // "Hired", "Stolen", "Retired"
        }

        public class ComponentInstalledArgs : EventArgs
        {
            public string ComponentId;
            public string ComponentName;
            public float PerformanceGain;
            public float ActualPerformance;
            public string InstallResult;  // "Normal", "BetterThanExpected", "Failed"
        }

        public class BudgetChangedArgs : EventArgs
        {
            public long NewBalance;
            public float NewBudget;        // en millones (float) para UI
            public long ChangeAmount;
            public string Reason;
            public string TeamId;
            public string FinancialStatus; // "OK", "Struggling", "Crisis"
        }

        public class GameStateChangedArgs : EventArgs
        {
            public string PreviousState;
            public string NewState;
        }

        public class RandomEventArgs : EventArgs
        {
            public string EventId;
            public string Title;
            public string Description;
            public string EventType; // Positive, Negative, Neutral
            public string AffectedTeamId;
            public List<string> PlayerOptions;
        }

        // ── Args de Pilotos ───────────────────────────────────

        public class PilotMoodChangedArgs : EventArgs
        {
            public string PilotId;
            public string PilotName;
            public string TeamId;
            public string OldMood;
            public string NewMood;
            public string Reason;
        }

        public class InjuryOccurredArgs : EventArgs
        {
            public string PilotId;
            public string PilotName;
            public string TeamId;
            public string Description;
            public int RacesOut;
            public bool IsSerious;
        }

        // ── Args de FIA ───────────────────────────────────────

        public class FIAInvestigationArgs : EventArgs
        {
            public string TeamId;
            public string ComponentId;
            public string InvestigationReason;
            public bool WasDetected;
            public string SanctionType;   // "Pending", "Warning", "Fine", etc.
            public float FineAmount;
            public int PointsPenalty;
            public string Description;
        }

        // ── Args de Mercado ───────────────────────────────────

        public class RivalTransferArgs : EventArgs
        {
            public string PilotId;
            public string PilotName;
            public string FromTeamId;
            public string FromTeamName;
            public string ToTeamId;
            public string ToTeamName;
            public float Salary;
        }

        // ══════════════════════════════════════════════════════
        // DISPARADORES (FIRE METHODS)
        // ══════════════════════════════════════════════════════

        public void FireGameReady() => OnGameReady?.Invoke();
        public void FireRaceFinished(RaceFinishedArgs args) => OnRaceFinished?.Invoke(this, args);
        public void FireSprintFinished(SprintFinishedArgs args) => OnSprintFinished?.Invoke(this, args);
        public void FireSeasonEnd(SeasonEndArgs args) => OnSeasonEnd?.Invoke(this, args);
        public void FireTransferCompleted(ContractSignedArgs args) => OnTransferCompleted?.Invoke(this, args);
        public void FireContractSigned(ContractSignedArgs args) => OnContractSigned?.Invoke(this, args);

        public void FireBudgetChanged(long balance, long change, string reason,
            string teamId = "", string status = "OK") =>
            OnBudgetChanged?.Invoke(this, new BudgetChangedArgs
            {
                NewBalance = balance,
                NewBudget = balance / 1_000_000f,
                ChangeAmount = change,
                Reason = reason,
                TeamId = teamId,
                FinancialStatus = status
            });

        public void FireQualifyingFinished(QualifyingFinishedArgs args) => OnQualifyingFinished?.Invoke();

        public void FireComponentInstalled(string compId, float gain) =>
            OnComponentInstalled?.Invoke(this, new ComponentInstalledArgs
            {
                ComponentId = compId,
                ComponentName = compId,
                PerformanceGain = gain,
                ActualPerformance = gain,
                InstallResult = "Normal"
            });

        public void FireComponentInstalled(ComponentInstalledArgs args) =>
            OnComponentInstalled?.Invoke(this, args);

        public void FireStaffChanged(string role, string name) =>
            OnStaffChanged?.Invoke(this, new StaffChangedArgs
            {
                Role = role,
                NewStaffName = name,
                StaffName = name,
                ChangeType = "Hired"
            });

        public void FireStaffChanged(StaffChangedArgs args) => OnStaffChanged?.Invoke(this, args);
        public void FireNewsGenerated(NewsGeneratedArgs args) => OnNewsGenerated?.Invoke(this, args);

        public void FireAcademyPromoted(string pilotId) => OnAcademyPilotPromoted?.Invoke(pilotId);
        public void FireRivalryChanged(string p1, string p2) => OnRivalryChanged?.Invoke(p1, p2);
        public void FireEngineChanged(string teamId, string supplierId) =>
            OnEngineSupplierChanged?.Invoke(teamId, supplierId);
        public void FireAchievementUnlocked(string achievementId) =>
            OnAchievementUnlocked?.Invoke(achievementId);

        public void FirePilotMoodChanged(PilotMoodChangedArgs args) =>
            OnPilotMoodChanged?.Invoke(this, args);
        public void FireInjuryOccurred(InjuryOccurredArgs args) =>
            OnInjuryOccurred?.Invoke(this, args);
        public void FireFIAInvestigation(FIAInvestigationArgs args) =>
            OnFIAInvestigation?.Invoke(this, args);
        public void FireRivalTransfer(RivalTransferArgs args) =>
            OnRivalTransfer?.Invoke(this, args);

        public void FireGameStateChanged(GameStateChangedArgs args) =>
            OnGameStateChanged?.Invoke(this, args);
        public void FireRandomEvent(RandomEventArgs args) =>
            OnRandomEvent?.Invoke(this, args);

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
            OnNewsGenerated = null;
            OnPilotMoodChanged = null;
            OnInjuryOccurred = null;
            OnFIAInvestigation = null;
            OnRivalTransfer = null;
            OnGameStateChanged = null;
            OnRandomEvent = null;
        }
    }
}
