// ============================================================
// F1 Career Manager — HubScreen.cs
// Pantalla principal del juego — estilo FC25
// ============================================================
// PREFAB: HubScreen_Prefab (Canvas fullscreen)
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;
using F1CareerManager.AI.PressAI;

namespace F1CareerManager.UI.Screens
{
    /// <summary>
    /// Pantalla principal estilo FC25.
    /// Cabecera: logo equipo, temporada, semana, presupuesto, posición.
    /// Menú lateral con NavigationBar.
    /// Feed central con NewsFeed.
    /// Panel inferior: próxima carrera, pilotos, R&D.
    /// Escucha EventBus para actualizar en tiempo real.
    /// </summary>
    public class HubScreen : MonoBehaviour
    {
        // ── Referencias: Header ──────────────────────────────
        [Header("Cabecera")]
        [SerializeField] private Image _teamLogo;
        [SerializeField] private Image _teamColorAccent;
        [SerializeField] private Text _teamNameText;
        [SerializeField] private Text _seasonWeekText;
        [SerializeField] private Text _budgetText;
        [SerializeField] private Text _positionText;

        // ── Referencias: Navegación ──────────────────────────
        [Header("Navegación")]
        [SerializeField] private Components.NavigationBar _navBar;

        // ── Referencias: Feed ────────────────────────────────
        [Header("Feed Central")]
        [SerializeField] private Components.NewsFeed _newsFeed;

        // ── Referencias: Panel inferior ──────────────────────
        [Header("Panel Inferior")]
        [SerializeField] private Text _nextRaceText;
        [SerializeField] private Text _nextRaceCountdown;
        [SerializeField] private Image _pilot1Sprite;
        [SerializeField] private Text _pilot1Name;
        [SerializeField] private Text _pilot1Mood;
        [SerializeField] private Image _pilot2Sprite;
        [SerializeField] private Text _pilot2Name;
        [SerializeField] private Text _pilot2Mood;
        [SerializeField] private Image _rndProgressBar;
        [SerializeField] private Text _rndProgressText;

        // ── Referencias: Background ──────────────────────────
        [Header("Fondo")]
        [SerializeField] private Image _backgroundImage;

        // ── Estado ───────────────────────────────────────────
        private TeamData _playerTeam;
        private List<PilotData> _teamPilots;
        private int _currentSeason;
        private int _currentWeek;
        private System.Action<string> _onNavigate;

        // ══════════════════════════════════════════════════════
        // CICLO DE VIDA
        // ══════════════════════════════════════════════════════

        private void OnEnable()
        {
            // Suscribirse a eventos del juego
            EventBus.Instance.OnBudgetChanged += HandleBudgetChanged;
            EventBus.Instance.OnPilotMoodChanged += HandleMoodChanged;
            EventBus.Instance.OnRaceFinished += HandleRaceFinished;
            EventBus.Instance.OnSeasonEnd += HandleSeasonEnd;
        }

        private void OnDisable()
        {
            EventBus.Instance.OnBudgetChanged -= HandleBudgetChanged;
            EventBus.Instance.OnPilotMoodChanged -= HandleMoodChanged;
            EventBus.Instance.OnRaceFinished -= HandleRaceFinished;
            EventBus.Instance.OnSeasonEnd -= HandleSeasonEnd;
        }

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Inicializa el hub con datos del juego.
        /// Llamar al entrar a la pantalla principal.
        /// </summary>
        public void Initialize(TeamData playerTeam, List<PilotData> pilots,
            int season, int week, List<GeneratedNews> news,
            CircuitData nextRace, System.Action<string> onNavigate)
        {
            _playerTeam = playerTeam;
            _teamPilots = pilots;
            _currentSeason = season;
            _currentWeek = week;
            _onNavigate = onNavigate;

            SetupHeader();
            SetupNavigation();
            SetupFeed(news);
            SetupBottomPanel(nextRace);
            SetupBackground();
        }

        // ══════════════════════════════════════════════════════
        // SETUP DE SECCIONES
        // ══════════════════════════════════════════════════════

        private void SetupHeader()
        {
            if (_teamNameText != null)
            {
                _teamNameText.text = _playerTeam.shortName;
                _teamNameText.color = UITheme.TextPrimary;
                _teamNameText.fontSize = UITheme.FONT_SIZE_HEADER;
            }

            if (_teamColorAccent != null)
                _teamColorAccent.color = UITheme.GetTeamColor(_playerTeam.id);

            if (_seasonWeekText != null)
            {
                _seasonWeekText.text =
                    $"Temporada {_currentSeason} — Semana {_currentWeek}";
                _seasonWeekText.color = UITheme.TextSecondary;
                _seasonWeekText.fontSize = UITheme.FONT_SIZE_SM;
            }

            RefreshBudget();
            RefreshPosition();
        }

        private void SetupNavigation()
        {
            if (_navBar != null)
            {
                _navBar.Initialize(OnSectionChanged);
            }
        }

        private void SetupFeed(List<GeneratedNews> news)
        {
            if (_newsFeed != null)
            {
                _newsFeed.Initialize(news, OnNewsAction);
            }
        }

