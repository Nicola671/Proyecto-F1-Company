// ============================================================
// F1 Career Manager — CalendarScreen.cs
// Vista del calendario de la temporada F1
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace F1CareerManager.UI.Screens
{
    public class CalendarScreen : MonoBehaviour
    {
        [Header("Calendar Header")]
        [SerializeField] private Text seasonText;
        [SerializeField] private Text currentWeekText;
        [SerializeField] private Text racesRemainingText;

        [Header("Race List")]
        [SerializeField] private Transform raceListContainer;
        [SerializeField] private GameObject raceEntryPrefab;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Selected Race Detail")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text raceNameText;
        [SerializeField] private Text circuitNameText;
        [SerializeField] private Text countryFlagText;
        [SerializeField] private Text lapsText;
        [SerializeField] private Text distanceText;
        [SerializeField] private Text typeText;
        [SerializeField] private Text sprintBadge;
        [SerializeField] private Text nightBadge;

        [Header("Circuit Characteristics")]
        [SerializeField] private Image overtakingBar;
        [SerializeField] private Text overtakingText;
        [SerializeField] private Image safetyCarBar;
        [SerializeField] private Text safetyCarText;
        [SerializeField] private Image rainBar;
        [SerializeField] private Text rainText;
        [SerializeField] private Text tireDegText;
        [SerializeField] private Text drsZonesText;
        [SerializeField] private Text temperatureText;
        [SerializeField] private Text altitudeText;

        [Header("Favors/Penalizes")]
        [SerializeField] private Text favorsText;
        [SerializeField] private Text penalizesText;
        [SerializeField] private Text specialNoteText;

        [Header("Race Result (if completed)")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text p1Text, p2Text, p3Text;
        [SerializeField] private Text playerResultText;

        // ──────────────────────────────────────────────────────
        [System.Serializable]
        public class CalendarRaceInfo
        {
            public int round;
            public string id, name, shortName, country, flag, city;
            public int laps;
            public float circuitLength, raceDistance;
            public bool isSprint, isNight;
            public string type;
            public int rainProb, safetyCarProb, overtaking, drsZones, altitude, temperature;
            public string tireDeg, specialNote;
            public List<string> favors, penalizes, historicWinners;
            public int legacyBonus;
            // Resultado (si ya se corrió)
            public bool completed;
            public string p1Name, p2Name, p3Name, playerPosition;
        }

        private List<CalendarRaceInfo> races = new List<CalendarRaceInfo>();
        private int currentRound = 1;

        // ══════════════════════════════════════════════════════

        public void Refresh(List<CalendarRaceInfo> raceList, int season, int week, int activeRound)
        {
            races = raceList;
            currentRound = activeRound;

            if (seasonText != null) seasonText.text = $"Temporada {season}";
            if (currentWeekText != null) currentWeekText.text = $"Semana {week}";
            int remaining = 0;
            foreach (var r in races) if (!r.completed) remaining++;
            if (racesRemainingText != null) racesRemainingText.text = $"{remaining} carreras restantes";

            PopulateRaceList();

            // Auto-scroll al GP actual
            if (activeRound > 0 && activeRound <= races.Count)
                SelectRace(races[activeRound - 1].id);
        }

        private void PopulateRaceList()
        {
            if (raceListContainer == null || raceEntryPrefab == null) return;

            foreach (Transform child in raceListContainer)
                Destroy(child.gameObject);

            foreach (var race in races)
            {
                GameObject entry = Instantiate(raceEntryPrefab, raceListContainer);

                // Round number
                Text roundText = entry.transform.Find("Round")?.GetComponent<Text>();
                if (roundText != null) roundText.text = $"R{race.round}";

                // Flag + name
                Text nameLabel = entry.transform.Find("Name")?.GetComponent<Text>();
                if (nameLabel != null) nameLabel.text = $"{race.flag} {race.shortName}";

                // Sprint badge
                Text sprintLabel = entry.transform.Find("Sprint")?.GetComponent<Text>();
                if (sprintLabel != null)
                {
                    sprintLabel.text = race.isSprint ? "SPRINT" : "";
                    sprintLabel.gameObject.SetActive(race.isSprint);
                }

                // Estado: completado, actual, futuro
                Image bg = entry.GetComponent<Image>();
                if (bg != null)
                {
                    if (race.completed)
                        bg.color = new Color(0.2f, 0.3f, 0.2f, 0.8f); // Verde oscuro
                    else if (race.round == currentRound)
                        bg.color = new Color(0.4f, 0.3f, 0.1f, 0.9f); // Dorado
                    else
                        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.7f); // Gris
                }

                // Resultado rápido si se corrió
                Text resultLabel = entry.transform.Find("Result")?.GetComponent<Text>();
                if (resultLabel != null)
                    resultLabel.text = race.completed ? $"P{race.playerPosition}" : "";

                string raceId = race.id;
                Button btn = entry.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => SelectRace(raceId));
            }
        }

        public void SelectRace(string raceId)
        {
            CalendarRaceInfo race = races.Find(r => r.id == raceId);
            if (race == null) return;

            if (raceNameText != null) raceNameText.text = race.name;
            if (circuitNameText != null) circuitNameText.text = $"{race.city}, {race.country}";
            if (countryFlagText != null) countryFlagText.text = race.flag;
            if (lapsText != null) lapsText.text = $"{race.laps} vueltas";
            if (distanceText != null) distanceText.text = $"{race.raceDistance:F1} km";
            if (typeText != null) typeText.text = FormatType(race.type);

            // Badges
            if (sprintBadge != null) { sprintBadge.text = "🏃 SPRINT"; sprintBadge.gameObject.SetActive(race.isSprint); }
            if (nightBadge != null) { nightBadge.text = "🌙 NOCTURNA"; nightBadge.gameObject.SetActive(race.isNight); }

            // Characteristics
            SetBar(overtakingBar, overtakingText, race.overtaking, "Adelantamiento");
            SetBar(safetyCarBar, safetyCarText, race.safetyCarProb, "Safety Car");
            SetBar(rainBar, rainText, race.rainProb, "Lluvia");
            if (tireDegText != null) tireDegText.text = $"Desgaste: {race.tireDeg}";
            if (drsZonesText != null) drsZonesText.text = $"DRS: {race.drsZones} zonas";
            if (temperatureText != null) temperatureText.text = $"🌡️ {race.temperature}°C";
            if (altitudeText != null) altitudeText.text = $"⛰️ {race.altitude}m";

            // Favors / Penalizes
            if (favorsText != null) favorsText.text = race.favors != null ? "✅ " + string.Join(", ", FormatSkills(race.favors)) : "";
            if (penalizesText != null) penalizesText.text = race.penalizes != null ? "❌ " + string.Join(", ", FormatSkills(race.penalizes)) : "";
            if (specialNoteText != null) specialNoteText.text = race.specialNote;

            // Resultado
            if (resultPanel != null) resultPanel.SetActive(race.completed);
            if (race.completed)
            {
                if (p1Text != null) p1Text.text = $"🥇 {race.p1Name}";
                if (p2Text != null) p2Text.text = $"🥈 {race.p2Name}";
                if (p3Text != null) p3Text.text = $"🥉 {race.p3Name}";
                if (playerResultText != null) playerResultText.text = $"Tu resultado: P{race.playerPosition}";
            }

            if (detailPanel != null) detailPanel.SetActive(true);
        }

        private void SetBar(Image bar, Text label, int value, string name)
        {
            if (bar != null) bar.fillAmount = value / 100f;
            if (label != null) label.text = $"{name}: {value}%";
        }

        private string FormatType(string type)
        {
            return type switch
            {
                "STREET" => "🏙️ Urbano",
                "SEMI_STREET" => "🏙️ Semi-Urbano",
                "HIGH_SPEED" => "🚀 Alta Velocidad",
                "TECHNICAL" => "🔧 Técnico",
                "MIXED" => "⚖️ Mixto",
                _ => type
            };
        }

        private List<string> FormatSkills(List<string> skills)
        {
            var formatted = new List<string>();
            var map = new Dictionary<string, string>
            {
                {"engine_power","Motor"}, {"tire_management","Neumáticos"},
                {"aero_downforce","Aero"}, {"chassis_aero","Chassis"},
                {"top_speed","Vel. Punta"}, {"consistency","Consistencia"},
                {"adaptability","Adaptabilidad"}, {"rain_skill","Lluvia"},
                {"starts_skill","Largadas"}, {"defense_skill","Defensa"}
            };
            foreach (var s in skills)
                formatted.Add(map.ContainsKey(s) ? map[s] : s);
            return formatted;
        }
    }
}
