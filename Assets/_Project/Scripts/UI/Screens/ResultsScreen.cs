// ============================================================
// F1 Career Manager — ResultsScreen.cs
// Pantalla de resultados post-carrera — podio animado
// ============================================================
// PREFAB: ResultsScreen_Prefab
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.UI.Screens
{
    /// <summary>
    /// Pantalla de resultados:
    /// - Podio animado top 3 con sprites pixel art
    /// - Tabla completa 20 posiciones
    /// - Tabs: Carrera / Campeonato Pilotos / Constructores
    /// - Stats de carrera
    /// - Sección "Tu carrera"
    /// - Reacciones de pilotos
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        // ── Referencias: Podio ───────────────────────────────
        [Header("Podio")]
        [SerializeField] private Image _p1Sprite;
        [SerializeField] private Image _p2Sprite;
        [SerializeField] private Image _p3Sprite;
        [SerializeField] private Text _p1Name;
        [SerializeField] private Text _p2Name;
        [SerializeField] private Text _p3Name;
        [SerializeField] private Text _p1Time;
        [SerializeField] private Text _p2Gap;
        [SerializeField] private Text _p3Gap;
        [SerializeField] private Image _p1TeamColor;
        [SerializeField] private Image _p2TeamColor;
        [SerializeField] private Image _p3TeamColor;
        [SerializeField] private RectTransform _p1Platform;
        [SerializeField] private RectTransform _p2Platform;
        [SerializeField] private RectTransform _p3Platform;

        // ── Referencias: Tabla ───────────────────────────────
        [Header("Tabla de resultados")]
        [SerializeField] private ScrollRect _resultsScroll;
        [SerializeField] private Transform _resultsContainer;

        // ── Referencias: Tabs ────────────────────────────────
        [Header("Tabs")]
        [SerializeField] private Button _tabRace;
        [SerializeField] private Button _tabDrivers;
        [SerializeField] private Button _tabConstructors;
        [SerializeField] private Image _tabRaceIndicator;
        [SerializeField] private Image _tabDriversIndicator;
        [SerializeField] private Image _tabConstructorsIndicator;

        // ── Referencias: Stats de carrera ────────────────────
        [Header("Stats carrera")]
        [SerializeField] private Text _fastestLapText;
        [SerializeField] private Text _totalOvertakesText;
        [SerializeField] private Text _safetyCarsText;
        [SerializeField] private Text _dnfCountText;

        // ── Referencias: Tu carrera ──────────────────────────
        [Header("Tu carrera")]
        [SerializeField] private Text _yourPilot1Result;
        [SerializeField] private Text _yourPilot2Result;
        [SerializeField] private Text _pointsEarned;
        [SerializeField] private Text _pilotReactions;

        // ── Continuar ────────────────────────────────────────
        [Header("Navegación")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private Text _continueText;

        // ── Estado ───────────────────────────────────────────
        private string _activeTab = "race";
        private System.Action _onContinue;

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ══════════════════════════════════════════════════════

        public void Initialize(EventBus.RaceFinishedArgs raceArgs,
            List<PilotData> allPilots, List<TeamData> allTeams,
            string playerTeamId, System.Action onContinue)
        {
            _onContinue = onContinue;

            SetupPodium(raceArgs, allPilots);
            SetupResultsTable(raceArgs, allPilots);
            SetupRaceStats(raceArgs);
            SetupYourRace(raceArgs, allPilots, playerTeamId);
            SetupTabs();
            SetupContinueButton();

            // Animación del podio
            StartCoroutine(PodiumEntryAnimation());
        }

        // ══════════════════════════════════════════════════════
        // PODIO
        // ══════════════════════════════════════════════════════

        private void SetupPodium(EventBus.RaceFinishedArgs args,
            List<PilotData> pilots)
        {
            if (args.FinalPositions == null || args.FinalPositions.Count < 3)
                return;

            // P1
            var p1 = args.FinalPositions[0];
            var p1Data = pilots.Find(p => p.id == p1.PilotId);
            SetPodiumEntry(_p1Name, _p1Time, _p1TeamColor, _p1Sprite,
                p1Data, "GANADOR", p1.TeamId);

            // P2
            var p2 = args.FinalPositions[1];
            var p2Data = pilots.Find(p => p.id == p2.PilotId);
            SetPodiumEntry(_p2Name, _p2Gap, _p2TeamColor, _p2Sprite,
                p2Data, "+2.3s", p2.TeamId);

            // P3
            var p3 = args.FinalPositions[2];
            var p3Data = pilots.Find(p => p.id == p3.PilotId);
            SetPodiumEntry(_p3Name, _p3Gap, _p3TeamColor, _p3Sprite,
                p3Data, "+5.1s", p3.TeamId);
        }

        private void SetPodiumEntry(Text nameText, Text gapText,
            Image teamColor, Image sprite, PilotData pilot,
            string gap, string teamId)
        {
            if (nameText != null && pilot != null)
            {
                nameText.text = $"{pilot.firstName[0]}. {pilot.lastName}";
                nameText.color = UITheme.TextPrimary;
                nameText.fontSize = UITheme.FONT_SIZE_MD;
            }

            if (gapText != null)
            {
                gapText.text = gap;
                gapText.color = UITheme.TextSecondary;
                gapText.fontSize = UITheme.FONT_SIZE_SM;
            }

            if (teamColor != null)
                teamColor.color = UITheme.GetTeamColor(teamId);
        }

        // ══════════════════════════════════════════════════════
        // TABLA DE RESULTADOS
        // ══════════════════════════════════════════════════════

        private void SetupResultsTable(EventBus.RaceFinishedArgs args,
            List<PilotData> pilots)
        {
            if (_resultsContainer == null || args.FinalPositions == null) return;

            foreach (Transform child in _resultsContainer)
                Destroy(child.gameObject);

            foreach (var pos in args.FinalPositions)
            {
                var pilot = pilots.Find(p => p.id == pos.PilotId);
                if (pilot == null) continue;

                CreateResultRow(pos, pilot);
            }
        }

        private void CreateResultRow(EventBus.RacePositionInfo pos,
            PilotData pilot)
        {
            GameObject row = new GameObject($"Result_P{pos.Position}",
                typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_resultsContainer, false);

            Image bg = row.GetComponent<Image>();
            bool isTop3 = pos.Position <= 3;
            bg.color = isTop3
                ? UITheme.WithAlpha(UITheme.AccentGold, 0.1f)
                : UITheme.BackgroundCard;

            RectTransform rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 32f);

            // Auxiliar para crear textos inline
            System.Action<string, Color, float, float, TextAnchor> addText =
                (text, color, minX, maxX, align) =>
                {
                    GameObject obj = new GameObject("Col", typeof(Text));
                    obj.transform.SetParent(row.transform, false);
                    Text t = obj.GetComponent<Text>();
                    t.text = text;
                    t.color = color;
                    t.fontSize = UITheme.FONT_SIZE_SM;
                    t.alignment = align;
                    RectTransform tRt = obj.GetComponent<RectTransform>();
                    tRt.anchorMin = new Vector2(minX, 0);
                    tRt.anchorMax = new Vector2(maxX, 1);
                    tRt.offsetMin = new Vector2(4, 0);
                    tRt.offsetMax = new Vector2(-4, 0);
                };

            // Posición
            Color posColor = pos.Position == 1 ? UITheme.AccentGold :
                pos.Position == 2 ? UITheme.AccentSilver :
                pos.Position == 3 ? UITheme.AccentBronze :
                UITheme.TextPrimary;
            addText($"P{pos.Position}", posColor, 0f, 0.1f,
                TextAnchor.MiddleCenter);

            // Nombre
            addText($"{pilot.firstName[0]}. {pilot.lastName}",
                pos.DNF ? UITheme.TextMuted : UITheme.TextPrimary,
                0.12f, 0.55f, TextAnchor.MiddleLeft);

            // Equipo
            addText(pos.TeamId ?? "",
                UITheme.GetTeamColor(pos.TeamId ?? ""),
                0.55f, 0.75f, TextAnchor.MiddleLeft);

            // Puntos
            int points = pos.PointsEarned;
            addText(points > 0 ? $"+{points}pts" : (pos.DNF ? "DNF" : ""),
                points > 0 ? UITheme.TextPositive :
                    (pos.DNF ? UITheme.TextNegative : UITheme.TextMuted),
                0.75f, 1f, TextAnchor.MiddleRight);
        }

        // ══════════════════════════════════════════════════════
        // STATS DE CARRERA
        // ══════════════════════════════════════════════════════

        private void SetupRaceStats(EventBus.RaceFinishedArgs args)
        {
            if (_fastestLapText != null)
            {
                _fastestLapText.text = $"⏱ Vuelta rápida: {args.FastestLapId ?? "N/A"}";
                _fastestLapText.color = UITheme.AccentPrimary;
                _fastestLapText.fontSize = UITheme.FONT_SIZE_SM;
            }

            int dnfs = args.FinalPositions?.FindAll(p => p.DNF).Count ?? 0;
            if (_dnfCountText != null)
            {
                _dnfCountText.text = $"💥 Abandonos: {dnfs}";
                _dnfCountText.color = UITheme.TextSecondary;
            }

            if (_safetyCarsText != null)
            {
                _safetyCarsText.text = $"🚗 Safety Cars: {args.SafetyCars}";
                _safetyCarsText.color = UITheme.TextSecondary;
            }
        }

        // ══════════════════════════════════════════════════════
        // TU CARRERA
        // ══════════════════════════════════════════════════════

        private void SetupYourRace(EventBus.RaceFinishedArgs args,
            List<PilotData> pilots, string playerTeamId)
        {
            var yourPilots = args.FinalPositions?.FindAll(
                p => p.TeamId == playerTeamId) ?? new List<EventBus.RacePositionInfo>();

            if (_yourPilot1Result != null && yourPilots.Count > 0)
            {
                var p = yourPilots[0];
                var data = pilots.Find(d => d.id == p.PilotId);
                _yourPilot1Result.text = data != null
                    ? $"{data.lastName}: P{p.Position} ({(p.DNF ? "DNF" : $"+{p.PointsEarned}pts")})"
                    : "N/A";
                _yourPilot1Result.color = p.Position <= 10
                    ? UITheme.TextPositive : UITheme.TextPrimary;
            }

            if (_yourPilot2Result != null && yourPilots.Count > 1)
            {
                var p = yourPilots[1];
                var data = pilots.Find(d => d.id == p.PilotId);
                _yourPilot2Result.text = data != null
                    ? $"{data.lastName}: P{p.Position} ({(p.DNF ? "DNF" : $"+{p.PointsEarned}pts")})"
                    : "N/A";
                _yourPilot2Result.color = p.Position <= 10
                    ? UITheme.TextPositive : UITheme.TextPrimary;
            }

            int totalPts = 0;
            foreach (var p in yourPilots) totalPts += p.PointsEarned;

            if (_pointsEarned != null)
            {
                _pointsEarned.text = $"🏆 Puntos ganados: {totalPts}";
                _pointsEarned.color = totalPts > 0
                    ? UITheme.TextPositive : UITheme.TextSecondary;
            }
        }

        // ══════════════════════════════════════════════════════
        // TABS
        // ══════════════════════════════════════════════════════

        private void SetupTabs()
        {
            if (_tabRace != null)
            {
                _tabRace.onClick.RemoveAllListeners();
                _tabRace.onClick.AddListener(() => SetActiveTab("race"));
            }

            if (_tabDrivers != null)
            {
                _tabDrivers.onClick.RemoveAllListeners();
                _tabDrivers.onClick.AddListener(() => SetActiveTab("drivers"));
            }

            if (_tabConstructors != null)
            {
                _tabConstructors.onClick.RemoveAllListeners();
                _tabConstructors.onClick.AddListener(() => SetActiveTab("constructors"));
            }

            SetActiveTab("race");
        }

        private void SetActiveTab(string tab)
        {
            _activeTab = tab;

            if (_tabRaceIndicator != null)
                _tabRaceIndicator.color = tab == "race"
                    ? UITheme.AccentPrimary : Color.clear;
            if (_tabDriversIndicator != null)
                _tabDriversIndicator.color = tab == "drivers"
                    ? UITheme.AccentPrimary : Color.clear;
            if (_tabConstructorsIndicator != null)
                _tabConstructorsIndicator.color = tab == "constructors"
                    ? UITheme.AccentPrimary : Color.clear;
        }

        // ══════════════════════════════════════════════════════
        // CONTINUAR
        // ══════════════════════════════════════════════════════

        private void SetupContinueButton()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveAllListeners();
                _continueButton.onClick.AddListener(() => _onContinue?.Invoke());

                Image bg = _continueButton.GetComponent<Image>();
                if (bg != null) bg.color = UITheme.AccentPrimary;
            }

            if (_continueText != null)
            {
                _continueText.text = "CONTINUAR ▶";
                _continueText.color = UITheme.TextPrimary;
            }
        }

        // ══════════════════════════════════════════════════════
        // ANIMACIÓN DEL PODIO
        // ══════════════════════════════════════════════════════

        private IEnumerator PodiumEntryAnimation()
        {
            // Ocultar inicialmente
            if (_p3Platform != null) _p3Platform.localScale = Vector3.zero;
            if (_p2Platform != null) _p2Platform.localScale = Vector3.zero;
            if (_p1Platform != null) _p1Platform.localScale = Vector3.zero;

            yield return new WaitForSeconds(0.3f);

            // P3 primero
            yield return StartCoroutine(
                ScaleIn(_p3Platform, UITheme.ANIM_NORMAL));
            yield return new WaitForSeconds(0.2f);

            // P2
            yield return StartCoroutine(
                ScaleIn(_p2Platform, UITheme.ANIM_NORMAL));
            yield return new WaitForSeconds(0.2f);

            // P1 con más drama
            yield return StartCoroutine(
                ScaleIn(_p1Platform, UITheme.ANIM_SLOW));
        }

        private IEnumerator ScaleIn(RectTransform rt, float duration)
        {
            if (rt == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                // Overshoot para efecto bouncy
                float scale = t < 0.8f
                    ? Mathf.Lerp(0f, 1.1f, t / 0.8f)
                    : Mathf.Lerp(1.1f, 1f, (t - 0.8f) / 0.2f);
                rt.localScale = Vector3.one * scale;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
