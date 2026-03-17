// ============================================================
// F1 Career Manager — LegacyScreen.cs
// Pantalla de legado, Hall of Fame y logros desbloqueados
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using F1CareerManager.Core;

namespace F1CareerManager.UI.Screens
{
    public class LegacyScreen : MonoBehaviour
    {
        [Header("Legacy Overview")]
        [SerializeField] private Text rankTitleText;
        [SerializeField] private Text rankDescriptionText;
        [SerializeField] private Text totalPointsText;
        [SerializeField] private Image rankProgressBar;
        [SerializeField] private Text nextRankText;
        [SerializeField] private Text pointsToNextText;
        [SerializeField] private Image rankIcon;

        [Header("Stats Summary")]
        [SerializeField] private Text totalWinsText;
        [SerializeField] private Text totalPodiumsText;
        [SerializeField] private Text totalPolesText;
        [SerializeField] private Text constructorTitlesText;
        [SerializeField] private Text driverTitlesText;
        [SerializeField] private Text seasonsPlayedText;
        [SerializeField] private Text bestFinishText;

        [Header("Achievements Section")]
        [SerializeField] private Text achievementProgressText;
        [SerializeField] private Image achievementProgressBar;
        [SerializeField] private Transform achievementListContainer;
        [SerializeField] private GameObject achievementCardPrefab;

        [Header("Achievement Filters")]
        [SerializeField] private Button filterAllButton;
        [SerializeField] private Button filterRacingButton;
        [SerializeField] private Button filterManagementButton;
        [SerializeField] private Button filterFinancialButton;
        [SerializeField] private Button filterLegacyButton;
        [SerializeField] private Button filterSecretButton;

        [Header("Hall of Fame")]
        [SerializeField] private Transform hallOfFameContainer;
        [SerializeField] private GameObject hallOfFameEntryPrefab;

        [Header("Tabs")]
        [SerializeField] private Button tabOverview;
        [SerializeField] private Button tabAchievements;
        [SerializeField] private Button tabHallOfFame;
        [SerializeField] private GameObject panelOverview;
        [SerializeField] private GameObject panelAchievements;
        [SerializeField] private GameObject panelHallOfFame;

        // ── Estado ───────────────────────────────────────────
        private string currentFilter = "ALL";
        private string currentTab = "OVERVIEW";

        // Modelos para la UI
        [System.Serializable]
        public class LegacyRankInfo
        {
            public string rankName;
            public int currentPoints, nextRankPoints;
            public float progress;
        }

        [System.Serializable]
        public class AchievementDisplayInfo
        {
            public string id, title, description, icon, category, rarity, flavorText;
            public bool unlocked, secret;
            public int currentProgress, targetProgress;
            public int legacyPoints;
        }

        [System.Serializable]
        public class HallOfFameEntry
        {
            public int season;
            public string teamName;
            public string achievement; // "Constructor Champion", "Driver Champion", etc.
            public int points;
        }

        // ── Rank definitions ──────────────────────────────────
        private readonly (string name, int points, string desc)[] ranks = new[]
        {
            ("Novato", 0, "Recién llegás al paddock"),
            ("Contendiente", 500, "El paddock empieza a respetarte"),
            ("Fuerza Establecida", 1000, "Ya no sos un recién llegado"),
            ("Gran Equipo", 2000, "Tu nombre suena con los grandes"),
            ("Leyenda", 3000, "Pocos han llegado tan lejos"),
            ("Dinastía", 5000, "Tu legado vivirá para siempre")
        };

        private void Awake()
        {
            SetupTabButtons();
            SetupFilterButtons();
        }

        private void SetupTabButtons()
        {
            if (tabOverview != null) tabOverview.onClick.AddListener(() => SwitchTab("OVERVIEW"));
            if (tabAchievements != null) tabAchievements.onClick.AddListener(() => SwitchTab("ACHIEVEMENTS"));
            if (tabHallOfFame != null) tabHallOfFame.onClick.AddListener(() => SwitchTab("HALL_OF_FAME"));
        }

