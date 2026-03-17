// ============================================================
// F1 Career Manager — TeamSelectionScreen.cs
// Pantalla de selección de equipo al iniciar nueva partida
// Muestra los 10 equipos con stats, presupuesto y dificultad
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using F1CareerManager.Core;

namespace F1CareerManager.UI.Screens
{
    public class TeamSelectionScreen : MonoBehaviour
    {
        [Header("Team List")]
        [SerializeField] private Transform teamListContainer;
        [SerializeField] private GameObject teamCardPrefab;

        [Header("Selected Team Detail")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text teamNameText;
        [SerializeField] private Text teamFullNameText;
        [SerializeField] private Image teamColorBand;
        [SerializeField] private Text budgetText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text pilotsText;
        [SerializeField] private Text fanbaseText;
        [SerializeField] private Text prestigeText;

        [Header("Car Stats Bars")]
        [SerializeField] private Image aeroBar;
        [SerializeField] private Image engineBar;
        [SerializeField] private Image chassisBar;
        [SerializeField] private Image reliabilityBar;
        [SerializeField] private Text aeroValueText;
        [SerializeField] private Text engineValueText;
        [SerializeField] private Text chassisValueText;
        [SerializeField] private Text reliabilityValueText;

        [Header("Staff Stars")]
        [SerializeField] private Text techDirectorStars;
        [SerializeField] private Text aeroChiefStars;
        [SerializeField] private Text engineChiefStars;

        [Header("Difficulty Selection")]
        [SerializeField] private Transform difficultyContainer;
        [SerializeField] private GameObject difficultyCardPrefab;
        [SerializeField] private Text difficultyNameText;
        [SerializeField] private Text difficultyDescText;

        [Header("Challenge Rating")]
        [SerializeField] private Text challengeRatingText;
        [SerializeField] private Image challengeBar;

        [Header("Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button backButton;

        // ── Estado ───────────────────────────────────────────
        private string selectedTeamId = "";
        private string selectedDifficultyId = "STANDARD";
        private List<TeamInfo> teamList = new List<TeamInfo>();

        // Modelo simplificado para la UI
        [Serializable]
        public class TeamInfo
        {
            public string id, name, shortName, color;
            public int stars, aero, engine, chassis, reliability, overall;
            public long budget;
            public int fanbase, prestige;
            public int tdLevel, aeroLevel, engineLevel;
            public string objectiveStandard;
            public string pilot1Name, pilot2Name;
        }

        [Serializable]
        public class DifficultyInfo
        {
            public string id, name, description;
            public float challengeMultiplier;
        }

        private readonly DifficultyInfo[] difficulties = new DifficultyInfo[]
        {
            new DifficultyInfo { id = "NARRATIVE", name = "Narrativo", description = "Disfrutá la historia sin presión. Más dinero, menos desastres.", challengeMultiplier = 0.5f },
            new DifficultyInfo { id = "STANDARD", name = "Estándar", description = "La experiencia F1 balanceada. Desafíos justos, recompensas proporcionales.", challengeMultiplier = 1.0f },
            new DifficultyInfo { id = "DEMANDING", name = "Exigente", description = "Para los que buscan un desafío real. Rivales agresivos, FIA estricta.", challengeMultiplier = 1.5f },
            new DifficultyInfo { id = "LEGEND", name = "Leyenda", description = "Solo para los mejores. Todo en tu contra. Un error y perdés la temporada.", challengeMultiplier = 2.0f }
        };

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ══════════════════════════════════════════════════════

        public void Initialize(List<TeamInfo> teams)
        {
            teamList = teams;
            PopulateTeamCards();
            PopulateDifficultyCards();

            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmPressed);
            if (backButton != null) backButton.onClick.AddListener(OnBackPressed);

            // Seleccionar el primer equipo por defecto
            if (teamList.Count > 0)
                SelectTeam(teamList[0].id);
        }

        private void PopulateTeamCards()
        {
            if (teamListContainer == null || teamCardPrefab == null) return;

            foreach (Transform child in teamListContainer)
                Destroy(child.gameObject);

            foreach (var team in teamList)
            {
                GameObject card = Instantiate(teamCardPrefab, teamListContainer);
                // Configurar card visual
                Text nameText = card.GetComponentInChildren<Text>();
                if (nameText != null) nameText.text = team.shortName;

                // Color band
                Image colorImg = card.transform.Find("ColorBand")?.GetComponent<Image>();
                if (colorImg != null && ColorUtility.TryParseHtmlString(team.color, out Color c))
                    colorImg.color = c;

                // Stars
                Text starsText = card.transform.Find("Stars")?.GetComponent<Text>();
                if (starsText != null) starsText.text = new string('★', team.stars) + new string('☆', 5 - team.stars);

                // Overall
                Text overallText = card.transform.Find("Overall")?.GetComponent<Text>();
                if (overallText != null) overallText.text = $"OVR {team.overall}";

                // Click handler
                string teamId = team.id;
                Button btn = card.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => SelectTeam(teamId));
            }
        }

        private void PopulateDifficultyCards()
        {
            if (difficultyContainer == null) return;

            foreach (Transform child in difficultyContainer)
                Destroy(child.gameObject);

            foreach (var diff in difficulties)
            {
                if (difficultyCardPrefab != null)
                {
                    GameObject card = Instantiate(difficultyCardPrefab, difficultyContainer);
                    Text label = card.GetComponentInChildren<Text>();
                    if (label != null) label.text = diff.name;

                    string diffId = diff.id;
                    Button btn = card.GetComponent<Button>();
                    if (btn != null) btn.onClick.AddListener(() => SelectDifficulty(diffId));
                }
            }
        }

        // ══════════════════════════════════════════════════════
        // SELECCIÓN
        // ══════════════════════════════════════════════════════

        public void SelectTeam(string teamId)
        {
            selectedTeamId = teamId;
            TeamInfo team = teamList.Find(t => t.id == teamId);
            if (team == null) return;

            // Actualizar panel de detalles
            if (teamNameText != null) teamNameText.text = team.shortName;
            if (teamFullNameText != null) teamFullNameText.text = team.name;
            if (budgetText != null) budgetText.text = $"${team.budget / 1000000}M";
            if (objectiveText != null) objectiveText.text = FormatObjective(team.objectiveStandard);
            if (pilotsText != null) pilotsText.text = $"{team.pilot1Name}\n{team.pilot2Name}";
            if (fanbaseText != null) fanbaseText.text = $"{team.fanbase}/100";
            if (prestigeText != null) prestigeText.text = $"{team.prestige}/100";

            // Color
            if (teamColorBand != null && ColorUtility.TryParseHtmlString(team.color, out Color c))
                teamColorBand.color = c;

            // Stats bars (normalizar a 0-1 para fillAmount)
            SetStatBar(aeroBar, aeroValueText, team.aero);
            SetStatBar(engineBar, engineValueText, team.engine);
            SetStatBar(chassisBar, chassisValueText, team.chassis);
            SetStatBar(reliabilityBar, reliabilityValueText, team.reliability);

            // Staff stars
            if (techDirectorStars != null) techDirectorStars.text = new string('★', team.tdLevel);
            if (aeroChiefStars != null) aeroChiefStars.text = new string('★', team.aeroLevel);
            if (engineChiefStars != null) engineChiefStars.text = new string('★', team.engineLevel);

            // Calcular desafío combinado (equipo peor = más desafío)
            UpdateChallengeRating(team);

            if (detailPanel != null) detailPanel.SetActive(true);
        }

        public void SelectDifficulty(string diffId)
        {
            selectedDifficultyId = diffId;
            DifficultyInfo diff = Array.Find(difficulties, d => d.id == diffId);
            if (diff == null) return;

            if (difficultyNameText != null) difficultyNameText.text = diff.name;
            if (difficultyDescText != null) difficultyDescText.text = diff.description;

            // Recalcular desafío con nueva dificultad
            TeamInfo team = teamList.Find(t => t.id == selectedTeamId);
            if (team != null) UpdateChallengeRating(team);
        }

        private void UpdateChallengeRating(TeamInfo team)
        {
            // Rating de desafío: equipo peor + dificultad mayor = más estrellas
            float teamFactor = 1f - (team.overall / 100f); // 0 = top, 1 = bottom
            DifficultyInfo diff = Array.Find(difficulties, d => d.id == selectedDifficultyId);
            float diffFactor = diff != null ? diff.challengeMultiplier : 1f;

            float challenge = Mathf.Clamp01(teamFactor * 0.6f + (diffFactor - 0.5f) * 0.4f);
            int stars = Mathf.Clamp(Mathf.RoundToInt(challenge * 5f), 1, 5);

            string[] labels = { "", "Relajado", "Accesible", "Desafiante", "Brutal", "Pesadilla" };

            if (challengeRatingText != null) challengeRatingText.text = $"{new string('★', stars)} {labels[stars]}";
            if (challengeBar != null) challengeBar.fillAmount = challenge;
        }

        private void SetStatBar(Image bar, Text valueText, int value)
        {
            if (bar != null) bar.fillAmount = value / 100f;
            if (valueText != null) valueText.text = value.ToString();
        }

        private string FormatObjective(string obj)
        {
            if (string.IsNullOrEmpty(obj)) return "Sin objetivo definido";
            return obj switch
            {
                "win_both_championships" => "🏆 Ganar ambos campeonatos",
                "win_constructors_championship" => "🏆 Ganar constructores",
                "top2_constructors" => "🥈 Top 2 constructores",
                "top3_constructors" => "🥉 Top 3 constructores",
                "top4_constructors" => "Top 4 constructores",
                "top5_constructors" => "Top 5 constructores",
                "top6_constructors" => "Top 6 constructores",
                "top7_constructors" => "Top 7 constructores",
                "top8_constructors" => "Top 8 constructores",
                "score_points" => "Sumar puntos regularmente",
                "race_win" => "Ganar una carrera",
                "podium_finish" => "Subir al podio",
                _ => obj
            };
        }

        // ══════════════════════════════════════════════════════
        // ACCIONES
        // ══════════════════════════════════════════════════════

        private void OnConfirmPressed()
        {
            if (string.IsNullOrEmpty(selectedTeamId))
            {
                Debug.LogWarning("[TeamSelection] No se seleccionó ningún equipo");
                return;
            }

            Debug.Log($"[TeamSelection] Equipo: {selectedTeamId}, Dificultad: {selectedDifficultyId}");

            // Iniciar nueva partida con la selección
            // GameManager.Instance.StartNewGame(selectedTeamId, selectedDifficultyId);
        }

        private void OnBackPressed()
        {
            // UIManager.Instance.NavigateTo("MainMenu");
        }

        // ═══ API Pública ═════════════════════════════════════
        public string SelectedTeamId => selectedTeamId;
        public string SelectedDifficultyId => selectedDifficultyId;
    }
}
