// ============================================================
// F1 Career Manager — ContractPopup.cs
// Modal de negociación de contrato
// ============================================================
// PREFAB: ContractPopup_Prefab (fullscreen overlay)
// Integra con NegotiationSystem.cs
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using F1CareerManager.Data;
using F1CareerManager.Market;

namespace F1CareerManager.UI.Components
{
    /// <summary>
    /// Modal de negociación de contrato.
    /// Muestra: salario pedido vs ofrecido, duración, rol, bonos.
    /// Slider para ajustar salario ofrecido.
    /// Feedback visual de aceptación/rechazo/contraoferta.
    /// </summary>
    public class ContractPopup : MonoBehaviour
    {
        // ── Referencias: Overlay ─────────────────────────────
        [Header("Overlay")]
        [SerializeField] private Image _overlayBg;
        [SerializeField] private GameObject _popupPanel;

        // ── Referencias: Info del piloto ─────────────────────
        [Header("Piloto")]
        [SerializeField] private Image _pilotSprite;
        [SerializeField] private Text _pilotName;
        [SerializeField] private Text _pilotOverall;
        [SerializeField] private StarRating _pilotStars;
        [SerializeField] private Text _pilotCurrentTeam;

        // ── Referencias: Términos ────────────────────────────
        [Header("Términos del contrato")]
        [SerializeField] private Slider _salarySlider;
        [SerializeField] private Text _salaryLabel;
        [SerializeField] private Text _salaryExpectedLabel;
        [SerializeField] private Text _durationLabel;
        [SerializeField] private Button _durationMinus;
        [SerializeField] private Button _durationPlus;
        [SerializeField] private Text _roleLabel;
        [SerializeField] private Button _roleToggle;
        [SerializeField] private Text _releaseCLabel;
        [SerializeField] private Slider _releaseClauseSlider;
        [SerializeField] private Text _bonusWinLabel;
        [SerializeField] private Slider _bonusWinSlider;

        // ── Referencias: Satisfacción ────────────────────────
        [Header("Evaluación")]
        [SerializeField] private Image _satisfactionBar;
        [SerializeField] private Image _satisfactionBarBg;
        [SerializeField] private Text _satisfactionLabel;
        [SerializeField] private Text _pilotResponseText;

        // ── Referencias: Botones ─────────────────────────────
        [Header("Botones")]
        [SerializeField] private Button _offerButton;
        [SerializeField] private Text _offerButtonText;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Text _roundLabel;

        // ── Estado ───────────────────────────────────────────
        private PilotData _pilot;
        private TeamData _team;
        private NegotiationState _currentNego;
        private NegotiationSystem _negotiationSystem;
        private System.Action<bool> _onComplete; // true=fichado, false=cancelado

        // Valores del formulario
        private float _offerSalary;
        private int _offerYears = 2;
        private string _offerRole = "Second";
        private float _offerReleaseClause;
        private float _offerBonusWin;

        // ══════════════════════════════════════════════════════
        // ABRIR / CERRAR
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Abre el popup de negociación para un piloto.
        /// </summary>
        public void Open(PilotData pilot, TeamData team,
            NegotiationSystem negoSystem,
            System.Action<bool> onComplete = null)
        {
            _pilot = pilot;
            _team = team;
            _negotiationSystem = negoSystem;
            _onComplete = onComplete;

            // Empezar negociación
            _currentNego = _negotiationSystem.StartNegotiation(
                pilot, team, "Second", false);

            // Valores iniciales
            _offerSalary = pilot.salary;
            _offerYears = 2;
            _offerRole = "Second";
            _offerReleaseClause = pilot.salary * 3f;
            _offerBonusWin = 0.5f;

            SetupPilotInfo();
            SetupSliders();
            SetupButtons();
            UpdateSatisfactionPreview();

            // Mostrar con animación
            gameObject.SetActive(true);
            _popupPanel.SetActive(true);
            if (_overlayBg != null) _overlayBg.color = UITheme.BackgroundOverlay;

            StartCoroutine(OpenAnimation());
        }

        /// <summary>Cierra el popup</summary>
        public void Close()
        {
            StartCoroutine(CloseAnimation());
        }