        private void SetupFilterButtons()
        {
            if (filterAllButton != null) filterAllButton.onClick.AddListener(() => FilterAchievements("ALL"));
            if (filterRacingButton != null) filterRacingButton.onClick.AddListener(() => FilterAchievements("RACING"));
            if (filterManagementButton != null) filterManagementButton.onClick.AddListener(() => FilterAchievements("MANAGEMENT"));
            if (filterFinancialButton != null) filterFinancialButton.onClick.AddListener(() => FilterAchievements("FINANCIAL"));
            if (filterLegacyButton != null) filterLegacyButton.onClick.AddListener(() => FilterAchievements("LEGACY"));
            if (filterSecretButton != null) filterSecretButton.onClick.AddListener(() => FilterAchievements("SECRET"));
        }

        // ══════════════════════════════════════════════════════
        // TABS
        // ══════════════════════════════════════════════════════

        public void SwitchTab(string tab)
        {
            currentTab = tab;
            if (panelOverview != null) panelOverview.SetActive(tab == "OVERVIEW");
            if (panelAchievements != null) panelAchievements.SetActive(tab == "ACHIEVEMENTS");
            if (panelHallOfFame != null) panelHallOfFame.SetActive(tab == "HALL_OF_FAME");
        }

        // ══════════════════════════════════════════════════════
        // OVERVIEW TAB
        // ══════════════════════════════════════════════════════

        public void UpdateOverview(LegacyRankInfo rankInfo, int wins, int podiums,
            int poles, int constructorChamps, int driverChamps, int seasons, string bestFinish)
        {
            // Rank
            if (rankTitleText != null) rankTitleText.text = rankInfo.rankName;
            if (totalPointsText != null) totalPointsText.text = $"{rankInfo.currentPoints} pts";
            if (rankProgressBar != null) rankProgressBar.fillAmount = rankInfo.progress;

            // Calcular siguiente rango
            string nextRank = "Máximo alcanzado";
            int pointsNeeded = 0;
            for (int i = 0; i < ranks.Length; i++)
            {
                if (rankInfo.currentPoints < ranks[i].points)
                {
                    nextRank = ranks[i].name;
                    pointsNeeded = ranks[i].points - rankInfo.currentPoints;
                    break;
                }
            }
            if (nextRankText != null) nextRankText.text = nextRank;
            if (pointsToNextText != null) pointsToNextText.text = pointsNeeded > 0 ? $"Faltan {pointsNeeded} pts" : "🏆 Rango máximo";

            // Find rank description
            string rankDesc = "";
            for (int i = ranks.Length - 1; i >= 0; i--)
            {
                if (rankInfo.currentPoints >= ranks[i].points)
                {
                    rankDesc = ranks[i].desc;
                    break;
                }
            }
            if (rankDescriptionText != null) rankDescriptionText.text = rankDesc;

            // Stats
            if (totalWinsText != null) totalWinsText.text = wins.ToString();
            if (totalPodiumsText != null) totalPodiumsText.text = podiums.ToString();
            if (totalPolesText != null) totalPolesText.text = poles.ToString();
            if (constructorTitlesText != null) constructorTitlesText.text = constructorChamps.ToString();
            if (driverTitlesText != null) driverTitlesText.text = driverChamps.ToString();
            if (seasonsPlayedText != null) seasonsPlayedText.text = seasons.ToString();
            if (bestFinishText != null) bestFinishText.text = bestFinish;
        }

        // ══════════════════════════════════════════════════════
        // ACHIEVEMENTS TAB
        // ══════════════════════════════════════════════════════

        public void UpdateAchievements(List<AchievementDisplayInfo> achievements, int unlockedCount, int totalCount)
        {
            // Progress header
            if (achievementProgressText != null)
                achievementProgressText.text = $"{unlockedCount}/{totalCount} desbloqueados ({Mathf.RoundToInt((float)unlockedCount / totalCount * 100)}%)";
            if (achievementProgressBar != null)
                achievementProgressBar.fillAmount = (float)unlockedCount / totalCount;

            // Populate list
            PopulateAchievementList(achievements);
        }

