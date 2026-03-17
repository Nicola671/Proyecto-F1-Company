using UnityEngine;
using F1CareerManager.Core;
using System.Collections.Generic;

namespace F1CareerManager.Testing
{
    public class AchievementSystemTester : MonoBehaviour
    {
        [Header("Manual Triggers (Right-click in inspector to run)")]
        
        [ContextMenu("Simulate First Win (Monaco)")]
        public void SimulateFirstWin()
        {
            if (EventBus.Instance == null) { Debug.LogError("EventBus instance not found!"); return; }
            if (GameManager.Instance == null) { Debug.LogError("GameManager instance not found!"); return; }

            var playerTeam = GameManager.Instance.GetPlayerTeam();
            if (playerTeam == null) { Debug.LogError("No player team found in GameManager!"); return; }

            string pilotId = (playerTeam.driverIds != null && playerTeam.driverIds.Count > 0) ? playerTeam.driverIds[0] : "pilot_01";
            
            Debug.Log($"[Test] Simulating win for pilot {pilotId} at monaco");
            
            var args = new EventBus.RaceFinishedArgs
            {
                CircuitId = "monaco",
                WinnerId = pilotId,
                FinalPositions = new List<EventBus.RacePositionInfo>
                {
                    new EventBus.RacePositionInfo { PilotId = pilotId, Position = 1, TeamId = playerTeam.id, DNF = false }
                }
            };
            
            EventBus.Instance.FireRaceFinished(args);
        }

        [ContextMenu("Simulate 1-2 Finish")]
        public void SimulateOneTwoFinish()
        {
             if (EventBus.Instance == null) return;
             var playerTeam = GameManager.Instance?.GetPlayerTeam();
             if (playerTeam == null) return;
             
             string p1 = playerTeam.driverIds.Count > 0 ? playerTeam.driverIds[0] : "p1";
             string p2 = playerTeam.driverIds.Count > 1 ? playerTeam.driverIds[1] : "p2";

             var args = new EventBus.RaceFinishedArgs
             {
                 CircuitId = "silverstone",
                 WinnerId = p1,
                 FinalPositions = new List<EventBus.RacePositionInfo>
                 {
                     new EventBus.RacePositionInfo { PilotId = p1, Position = 1, TeamId = playerTeam.id, DNF = false },
                     new EventBus.RacePositionInfo { PilotId = p2, Position = 2, TeamId = playerTeam.id, DNF = false }
                 }
             };

             EventBus.Instance.FireRaceFinished(args);
        }

        [ContextMenu("Add 1000 Legacy Points & Finish Season")]
        public void AddLegacyPoints()
        {
            if (GameManager.Instance == null || EventBus.Instance == null) return;
            
            GameManager.Instance.Legacy.totalLegacyPoints += 1000;
            Debug.Log($"[Test] Legacy Points set to {GameManager.Instance.Legacy.totalLegacyPoints}");

            var args = new EventBus.SeasonEndArgs
            {
                FinalPosition = 1,
                ObjectiveMet = true
            };
            
            EventBus.Instance.FireSeasonEnd(args);
        }

        [ContextMenu("Simulate Rich Team ($200M+)")]
        public void AddBudget()
        {
            if (EventBus.Instance == null) return;
            
            // FireBudgetChanged(long balance, long change, string reason)
            EventBus.Instance.FireBudgetChanged(205000000, 50000000, "Investor windfall");
        }

        [ContextMenu("Simulate Academy Promotion")]
        public void SimulateAcademyPromotion()
        {
            if (EventBus.Instance == null) return;
            EventBus.Instance.FireAcademyPromoted("regen_001");
        }
    }
}
