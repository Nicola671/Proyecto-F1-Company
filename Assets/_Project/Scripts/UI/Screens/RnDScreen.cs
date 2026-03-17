// ============================================================
// F1 Career Manager — RnDScreen.cs
// Pantalla de R&D — auto con 4 zonas + tech tree
// ============================================================
// PREFAB: RnDScreen_Prefab
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using F1CareerManager.Data;

namespace F1CareerManager.UI.Screens
{
    /// <summary>
    /// Vista del auto con 4 zonas clicables.
    /// Tech tree visual con nodos.
    /// Radar chart del auto.
    /// Panel de componente seleccionado.
    /// </summary>
    public class RnDScreen : MonoBehaviour
    {
        // ── Referencias: Auto ────────────────────────────────
        [Header("Zonas del auto")]
        [SerializeField] private Button _aeroZone;
        [SerializeField] private Button _engineZone;
        [SerializeField] private Button _chassisZone;
        [SerializeField] private Button _reliabilityZone;
        [SerializeField] private Image _carImage;

        [Header("Labels de zonas")]
        [SerializeField] private Text _aeroRatingText;
        [SerializeField] private Text _engineRatingText;
        [SerializeField] private Text _chassisRatingText;
        [SerializeField] private Text _reliabilityRatingText;

        // ── Referencias: Tech Tree ───────────────────────────
        [Header("Tech Tree")]
        [SerializeField] private GameObject _techTreePanel;
        [SerializeField] private Transform _techNodeContainer;
        [SerializeField] private Text _techTreeTitle;
        [SerializeField] private Button _closeTechTreeBtn;

        // ── Referencias: Componente seleccionado ─────────────
        [Header("Panel de componente")]
        [SerializeField] private GameObject _componentPanel;
        [SerializeField] private Text _compNameText;
        [SerializeField] private Text _compAreaText;
        [SerializeField] private Text _compStatsText;
        [SerializeField] private Text _compCostText;
        [SerializeField] private Text _compTimeText;
        [SerializeField] private Text _compLegalityText;
        [SerializeField] private Image _compLegalityIndicator;
        [SerializeField] private Button _installButton;
        [SerializeField] private Text _installButtonText;

        // ── Referencias: Radar del auto ──────────────────────
        [Header("Radar del auto")]
        [SerializeField] private Components.RadarChart _carRadar;

        // ── Referencias: Rendimiento global ──────────────────
        [Header("Rendimiento global")]
        [SerializeField] private Text _globalPerformanceText;
        [SerializeField] private Image _globalPerformanceBar;

        // ── Estado ───────────────────────────────────────────
        private TeamData _team;
        private string _selectedArea;
        private ComponentData _selectedComponent;
        private List<ComponentData> _allComponents;
        private System.Action<ComponentData> _onInstallComponent;

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ══════════════════════════════════════════════════════

        public void Initialize(TeamData team, List<ComponentData> components,
            System.Action<ComponentData> onInstall = null)
        {
            _team = team;
            _allComponents = components ?? new List<ComponentData>();
            _onInstallComponent = onInstall;

            SetupCarView();
            SetupZoneButtons();
            UpdateCarRadar();
            UpdateGlobalPerformance();

            if (_techTreePanel != null) _techTreePanel.SetActive(false);
            if (_componentPanel != null) _componentPanel.SetActive(false);
        }

        // ══════════════════════════════════════════════════════
        // VISTA DEL AUTO
        // ══════════════════════════════════════════════════════

        private void SetupCarView()
        {
            Color teamColor = UITheme.GetTeamColor(_team.id);

            if (_carImage != null)
                _carImage.color = teamColor;

            // Ratings por zona
            if (_aeroRatingText != null)
            {
                _aeroRatingText.text = $"AERO\n{_team.aeroRating}";
                _aeroRatingText.color = UITheme.GetStatColor(_team.aeroRating);
                _aeroRatingText.fontSize = UITheme.FONT_SIZE_SM;
            }

            if (_engineRatingText != null)
            {
                _engineRatingText.text = $"MOTOR\n{_team.engineRating}";
                _engineRatingText.color = UITheme.GetStatColor(_team.engineRating);
                _engineRatingText.fontSize = UITheme.FONT_SIZE_SM;
            }

            if (_chassisRatingText != null)
            {
                _chassisRatingText.text = $"CHASIS\n{_team.chassisRating}";
                _chassisRatingText.color = UITheme.GetStatColor(_team.chassisRating);
                _chassisRatingText.fontSize = UITheme.FONT_SIZE_SM;
            }

            if (_reliabilityRatingText != null)
            {
                _reliabilityRatingText.text = $"FIAB\n{_team.reliabilityRating}";
                _reliabilityRatingText.color = UITheme.GetStatColor(_team.reliabilityRating);
                _reliabilityRatingText.fontSize = UITheme.FONT_SIZE_SM;
            }
        }