        public void FilterAchievements(string category)
        {
            currentFilter = category;
            // Repopulate with filter — caller should call UpdateAchievements again
            Debug.Log($"[LegacyScreen] Filtro: {category}");
        }

        private void PopulateAchievementList(List<AchievementDisplayInfo> achievements)
        {
            if (achievementListContainer == null) return;

            foreach (Transform child in achievementListContainer)
                Destroy(child.gameObject);

            foreach (var ach in achievements)
            {
                // Filtrar por categoría
                if (currentFilter != "ALL" && ach.category != currentFilter) continue;
                // Logros secretos bloqueados se muestran como "???"
                if (ach.secret && !ach.unlocked) continue; // O mostrar como misterio

                if (achievementCardPrefab == null) continue;
                GameObject card = Instantiate(achievementCardPrefab, achievementListContainer);

                // Título
                Text titleText = card.transform.Find("Title")?.GetComponent<Text>();
                if (titleText != null) titleText.text = ach.unlocked ? ach.title : (ach.secret ? "???" : ach.title);

                // Descripción
                Text descText = card.transform.Find("Description")?.GetComponent<Text>();
                if (descText != null) descText.text = ach.unlocked ? ach.description : (ach.secret ? "???" : ach.description);

                // Rareza con color
                Text rarityText = card.transform.Find("Rarity")?.GetComponent<Text>();
                if (rarityText != null)
                {
                    rarityText.text = ach.rarity;
                    rarityText.color = GetRarityColor(ach.rarity);
                }

                // Progreso
                Image progressBar = card.transform.Find("ProgressBar")?.GetComponent<Image>();
                if (progressBar != null)
                    progressBar.fillAmount = ach.targetProgress > 0 ? (float)ach.currentProgress / ach.targetProgress : (ach.unlocked ? 1f : 0f);

                // Puntos
                Text pointsText = card.transform.Find("Points")?.GetComponent<Text>();
                if (pointsText != null) pointsText.text = $"+{ach.legacyPoints} pts";

                // Flavor text (solo si desbloqueado)
                Text flavorTextComp = card.transform.Find("FlavorText")?.GetComponent<Text>();
                if (flavorTextComp != null) flavorTextComp.text = ach.unlocked ? $"\"{ach.flavorText}\"" : "";

                // Opacidad si bloqueado
                CanvasGroup cg = card.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = ach.unlocked ? 1f : 0.5f;
            }
        }

        private Color GetRarityColor(string rarity)
        {
            return rarity switch
            {
                "COMMON" => new Color(0.7f, 0.7f, 0.7f),
                "UNCOMMON" => new Color(0.2f, 0.8f, 0.2f),
                "RARE" => new Color(0.2f, 0.4f, 1f),
                "EPIC" => new Color(0.6f, 0.2f, 0.8f),
                "LEGENDARY" => new Color(1f, 0.84f, 0f),
                _ => Color.white
            };
        }

        // ══════════════════════════════════════════════════════
        // HALL OF FAME TAB
        // ══════════════════════════════════════════════════════

        public void UpdateHallOfFame(List<HallOfFameEntry> entries)
        {
            if (hallOfFameContainer == null) return;

            foreach (Transform child in hallOfFameContainer)
                Destroy(child.gameObject);

            foreach (var entry in entries)
            {
                if (hallOfFameEntryPrefab == null) continue;
                GameObject row = Instantiate(hallOfFameEntryPrefab, hallOfFameContainer);

                Text seasonText = row.transform.Find("Season")?.GetComponent<Text>();
                if (seasonText != null) seasonText.text = $"T{entry.season}";

                Text teamText = row.transform.Find("Team")?.GetComponent<Text>();
                if (teamText != null) teamText.text = entry.teamName;

                Text achText = row.transform.Find("Achievement")?.GetComponent<Text>();
                if (achText != null) achText.text = entry.achievement;

                Text ptsText = row.transform.Find("Points")?.GetComponent<Text>();
                if (ptsText != null) ptsText.text = $"+{entry.points}";
            }
        }

        // ═══ API ═════════════════════════════════════════════
        public string CurrentFilter => currentFilter;
        public string CurrentTab => currentTab;
    }
}