        // ══════════════════════════════════════════════════════
        // SETUP
        // ══════════════════════════════════════════════════════

        private void SetupPilotInfo()
        {
            if (_pilotName != null)
            {
                _pilotName.text = $"{_pilot.firstName} {_pilot.lastName}";
                _pilotName.color = UITheme.TextPrimary;
            }

            if (_pilotOverall != null)
            {
                _pilotOverall.text = _pilot.overallRating.ToString();
                _pilotOverall.color = UITheme.GetStatColor(_pilot.overallRating);
            }

            if (_pilotStars != null)
                _pilotStars.SetRating(_pilot.stars);

            if (_pilotCurrentTeam != null)
            {
                bool isFree = string.IsNullOrEmpty(_pilot.currentTeamId);
                _pilotCurrentTeam.text = isFree ? "Agente libre" : _pilot.currentTeamId;
                _pilotCurrentTeam.color = isFree
                    ? UITheme.TextPositive : UITheme.TextSecondary;
            }

            if (_roundLabel != null)
            {
                _roundLabel.text = $"Ronda {_currentNego?.CurrentRound ?? 1} / 3";
                _roundLabel.color = UITheme.TextSecondary;
            }
        }

        private void SetupSliders()
        {
            // Slider de salario
            if (_salarySlider != null)
            {
                float minSalary = _pilot.salary * 0.5f;
                float maxSalary = _pilot.salary * 2.5f;
                _salarySlider.minValue = minSalary;
                _salarySlider.maxValue = maxSalary;
                _salarySlider.value = _offerSalary;
                _salarySlider.onValueChanged.RemoveAllListeners();
                _salarySlider.onValueChanged.AddListener(OnSalaryChanged);
            }

            if (_salaryExpectedLabel != null)
            {
                _salaryExpectedLabel.text = $"Pide: ${_pilot.salary:F1}M/año";
                _salaryExpectedLabel.color = UITheme.TextMuted;
            }

            // Slider cláusula de salida
            if (_releaseClauseSlider != null)
            {
                _releaseClauseSlider.minValue = _pilot.salary * 1f;
                _releaseClauseSlider.maxValue = _pilot.salary * 10f;
                _releaseClauseSlider.value = _offerReleaseClause;
                _releaseClauseSlider.onValueChanged.RemoveAllListeners();
                _releaseClauseSlider.onValueChanged.AddListener(OnReleaseClauseChanged);
            }

            // Slider bonus victoria
            if (_bonusWinSlider != null)
            {
                _bonusWinSlider.minValue = 0f;
                _bonusWinSlider.maxValue = 2f;
                _bonusWinSlider.value = _offerBonusWin;
                _bonusWinSlider.onValueChanged.RemoveAllListeners();
                _bonusWinSlider.onValueChanged.AddListener(OnBonusWinChanged);
            }

            UpdateLabels();
        }

        private void SetupButtons()
        {
            // Duración +/-
            if (_durationMinus != null)
            {
                _durationMinus.onClick.RemoveAllListeners();
                _durationMinus.onClick.AddListener(() =>
                {
                    _offerYears = Mathf.Max(1, _offerYears - 1);
                    UpdateLabels();
                    UpdateSatisfactionPreview();
                });
            }

            if (_durationPlus != null)
            {
                _durationPlus.onClick.RemoveAllListeners();
                _durationPlus.onClick.AddListener(() =>
                {
                    _offerYears = Mathf.Min(5, _offerYears + 1);
                    UpdateLabels();
                    UpdateSatisfactionPreview();
                });
            }

            // Toggle de rol
            if (_roleToggle != null)
            {
                _roleToggle.onClick.RemoveAllListeners();
                _roleToggle.onClick.AddListener(() =>
                {
                    _offerRole = _offerRole == "First" ? "Second" : "First";
                    UpdateLabels();
                    UpdateSatisfactionPreview();
                });
            }

            // Botón de ofrecer
            if (_offerButton != null)
            {
                _offerButton.onClick.RemoveAllListeners();
                _offerButton.onClick.AddListener(OnOfferClicked);

                Image btnBg = _offerButton.GetComponent<Image>();
                if (btnBg != null) btnBg.color = UITheme.AccentPrimary;
            }

            if (_offerButtonText != null)
                _offerButtonText.text = "ENVIAR OFERTA";

            // Botón cancelar
            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(() =>
                {
                    _onComplete?.Invoke(false);
                    Close();
                });
            }
        }