        private void SetupZoneButtons()
        {
            if (_aeroZone != null)
            {
                _aeroZone.onClick.RemoveAllListeners();
                _aeroZone.onClick.AddListener(() => OpenTechTree("Aerodynamics"));
            }

            if (_engineZone != null)
            {
                _engineZone.onClick.RemoveAllListeners();
                _engineZone.onClick.AddListener(() => OpenTechTree("Engine"));
            }

            if (_chassisZone != null)
            {
                _chassisZone.onClick.RemoveAllListeners();
                _chassisZone.onClick.AddListener(() => OpenTechTree("Chassis"));
            }

            if (_reliabilityZone != null)
            {
                _reliabilityZone.onClick.RemoveAllListeners();
                _reliabilityZone.onClick.AddListener(() => OpenTechTree("Reliability"));
            }

            if (_closeTechTreeBtn != null)
            {
                _closeTechTreeBtn.onClick.RemoveAllListeners();
                _closeTechTreeBtn.onClick.AddListener(CloseTechTree);
            }
        }

        // ══════════════════════════════════════════════════════
        // TECH TREE
        // ══════════════════════════════════════════════════════

        private void OpenTechTree(string area)
        {
            _selectedArea = area;

            if (_techTreePanel != null)
                _techTreePanel.SetActive(true);

            if (_techTreeTitle != null)
            {
                _techTreeTitle.text = GetAreaSpanish(area);
                _techTreeTitle.color = UITheme.TextPrimary;
                _techTreeTitle.fontSize = UITheme.FONT_SIZE_HEADER;
            }

            // Limpiar nodos anteriores
            if (_techNodeContainer != null)
            {
                foreach (Transform child in _techNodeContainer)
                    Destroy(child.gameObject);
            }

            // Filtrar componentes del área
            var areaComponents = _allComponents.FindAll(c => c.area == area);

            // Crear nodos del tech tree
            int index = 0;
            foreach (var comp in areaComponents)
            {
                CreateTechNode(comp, index);
                index++;
            }
        }

        private void CloseTechTree()
        {
            if (_techTreePanel != null)
                _techTreePanel.SetActive(false);
            if (_componentPanel != null)
                _componentPanel.SetActive(false);
        }

        private void CreateTechNode(ComponentData comp, int index)
        {
            if (_techNodeContainer == null) return;

            GameObject nodeObj = new GameObject($"Node_{comp.id}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            nodeObj.transform.SetParent(_techNodeContainer, false);

            // Layout
            RectTransform rt = nodeObj.GetComponent<RectTransform>();
            int row = index / 3;
            int col = index % 3;
            rt.sizeDelta = new Vector2(100f, 80f);
            rt.anchoredPosition = new Vector2(
                col * 120f + 60f, -(row * 100f + 50f));

            // Color según estado
            Image bg = nodeObj.GetComponent<Image>();
            bg.color = GetNodeColor(comp);

            // Nombre del nodo
            GameObject nameObj = new GameObject("Name", typeof(Text));
            nameObj.transform.SetParent(nodeObj.transform, false);
            Text nameText = nameObj.GetComponent<Text>();
            nameText.text = comp.shortName ?? comp.name;
            nameText.color = UITheme.TextPrimary;
            nameText.fontSize = UITheme.FONT_SIZE_XS;
            nameText.alignment = TextAnchor.MiddleCenter;
            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = Vector2.zero;
            nameRt.anchorMax = new Vector2(1, 0.5f);
            nameRt.offsetMin = new Vector2(2, 2);
            nameRt.offsetMax = new Vector2(-2, 0);

            // Estado icono
            GameObject statusObj = new GameObject("Status", typeof(Text));
            statusObj.transform.SetParent(nodeObj.transform, false);
            Text statusText = statusObj.GetComponent<Text>();
            statusText.text = GetStatusEmoji(comp);
            statusText.fontSize = UITheme.FONT_SIZE_MD;
            statusText.alignment = TextAnchor.MiddleCenter;
            RectTransform statusRt = statusObj.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0, 0.5f);
            statusRt.anchorMax = Vector2.one;
            statusRt.offsetMin = Vector2.zero;
            statusRt.offsetMax = Vector2.zero;

            // Click
            Button btn = nodeObj.GetComponent<Button>();
            var capturedComp = comp;
            btn.onClick.AddListener(() => SelectComponent(capturedComp));
        }

        // ══════════════════════════════════════════════════════
        // COMPONENTE SELECCIONADO
        // ══════════════════════════════════════════════════════

