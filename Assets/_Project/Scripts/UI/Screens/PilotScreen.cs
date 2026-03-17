// ============================================================
// F1 Career Manager — PilotScreen.cs
// Pantalla de gestión de pilotos
// ============================================================
// PREFAB: PilotScreen_Prefab
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.UI.Screens
{
    /// <summary>
    /// Pantalla de pilotos del equipo.
    /// - 2 PilotCards completas
    /// - Botones: ver detalles, hablar, renovar
    /// - Sección academia junior
    /// - PilotDetailView con RadarChart, historial, mood system
    /// </summary>
    public class PilotScreen : MonoBehaviour
    {
        // ── Referencias: Pilotos principales ─────────────────
        [Header("Pilotos del equipo")]
        [SerializeField] private Components.PilotCard _pilot1Card;
        [SerializeField] private Components.PilotCard _pilot2Card;

        [Header("Botones Piloto 1")]
        [SerializeField] private Button _pilot1DetailsBtn;
        [SerializeField] private Button _pilot1TalkBtn;
        [SerializeField] private Button _pilot1RenewBtn;

        [Header("Botones Piloto 2")]
        [SerializeField] private Button _pilot2DetailsBtn;
        [SerializeField] private Button _pilot2TalkBtn;
        [SerializeField] private Button _pilot2RenewBtn;

        // ── Referencias: Panel de detalle ────────────────────
        [Header("Detalle")]
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private Components.RadarChart _detailRadar;
        [SerializeField] private Text _detailNameText;
        [SerializeField] private Text _detailBioText;
        [SerializeField] private Components.StarRating _detailStars;

        [Header("Stats del detalle")]
        [SerializeField] private Components.StatBar _statSpeed;
        [SerializeField] private Components.StatBar _statConsistency;
        [SerializeField] private Components.StatBar _statRain;
        [SerializeField] private Components.StatBar _statStart;
        [SerializeField] private Components.StatBar _statDefense;
        [SerializeField] private Components.StatBar _statAttack;
        [SerializeField] private Components.StatBar _statTires;
        [SerializeField] private Components.StatBar _statFuel;
        [SerializeField] private Components.StatBar _statConcentration;
        [SerializeField] private Components.StatBar _statAdaptability;

        [Header("Mood detallado")]
        [SerializeField] private Text _detailMoodText;
        [SerializeField] private Text _detailMoodValueText;
        [SerializeField] private Image _detailMoodBar;

        [Header("Historial")]
        [SerializeField] private Text _detailHistoryText;

        [Header("Cerrar detalle")]
        [SerializeField] private Button _closeDetailBtn;

        // ── Referencias: Junior Academy ──────────────────────
        [Header("Academia Junior")]
        [SerializeField] private Transform _juniorContainer;
        [SerializeField] private Text _juniorTitle;

        // ── Estado ───────────────────────────────────────────
        private TeamData _team;
        private List<PilotData> _teamPilots;
        private List<PilotData> _juniorPilots;
        private PilotData _selectedPilot;
        private System.Action<PilotData> _onRenewContract;
        private System.Action<PilotData> _onTalkToPilot;

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ══════════════════════════════════════════════════════

        public void Initialize(TeamData team, List<PilotData> teamPilots,
            List<PilotData> juniors,
            System.Action<PilotData> onRenew = null,
            System.Action<PilotData> onTalk = null)
        {
            _team = team;
            _teamPilots = teamPilots;
            _juniorPilots = juniors ?? new List<PilotData>();
            _onRenewContract = onRenew;
            _onTalkToPilot = onTalk;

            SetupMainPilots();
            SetupButtons();
            SetupJuniorAcademy();

            if (_detailPanel != null)
                _detailPanel.SetActive(false);
        }

        // ══════════════════════════════════════════════════════
        // PILOTOS PRINCIPALES
        // ══════════════════════════════════════════════════════

        private void SetupMainPilots()
        {
            Color teamColor = UITheme.GetTeamColor(_team.id);

            if (_teamPilots.Count > 0 && _pilot1Card != null)
                _pilot1Card.Setup(_teamPilots[0], _team.id, OnPilotCardClicked);

            if (_teamPilots.Count > 1 && _pilot2Card != null)
                _pilot2Card.Setup(_teamPilots[1], _team.id, OnPilotCardClicked);
        }

        private void SetupButtons()
        {
            // Piloto 1
            if (_pilot1DetailsBtn != null && _teamPilots.Count > 0)
            {
                _pilot1DetailsBtn.onClick.RemoveAllListeners();
                var p1 = _teamPilots[0];
                _pilot1DetailsBtn.onClick.AddListener(() => ShowDetails(p1));
                StyleButton(_pilot1DetailsBtn, "Ver detalles", UITheme.AccentTertiary);
            }

            if (_pilot1TalkBtn != null && _teamPilots.Count > 0)
            {
                _pilot1TalkBtn.onClick.RemoveAllListeners();
                var p1 = _teamPilots[0];
                _pilot1TalkBtn.onClick.AddListener(() => TalkToPilot(p1));
                StyleButton(_pilot1TalkBtn, "Hablar 💬", UITheme.AccentSecondary);
            }

            if (_pilot1RenewBtn != null && _teamPilots.Count > 0)
            {
                _pilot1RenewBtn.onClick.RemoveAllListeners();
                var p1 = _teamPilots[0];
                _pilot1RenewBtn.onClick.AddListener(() => RenewContract(p1));
                StyleButton(_pilot1RenewBtn, "Renovar 📝", UITheme.AccentPrimary);
            }

            // Piloto 2 (análogo)
            if (_pilot2DetailsBtn != null && _teamPilots.Count > 1)
            {
                _pilot2DetailsBtn.onClick.RemoveAllListeners();
                var p2 = _teamPilots[1];
                _pilot2DetailsBtn.onClick.AddListener(() => ShowDetails(p2));
                StyleButton(_pilot2DetailsBtn, "Ver detalles", UITheme.AccentTertiary);
            }

            if (_pilot2TalkBtn != null && _teamPilots.Count > 1)
            {
                _pilot2TalkBtn.onClick.RemoveAllListeners();
                var p2 = _teamPilots[1];
                _pilot2TalkBtn.onClick.AddListener(() => TalkToPilot(p2));
                StyleButton(_pilot2TalkBtn, "Hablar 💬", UITheme.AccentSecondary);
            }

            if (_pilot2RenewBtn != null && _teamPilots.Count > 1)
            {
                _pilot2RenewBtn.onClick.RemoveAllListeners();
                var p2 = _teamPilots[1];
                _pilot2RenewBtn.onClick.AddListener(() => RenewContract(p2));
                StyleButton(_pilot2RenewBtn, "Renovar 📝", UITheme.AccentPrimary);
            }

            // Cerrar detalle
            if (_closeDetailBtn != null)
            {
                _closeDetailBtn.onClick.RemoveAllListeners();
                _closeDetailBtn.onClick.AddListener(HideDetails);
            }
        }

        // ══════════════════════════════════════════════════════
        // DETALLE DEL PILOTO
        // ══════════════════════════════════════════════════════

        private void ShowDetails(PilotData pilot)
        {
            _selectedPilot = pilot;

            if (_detailPanel != null)
                _detailPanel.SetActive(true);

            Color teamColor = UITheme.GetTeamColor(_team.id);

            // Nombre y bio
            if (_detailNameText != null)
            {
                _detailNameText.text = $"{pilot.firstName} {pilot.lastName}";
                _detailNameText.color = UITheme.TextPrimary;
                _detailNameText.fontSize = UITheme.FONT_SIZE_HEADER;
            }

            if (_detailBioText != null)
            {
                _detailBioText.text = $"#{pilot.number} | {pilot.nationality} | " +
                    $"Edad: {pilot.age} | {pilot.role}";
                _detailBioText.color = UITheme.TextSecondary;
            }

            if (_detailStars != null)
                _detailStars.SetRating(pilot.stars);

            // Radar chart
            if (_detailRadar != null)
                _detailRadar.SetFromPilotData(pilot, teamColor);

            // Stats completos
            SetupDetailStats(pilot);
            SetupDetailMood(pilot);
            SetupDetailHistory(pilot);
        }

        private void SetupDetailStats(PilotData pilot)
        {
            if (_statSpeed != null) _statSpeed.Setup("Velocidad", pilot.speed);
            if (_statConsistency != null) _statConsistency.Setup("Consistencia", pilot.consistency);
            if (_statRain != null) _statRain.Setup("Lluvia", pilot.rainSkill);
            if (_statStart != null) _statStart.Setup("Salidas", pilot.startSkill);
            if (_statDefense != null) _statDefense.Setup("Defensa", pilot.defense);
            if (_statAttack != null) _statAttack.Setup("Ataque", pilot.attack);
            if (_statTires != null) _statTires.Setup("Neumáticos", pilot.tireManagement);
            if (_statFuel != null) _statFuel.Setup("Combustible", pilot.fuelManagement);
            if (_statConcentration != null) _statConcentration.Setup("Concentración", pilot.concentration);
            if (_statAdaptability != null) _statAdaptability.Setup("Adaptabilidad", pilot.adaptability);
        }

        private void SetupDetailMood(PilotData pilot)
        {
            if (_detailMoodText != null)
            {
                string emoji = UITheme.GetMoodEmoji(pilot.mood);
                _detailMoodText.text = $"{emoji} {pilot.mood}";
                _detailMoodText.color = UITheme.GetMoodColor(pilot.mood);
            }

            if (_detailMoodValueText != null)
            {
                _detailMoodValueText.text = $"Humor: {pilot.moodValue}/100";
                _detailMoodValueText.color = UITheme.TextSecondary;
            }

            if (_detailMoodBar != null)
            {
                _detailMoodBar.fillAmount = pilot.moodValue / 100f;
                _detailMoodBar.color = UITheme.GetMoodColor(pilot.mood);
            }
        }

        private void SetupDetailHistory(PilotData pilot)
        {
            if (_detailHistoryText != null)
            {
                _detailHistoryText.text =
                    $"Carreras: {pilot.totalRaces} | " +
                    $"Victorias: {pilot.totalWins} | " +
                    $"Podios: {pilot.totalPodiums}\n" +
                    $"Campeonatos: {pilot.championships} | " +
                    $"Mejor resultado: P{pilot.bestFinish}\n" +
                    $"Contrato: {pilot.contractYearsLeft} año(s) | " +
                    $"Salario: ${pilot.salary:F1}M";
                _detailHistoryText.color = UITheme.TextSecondary;
                _detailHistoryText.fontSize = UITheme.FONT_SIZE_SM;
            }
        }

        private void HideDetails()
        {
            if (_detailPanel != null)
                _detailPanel.SetActive(false);
            _selectedPilot = null;
        }

        // ══════════════════════════════════════════════════════
        // ACCIONES
        // ══════════════════════════════════════════════════════

        private void TalkToPilot(PilotData pilot)
        {
            _onTalkToPilot?.Invoke(pilot);

            // Efecto inmediato: +5 humor
            pilot.moodValue = Mathf.Min(100, pilot.moodValue + 5);
            Components.NotificationToast.ShowSuccess(
                $"Hablaste con {pilot.lastName}: humor +5");

            // Refrescar cards
            RefreshCards();
        }

        private void RenewContract(PilotData pilot)
        {
            _onRenewContract?.Invoke(pilot);
        }

        private void OnPilotCardClicked(PilotData pilot)
        {
            ShowDetails(pilot);
        }

        // ══════════════════════════════════════════════════════
        // JUNIOR ACADEMY
        // ══════════════════════════════════════════════════════

        private void SetupJuniorAcademy()
        {
            if (_juniorTitle != null)
            {
                _juniorTitle.text = $"🎓 Academia Junior ({_juniorPilots.Count})";
                _juniorTitle.color = UITheme.TextPrimary;
                _juniorTitle.fontSize = UITheme.FONT_SIZE_MD;
            }

            // Crear mini-cards de juniors
            if (_juniorContainer != null)
            {
                foreach (Transform child in _juniorContainer)
                    Destroy(child.gameObject);

                foreach (var junior in _juniorPilots)
                {
                    CreateJuniorMiniCard(junior);
                }
            }
        }

        private void CreateJuniorMiniCard(PilotData pilot)
        {
            if (_juniorContainer == null) return;

            GameObject card = new GameObject($"Junior_{pilot.id}",
                typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_juniorContainer, false);

            Image bg = card.GetComponent<Image>();
            bg.color = UITheme.BackgroundCard;

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 60f);

            // Nombre
            GameObject nameObj = new GameObject("Name", typeof(Text));
            nameObj.transform.SetParent(card.transform, false);
            Text nameText = nameObj.GetComponent<Text>();
            nameText.text = $"{pilot.firstName[0]}. {pilot.lastName}";
            nameText.color = UITheme.TextPrimary;
            nameText.fontSize = UITheme.FONT_SIZE_SM;
            nameText.alignment = TextAnchor.MiddleLeft;
            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.5f);
            nameRt.anchorMax = new Vector2(0.7f, 1f);
            nameRt.offsetMin = new Vector2(UITheme.PADDING_SM, 0);
            nameRt.offsetMax = Vector2.zero;

            // Potencial
            GameObject potObj = new GameObject("Potential", typeof(Text));
            potObj.transform.SetParent(card.transform, false);
            Text potText = potObj.GetComponent<Text>();
            potText.text = $"⭐{pilot.stars} | Pot: {pilot.potential}";
            potText.color = UITheme.TextMuted;
            potText.fontSize = UITheme.FONT_SIZE_XS;
            potText.alignment = TextAnchor.MiddleLeft;
            RectTransform potRt = potObj.GetComponent<RectTransform>();
            potRt.anchorMin = new Vector2(0, 0);
            potRt.anchorMax = new Vector2(0.7f, 0.5f);
            potRt.offsetMin = new Vector2(UITheme.PADDING_SM, 0);
            potRt.offsetMax = Vector2.zero;
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private void RefreshCards()
        {
            if (_pilot1Card != null) _pilot1Card.Refresh();
            if (_pilot2Card != null) _pilot2Card.Refresh();
        }

        private void StyleButton(Button btn, string label, Color color)
        {
            Image bg = btn.GetComponent<Image>();
            if (bg != null) bg.color = color;

            Text text = btn.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
                text.color = UITheme.TextPrimary;
                text.fontSize = UITheme.FONT_SIZE_SM;
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