        // ══════════════════════════════════════════════════════
        // ACTUALIZACIÓN DE UI
        // ══════════════════════════════════════════════════════

        private void OnSalaryChanged(float value)
        {
            _offerSalary = value;
            UpdateLabels();
            UpdateSatisfactionPreview();
        }

        private void OnReleaseClauseChanged(float value)
        {
            _offerReleaseClause = value;
            UpdateLabels();
            UpdateSatisfactionPreview();
        }

        private void OnBonusWinChanged(float value)
        {
            _offerBonusWin = value;
            UpdateLabels();
            UpdateSatisfactionPreview();
        }

        private void UpdateLabels()
        {
            if (_salaryLabel != null)
            {
                _salaryLabel.text = $"${_offerSalary:F1}M/año";
                _salaryLabel.color = _offerSalary >= _pilot.salary
                    ? UITheme.TextPositive : UITheme.TextNegative;
            }

            if (_durationLabel != null)
            {
                _durationLabel.text = $"{_offerYears} año{(_offerYears > 1 ? "s" : "")}";
                _durationLabel.color = UITheme.TextPrimary;
            }

            if (_roleLabel != null)
            {
                _roleLabel.text = _offerRole == "First"
                    ? "PILOTO #1" : "PILOTO #2";
                _roleLabel.color = _offerRole == "First"
                    ? UITheme.AccentGold : UITheme.TextSecondary;
            }

            if (_releaseCLabel != null)
            {
                _releaseCLabel.text = $"${_offerReleaseClause:F1}M";
                _releaseCLabel.color = UITheme.TextSecondary;
            }

            if (_bonusWinLabel != null)
            {
                _bonusWinLabel.text = $"${_offerBonusWin:F2}M/victoria";
                _bonusWinLabel.color = UITheme.TextSecondary;
            }
        }

        private void UpdateSatisfactionPreview()
        {
            if (_currentNego == null) return;

            // Calcular satisfacción estimada (preview)
            float satisfaction = EstimateSatisfaction();

            if (_satisfactionBar != null && _satisfactionBarBg != null)
            {
                _satisfactionBarBg.color = UITheme.BackgroundInput;
                _satisfactionBar.fillAmount = satisfaction;

                if (satisfaction >= 0.80f)
                    _satisfactionBar.color = UITheme.TextPositive;
                else if (satisfaction >= 0.60f)
                    _satisfactionBar.color = UITheme.TextWarning;
                else
                    _satisfactionBar.color = UITheme.TextNegative;
            }

            if (_satisfactionLabel != null)
            {
                _satisfactionLabel.text = $"Satisfacción: {satisfaction * 100:F0}%";
                if (satisfaction >= 0.80f)
                    _satisfactionLabel.color = UITheme.TextPositive;
                else if (satisfaction >= 0.60f)
                    _satisfactionLabel.color = UITheme.TextWarning;
                else
                    _satisfactionLabel.color = UITheme.TextNegative;
            }
        }

        private float EstimateSatisfaction()
        {
            float sat = 0f;

            // Salario vs expectativa (40%)
            float salaryRatio = _offerSalary / Mathf.Max(0.1f, _pilot.salary);
            sat += Mathf.Clamp01(salaryRatio) * 0.40f;

            // Rol (15%)
            if (_offerRole == "First" || _pilot.ego < 75)
                sat += 0.15f;
            else if (_offerRole == "Second" && _pilot.ego >= 75)
                sat += 0.0f; // Ego alto no acepta #2

            // Duración (10%)
            if (_offerYears >= 2) sat += 0.10f;
            else sat += 0.05f;

            // Cláusula de salida (10%)
            float clauseRatio = _offerReleaseClause / Mathf.Max(0.1f, _pilot.salary * 5f);
            sat += Mathf.Clamp01(clauseRatio) * 0.10f;

            // Bonus (10%)
            if (_offerBonusWin > 0.3f) sat += 0.10f;
            else sat += 0.05f;

            // Base por equipo atractivo (15%)
            sat += 0.15f * (_team.reputation / 100f);

            return Mathf.Clamp01(sat);
        }