        private void SelectComponent(ComponentData comp)
        {
            _selectedComponent = comp;

            if (_componentPanel != null)
                _componentPanel.SetActive(true);

            if (_compNameText != null)
            {
                _compNameText.text = comp.name;
                _compNameText.color = UITheme.TextPrimary;
                _compNameText.fontSize = UITheme.FONT_SIZE_LG;
            }

            if (_compAreaText != null)
            {
                _compAreaText.text = GetAreaSpanish(comp.area);
                _compAreaText.color = UITheme.TextSecondary;
            }

            if (_compStatsText != null)
            {
                _compStatsText.text =
                    $"Rendimiento: +{comp.performanceGain}\n" +
                    $"Fiabilidad: {comp.reliability}%\n" +
                    $"Peso: {comp.weight:F1}kg";
                _compStatsText.color = UITheme.TextPrimary;
                _compStatsText.fontSize = UITheme.FONT_SIZE_SM;
            }

            if (_compCostText != null)
            {
                _compCostText.text = $"💰 ${comp.developmentCost:F1}M";
                _compCostText.color = comp.developmentCost > _team.budget
                    ? UITheme.TextNegative : UITheme.TextPositive;
            }

            if (_compTimeText != null)
            {
                _compTimeText.text = $"⏱ {comp.developmentWeeks} semanas";
                _compTimeText.color = UITheme.TextSecondary;
            }

            // Legalidad
            if (_compLegalityText != null)
            {
                bool isLegal = comp.legalityRisk <= 0.1f;
                _compLegalityText.text = isLegal ? "✅ Legal" :
                    $"⚠️ Riesgo: {comp.legalityRisk * 100:F0}%";
                _compLegalityText.color = isLegal
                    ? UITheme.TextPositive : UITheme.TextWarning;
            }

            if (_compLegalityIndicator != null)
            {
                _compLegalityIndicator.color = comp.legalityRisk <= 0.1f
                    ? UITheme.TextPositive : UITheme.TextWarning;
            }

            // Botón instalar
            if (_installButton != null)
            {
                bool canInstall = comp.status == "Available" &&
                    comp.developmentCost <= _team.budget;

                _installButton.interactable = canInstall;
                _installButton.onClick.RemoveAllListeners();
                _installButton.onClick.AddListener(OnInstallClicked);

                Image btnBg = _installButton.GetComponent<Image>();
                if (btnBg != null)
                    btnBg.color = canInstall
                        ? UITheme.AccentPrimary
                        : UITheme.TextMuted;
            }

            if (_installButtonText != null)
            {
                switch (comp.status)
                {
                    case "Installed":     _installButtonText.text = "✅ INSTALADO"; break;
                    case "InDevelopment": _installButtonText.text = "🔧 EN DESARROLLO"; break;
                    case "Locked":       _installButtonText.text = "🔒 BLOQUEADO"; break;
                    default:             _installButtonText.text = "DESARROLLAR"; break;
                }
            }
        }

        private void OnInstallClicked()
        {
            if (_selectedComponent != null)
            {
                _onInstallComponent?.Invoke(_selectedComponent);
                Components.NotificationToast.ShowInfo(
                    $"Desarrollando: {_selectedComponent.name}");
            }
        }

        // ══════════════════════════════════════════════════════
        // RADAR DEL AUTO
        // ══════════════════════════════════════════════════════

        private void UpdateCarRadar()
        {
            if (_carRadar == null || _team == null) return;

            _carRadar.SetFromCarData(
                _team.aeroRating, _team.engineRating,
                _team.chassisRating, _team.reliabilityRating,
                _team.pitStopSpeed, _team.carPerformance,
                UITheme.GetTeamColor(_team.id));
        }

        private void UpdateGlobalPerformance()
        {
            if (_globalPerformanceText != null)
            {
                _globalPerformanceText.text =
                    $"Rendimiento global: {_team.carPerformance}";
                _globalPerformanceText.color = UITheme.TextPrimary;
                _globalPerformanceText.fontSize = UITheme.FONT_SIZE_MD;
            }

            if (_globalPerformanceBar != null)
            {
                _globalPerformanceBar.fillAmount = _team.carPerformance / 100f;
                _globalPerformanceBar.color =
                    UITheme.GetStatColor(_team.carPerformance);
            }
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private Color GetNodeColor(ComponentData comp)
        {
            switch (comp.status)
            {
                case "Installed":      return UITheme.WithAlpha(UITheme.TextPositive, 0.3f);
                case "InDevelopment":  return UITheme.WithAlpha(UITheme.TextWarning, 0.3f);
                case "Available":     return UITheme.BackgroundCard;
                case "Locked":        return UITheme.WithAlpha(UITheme.TextMuted, 0.2f);
                default:              return UITheme.BackgroundCard;
            }
        }

        private string GetStatusEmoji(ComponentData comp)
        {
            switch (comp.status)
            {
                case "Installed":      return "✅";
                case "InDevelopment":  return "🔧";
                case "Available":     return "📦";
                case "Locked":        return "🔒";
                default:              return "❓";
            }
        }

        private string GetAreaSpanish(string area)
        {
            switch (area)
            {
                case "Aerodynamics": return "🌀 Aerodinámica";
                case "Engine":       return "⚙️ Motor";
                case "Chassis":      return "🏗️ Chasis";
                case "Reliability":  return "🔧 Fiabilidad";
                default:             return area;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            UpdateCarRadar();
            UpdateGlobalPerformance();
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