        private void SetupBottomPanel(CircuitData nextRace)
        {
            // Próxima carrera
            if (_nextRaceText != null && nextRace != null)
            {
                _nextRaceText.text = $"🏁 {nextRace.name}";
                _nextRaceText.color = UITheme.TextPrimary;
                _nextRaceText.fontSize = UITheme.FONT_SIZE_MD;
            }

            if (_nextRaceCountdown != null && nextRace != null)
            {
                int weeksToRace = nextRace.round - _currentWeek;
                _nextRaceCountdown.text = weeksToRace > 0
                    ? $"en {weeksToRace} semana{(weeksToRace > 1 ? "s" : "")}"
                    : "¡Esta semana!";
                _nextRaceCountdown.color = weeksToRace <= 1
                    ? UITheme.AccentPrimary : UITheme.TextSecondary;
                _nextRaceCountdown.fontSize = UITheme.FONT_SIZE_SM;
            }

            // Estado de pilotos
            RefreshPilotStatus();

            // Progreso R&D
            if (_rndProgressBar != null)
            {
                _rndProgressBar.fillAmount = _playerTeam.rndProgress / 100f;
                _rndProgressBar.color = UITheme.AccentTertiary;
            }

            if (_rndProgressText != null)
            {
                _rndProgressText.text = $"R&D: {_playerTeam.rndProgress:F0}%";
                _rndProgressText.color = UITheme.TextSecondary;
                _rndProgressText.fontSize = UITheme.FONT_SIZE_XS;
            }
        }

        private void SetupBackground()
        {
            if (_backgroundImage != null)
                _backgroundImage.color = UITheme.BackgroundDark;
        }

        // ══════════════════════════════════════════════════════
        // ACTUALIZACIONES EN TIEMPO REAL
        // ══════════════════════════════════════════════════════

        private void RefreshBudget()
        {
            if (_budgetText != null && _playerTeam != null)
            {
                _budgetText.text = $"💰 ${_playerTeam.budget:F1}M";
                _budgetText.color = _playerTeam.budget < 20f
                    ? UITheme.TextNegative : UITheme.TextPrimary;
                _budgetText.fontSize = UITheme.FONT_SIZE_MD;
            }
        }

        private void RefreshPosition()
        {
            if (_positionText != null && _playerTeam != null)
            {
                _positionText.text = $"📍 P{_playerTeam.constructorPosition}";
                _positionText.color = _playerTeam.constructorPosition <= 3
                    ? UITheme.TextPositive : UITheme.TextPrimary;
                _positionText.fontSize = UITheme.FONT_SIZE_MD;
            }
        }

        private void RefreshPilotStatus()
        {
            if (_teamPilots == null) return;

            // Piloto 1
            if (_teamPilots.Count > 0)
            {
                var p1 = _teamPilots[0];
                if (_pilot1Name != null)
                {
                    _pilot1Name.text = $"{p1.firstName[0]}. {p1.lastName}";
                    _pilot1Name.color = UITheme.TextPrimary;
                    _pilot1Name.fontSize = UITheme.FONT_SIZE_SM;
                }
                if (_pilot1Mood != null)
                {
                    _pilot1Mood.text = UITheme.GetMoodEmoji(p1.mood);
                    _pilot1Mood.fontSize = UITheme.FONT_SIZE_LG;
                }
            }

            // Piloto 2
            if (_teamPilots.Count > 1)
            {
                var p2 = _teamPilots[1];
                if (_pilot2Name != null)
                {
                    _pilot2Name.text = $"{p2.firstName[0]}. {p2.lastName}";
                    _pilot2Name.color = UITheme.TextPrimary;
                    _pilot2Name.fontSize = UITheme.FONT_SIZE_SM;
                }
                if (_pilot2Mood != null)
                {
                    _pilot2Mood.text = UITheme.GetMoodEmoji(p2.mood);
                    _pilot2Mood.fontSize = UITheme.FONT_SIZE_LG;
                }
            }
        }

        /// <summary>Avanza una semana en el hub</summary>
        public void AdvanceWeek(int newWeek, CircuitData nextRace)
        {
            _currentWeek = newWeek;

            if (_seasonWeekText != null)
                _seasonWeekText.text =
                    $"Temporada {_currentSeason} — Semana {_currentWeek}";

            SetupBottomPanel(nextRace);
            RefreshBudget();
            RefreshPosition();
            RefreshPilotStatus();
        }

        // ══════════════════════════════════════════════════════
        // HANDLERS DE EVENTOS
        // ══════════════════════════════════════════════════════

        private void HandleBudgetChanged(object sender,
            EventBus.BudgetChangedArgs args)
        {
            if (args.TeamId == _playerTeam?.id)
                RefreshBudget();
        }

        private void HandleMoodChanged(object sender,
            EventBus.PilotMoodChangedArgs args)
        {
            if (args.TeamId == _playerTeam?.id)
                RefreshPilotStatus();
        }

        private void HandleRaceFinished(object sender,
            EventBus.RaceFinishedArgs args)
        {
            RefreshPosition();
            RefreshBudget();

            // Notificar que hay resultados nuevos
            if (_navBar != null)
                _navBar.SetBadge("race", 1);
        }

        private void HandleSeasonEnd(object sender,
            EventBus.SeasonEndArgs args)
        {
            _currentSeason++;
            _currentWeek = 1;
            if (_seasonWeekText != null)
                _seasonWeekText.text =
                    $"Temporada {_currentSeason} — Semana {_currentWeek}";
        }

        // ══════════════════════════════════════════════════════
        // NAVEGACIÓN
        // ══════════════════════════════════════════════════════

        private void OnSectionChanged(string sectionId)
        {
            _onNavigate?.Invoke(sectionId);
        }

        private void OnNewsAction(GeneratedNews news)
        {
            // Navegar según tipo de noticia
            if (news.Headline.Contains("lesión"))
                _onNavigate?.Invoke("pilots");
            else if (news.Headline.Contains("R&D"))
                _onNavigate?.Invoke("rnd");
            else if (news.Headline.Contains("contrato"))
                _onNavigate?.Invoke("market");
            else
                _onNavigate?.Invoke("hub");
        }

        // ══════════════════════════════════════════════════════
        // SHOW / HIDE
        // ══════════════════════════════════════════════════════

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshBudget();
            RefreshPosition();
            RefreshPilotStatus();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