        // ══════════════════════════════════════════════════════
        // ENVÍO DE OFERTA
        // ══════════════════════════════════════════════════════

        private void OnOfferClicked()
        {
            if (_currentNego == null) return;

            // Hacer oferta a través del NegotiationSystem
            NegotiationResult result = _negotiationSystem.MakeOffer(
                _currentNego, _offerSalary, _offerReleaseClause,
                _offerRole, _offerYears, _offerBonusWin, 0f);

            // Mostrar respuesta
            ShowResponse(result);
        }

        private void ShowResponse(NegotiationResult result)
        {
            if (_pilotResponseText != null)
            {
                _pilotResponseText.text = result.Response;
                _pilotResponseText.gameObject.SetActive(true);
            }

            switch (result.Outcome)
            {
                case "Accepted":
                    if (_pilotResponseText != null)
                        _pilotResponseText.color = UITheme.TextPositive;
                    if (_offerButtonText != null)
                        _offerButtonText.text = "✅ ¡FICHADO!";
                    NotificationToast.ShowSuccess(
                        $"¡{_pilot.lastName} acepta la oferta!");

                    // Cerrar después de delay
                    StartCoroutine(DelayedComplete(true));
                    break;

                case "Rejected":
                    if (_pilotResponseText != null)
                        _pilotResponseText.color = UITheme.TextNegative;
                    if (_offerButtonText != null)
                        _offerButtonText.text = "❌ RECHAZADO";
                    NotificationToast.ShowError(
                        $"{_pilot.lastName} rechaza la oferta.");

                    StartCoroutine(DelayedComplete(false));
                    break;

                case "Counter":
                    if (_pilotResponseText != null)
                        _pilotResponseText.color = UITheme.TextWarning;

                    // Actualizar ronda
                    if (_roundLabel != null)
                        _roundLabel.text = $"Ronda {_currentNego.CurrentRound} / 3";

                    // Ajustar slider al contraoferta
                    if (result.CounterSalary > 0)
                    {
                        _offerSalary = result.CounterSalary;
                        if (_salarySlider != null)
                            _salarySlider.value = _offerSalary;
                    }

                    if (_offerButtonText != null)
                        _offerButtonText.text = "ENVIAR CONTRAOFERTA";

                    UpdateLabels();
                    UpdateSatisfactionPreview();
                    break;
            }
        }

        private IEnumerator DelayedComplete(bool success)
        {
            yield return new WaitForSeconds(1.5f);
            _onComplete?.Invoke(success);
            Close();
        }

        // ══════════════════════════════════════════════════════
        // ANIMACIONES
        // ══════════════════════════════════════════════════════

        private IEnumerator OpenAnimation()
        {
            if (_popupPanel == null) yield break;

            RectTransform rt = _popupPanel.GetComponent<RectTransform>();
            CanvasGroup cg = _popupPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = _popupPanel.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            rt.localScale = Vector3.one * 0.8f;
            cg.alpha = 0f;

            while (elapsed < UITheme.ANIM_NORMAL)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / UITheme.ANIM_NORMAL);
                rt.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                cg.alpha = t;
                yield return null;
            }

            rt.localScale = Vector3.one;
            cg.alpha = 1f;
        }

        private IEnumerator CloseAnimation()
        {
            if (_popupPanel == null) yield break;

            CanvasGroup cg = _popupPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = _popupPanel.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < UITheme.ANIM_FAST)
            {
                elapsed += Time.deltaTime;
                float t = 1f - (elapsed / UITheme.ANIM_FAST);
                cg.alpha = t;
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }

    /// <summary>Resultado de negociación para la UI</summary>
    public class NegotiationResult
    {
        public string Outcome;         // "Accepted", "Rejected", "Counter"
        public string Response;        // Texto del piloto
        public float CounterSalary;    // Salario contraofertado
    }
}
