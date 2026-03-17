using UnityEngine;
using F1CareerManager.Core;
using System.Collections.Generic;

namespace F1CareerManager.Testing
{
    public class AchievementSystemTester : MonoBehaviour
    {
        [ContextMenu("Simulate First Win (Monaco)")]
        public void SimulateFirstWin()
        {
            if (EventBus.Instance == null) { Debug.LogError("EventBus no encontrado!"); return; }
            if (GameManager.Instance == null) { Debug.LogError("GameManager no encontrado!"); return; }

            var playerTeam = GameManager.Instance.GetPlayerTeam();
            if (playerTeam == null) { Debug.LogError("No hay equipo del jugador!"); return; }

            string pilotId = (playerTeam.driverIds != null && playerTeam.driverIds.Count > 0)
                ? playerTeam.driverIds[0] : "pilot_01";

            Debug.Log($"[Test] Simulando victoria en Mónaco para piloto {pilotId}");

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
            if (EventBus.Instance == null || GameManager.Instance == null) return;
            var playerTeam = GameManager.Instance.GetPlayerTeam();
            if (playerTeam == null) return;

            string p1 = playerTeam.driverIds.Count > 0 ? playerTeam.driverIds[0] : "p1";
            string p2 = playerTeam.driverIds.Count > 1 ? playerTeam.driverIds[1] : "p2";

            EventBus.Instance.FireRaceFinished(new EventBus.RaceFinishedArgs
            {
                CircuitId = "silverstone",
                WinnerId = p1,
                FinalPositions = new List<EventBus.RacePositionInfo>
                {
                    new EventBus.RacePositionInfo { PilotId = p1, Position = 1, TeamId = playerTeam.id, DNF = false },
                    new EventBus.RacePositionInfo { PilotId = p2, Position = 2, TeamId = playerTeam.id, DNF = false }
                }
            });
        }

        [ContextMenu("Add 1000 Legacy Points & Finish Season")]
        public void AddLegacyPoints()
        {
            if (GameManager.Instance == null || EventBus.Instance == null) return;
            GameManager.Instance.Legacy.totalLegacyPoints += 1000;
            Debug.Log($"[Test] Legacy Points: {GameManager.Instance.Legacy.totalLegacyPoints}");
            EventBus.Instance.FireSeasonEnd(new EventBus.SeasonEndArgs { FinalPosition = 1, ObjectiveMet = true });
        }

        [ContextMenu("Simulate Rich Team ($200M+)")]
        public void SimulateRichTeam()
        {
            if (EventBus.Instance == null) return;
            EventBus.Instance.FireBudgetChanged(205_000_000, 50_000_000, "Inversor nuevo", "", "OK");
        }

        [ContextMenu("Simulate Academy Promotion")]
        public void SimulateAcademyPromotion()
        {
            if (EventBus.Instance == null) return;
            EventBus.Instance.FireAcademyPromoted("regen_001");
        }
    }
}
