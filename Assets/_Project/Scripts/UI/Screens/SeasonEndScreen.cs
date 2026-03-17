// ============================================================
// F1 Career Manager — SeasonEndScreen.cs
// Resumen completo de la temporada, premios y estadísticas final
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using F1CareerManager.Core;

namespace F1CareerManager.UI.Screens
{
    public class SeasonEndScreen : MonoBehaviour
    {
        [Header("Season Overview")]
        [SerializeField] private Text seasonTitleText;
        [SerializeField] private Text teamFinalPosText;
        [SerializeField] private Text objectiveResultText;
        [SerializeField] private Image objectiveStatusIcon;

        [Header("Standings Summary")]
        [SerializeField] private Transform driversPodiumContainer;
        [SerializeField] private Transform constructorsPodiumContainer;
        [SerializeField] private Text playerPilot1PosText;
        [SerializeField] private Text playerPilot2PosText;

        [Header("Financial Summary")]
        [SerializeField] private Text constructorPrizeText;
        [SerializeField] private Text sponsorshipBonusText;
        [SerializeField] private Text totalEarningsText;
        [SerializeField] private Text finalBudgetText;

        [Header("Stats & Highlights")]
        [SerializeField] private Text totalWinsText;
        [SerializeField] private Text totalPodiumsText;
        [SerializeField] private Text totalPolesText;
        [SerializeField] private Text totalPointsText;
        [SerializeField] private Text rivalDefeatedText;

        [Header("Legacy & Rewards")]
        [SerializeField] private Text legacyPointsGainedText;
        [SerializeField] private Transform achievementsUnlockedContainer;
        [SerializeField] private GameObject achievementMiniPrefab;

        [Header("Actions")]
        [SerializeField] private Button nextSeasonButton;
        [SerializeField] private Button shareResultsButton;

        // ── Modelos ───────────────────────────────────────────
        [System.Serializable]
        public class SeasonSummaryInfo
        {
            public int seasonNumber;
            public string teamId;
            public string teamName;
            public int constructorPos;
            public bool objectiveMet;
            public string objectiveDesc;
            
            public List<StandingsEntry> topDrivers;
            public List<StandingsEntry> topConstructors;
            public int pilot1Pos, pilot2Pos;
            public string pilot1Name, pilot2Name;

            public long constructorPrize;
            public long sponsorBonuses;
            public long totalIncome;
            public long carryOverBudget;

            public int wins, podiums, poles, points;
            public string mainRivalTeam;
            public bool defeatedRival;

            public int legacyPointsGained;
            public List<string> unlockedAchievementIds;
        }

        [System.Serializable]
        public class StandingsEntry
        {
            public string name;
            public int points;
            public string teamColor;
        }

        private void Awake()
        {
            if (nextSeasonButton != null) nextSeasonButton.onClick.AddListener(OnNextSeasonPressed);
            if (shareResultsButton != null) shareResultsButton.onClick.AddListener(OnSharePressed);
        }

        // ══════════════════════════════════════════════════════
        // ACTUALIZACIÓN
        // ══════════════════════════════════════════════════════

        public void Setup(SeasonSummaryInfo summary)
        {
            if (seasonTitleText != null) seasonTitleText.text = $"TEMPORADA {summary.seasonNumber} FINALIZADA";
            if (teamFinalPosText != null) teamFinalPosText.text = $"P{summary.constructorPos} en el Campeonato de Constructores";
            if (objectiveResultText != null) objectiveResultText.text = summary.objectiveMet ? $"Objetivo Cumplido: {summary.objectiveDesc}" : $"Objetivo Fallido: {summary.objectiveDesc}";
            
            if (objectiveStatusIcon != null)
                objectiveStatusIcon.color = summary.objectiveMet ? Color.green : Color.red;

            // Standings
            PopulatePodium(driversPodiumContainer, summary.topDrivers);
            PopulatePodium(constructorsPodiumContainer, summary.topConstructors);

            if (playerPilot1PosText != null) playerPilot1PosText.text = $"{summary.pilot1Name}: P{summary.pilot1Pos}";
            if (playerPilot2PosText != null) playerPilot2PosText.text = $"{summary.pilot2Name}: P{summary.pilot2Pos}";

            // Finance
            if (constructorPrizeText != null) constructorPrizeText.text = $"+${summary.constructorPrize / 1000000f:F1}M";
            if (sponsorshipBonusText != null) sponsorshipBonusText.text = $"+${summary.sponsorBonuses / 1000000f:F1}M";
            if (totalEarningsText != null) totalEarningsText.text = $"Total: +${summary.totalIncome / 1000000f:F1}M";
            if (finalBudgetText != null) finalBudgetText.text = $"Presupuesto p/ Sig. Año: ${summary.carryOverBudget / 1000000f:F1}M";

            // Stats
            if (totalWinsText != null) totalWinsText.text = summary.wins.ToString();
            if (totalPodiumsText != null) totalPodiumsText.text = summary.podiums.ToString();
            if (totalPolesText != null) totalPolesText.text = summary.poles.ToString();
            if (totalPointsText != null) totalPointsText.text = summary.points.ToString();
            
            if (rivalDefeatedText != null)
                rivalDefeatedText.text = summary.defeatedRival ? $"Venciste a {summary.mainRivalTeam}" : $"Perdiste contra {summary.mainRivalTeam}";

            // Legacy
            if (legacyPointsGainedText != null) legacyPointsGainedText.text = $"+{summary.legacyPointsGained} Puntos de Legado";
            
            PopulateAchievements(summary.unlockedAchievementIds);
        }

        private void PopulatePodium(Transform container, List<StandingsEntry> topEntries)
        {
            if (container == null) return;
            // Aquí iría la lógica para llenar el podio visual (1st, 2nd, 3rd)
            // Por brevedad, asumimos que se actualizan hijos prefijados
        }

        private void PopulateAchievements(List<string> ids)
        {
            if (achievementsUnlockedContainer == null || achievementMiniPrefab == null) return;

            foreach (Transform child in achievementsUnlockedContainer)
                Destroy(child.gameObject);

            if (ids == null || ids.Count == 0) return;

            foreach (var id in ids)
            {
                GameObject ach = Instantiate(achievementMiniPrefab, achievementsUnlockedContainer);
                // Configurar icono/título del logro
            }
        }

        // ══════════════════════════════════════════════════════
        // ACCIONES
        // ══════════════════════════════════════════════════════

        private void OnNextSeasonPressed()
        {
            Debug.Log("[SeasonEnd] Preparando siguiente temporada...");
            // SeasonManager.Instance.PrepareNextSeason();
        }

        private void OnSharePressed()
        {
            Debug.Log("[SeasonEnd] Compartiendo resultados...");
            // Captura de pantalla y compartir (native share)
        }
    }
}
