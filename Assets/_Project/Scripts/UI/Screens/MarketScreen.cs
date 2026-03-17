// ============================================================
// F1 Career Manager — MarketScreen.cs
// Pantalla de mercado — fichajes, staff, regens
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using F1CareerManager.Data;
using F1CareerManager.Market;

namespace F1CareerManager.UI.Screens
{
    /// <summary>
    /// Mercado: pilotos disponibles con filtros, mis pilotos,
    /// staff disponible, regens F2/F3.
    /// </summary>
    public class MarketScreen : MonoBehaviour
    {
        // ── Tabs ─────────────────────────────────────────────
        [Header("Tabs")]
        [SerializeField] private Button _tabPilots;
        [SerializeField] private Button _tabMyTeam;
        [SerializeField] private Button _tabStaff;
        [SerializeField] private Button _tabRegens;
        [SerializeField] private Image[] _tabIndicators;

        // ── Filtros ──────────────────────────────────────────
        [Header("Filtros")]
        [SerializeField] private Components.StarRating _filterStars;
        [SerializeField] private Slider _filterAgeMax;
        [SerializeField] private Text _filterAgeLabel;
        [SerializeField] private Slider _filterSalaryMax;
        [SerializeField] private Text _filterSalaryLabel;
        [SerializeField] private Button _filterApplyBtn;
        [SerializeField] private Button _filterResetBtn;

        // ── Lista ────────────────────────────────────────────
        [Header("Lista")]
        [SerializeField] private ScrollRect _listScroll;
        [SerializeField] private Transform _listContainer;
        [SerializeField] private Text _resultCountText;

        // ── Contrato popup ───────────────────────────────────
        [Header("Negociación")]
        [SerializeField] private Components.ContractPopup _contractPopup;

        // ── Estado ───────────────────────────────────────────
        private string _activeTab = "pilots";
        private TeamData _playerTeam;
        private List<PilotData> _allPilots;
        private List<StaffData> _allStaff;
        private NegotiationSystem _negoSystem;
        private int _minStars;
        private int _maxAge = 45;
        private float _maxSalary = 50f;

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ══════════════════════════════════════════════════════

        public void Initialize(TeamData team, List<PilotData> pilots,
            List<StaffData> staff, NegotiationSystem negoSys)
        {
            _playerTeam = team;
            _allPilots = pilots;
            _allStaff = staff;
            _negoSystem = negoSys;

            SetupTabs();
            SetupFilters();
            SetActiveTab("pilots");
        }

        private void SetupTabs()
        {
            if (_tabPilots != null)
            { _tabPilots.onClick.RemoveAllListeners(); _tabPilots.onClick.AddListener(() => SetActiveTab("pilots")); }
            if (_tabMyTeam != null)
            { _tabMyTeam.onClick.RemoveAllListeners(); _tabMyTeam.onClick.AddListener(() => SetActiveTab("myteam")); }
            if (_tabStaff != null)
            { _tabStaff.onClick.RemoveAllListeners(); _tabStaff.onClick.AddListener(() => SetActiveTab("staff")); }
            if (_tabRegens != null)
            { _tabRegens.onClick.RemoveAllListeners(); _tabRegens.onClick.AddListener(() => SetActiveTab("regens")); }
        }

        private void SetActiveTab(string tab)
        {
            _activeTab = tab;
            string[] tabs = { "pilots", "myteam", "staff", "regens" };
            if (_tabIndicators != null)
                for (int i = 0; i < _tabIndicators.Length && i < tabs.Length; i++)
                    _tabIndicators[i].color = tabs[i] == tab ? UITheme.AccentPrimary : Color.clear;

            switch (tab)
            {
                case "pilots": LoadPilots(); break;
                case "myteam": LoadMyTeam(); break;
                case "staff": LoadStaff(); break;
                case "regens": LoadRegens(); break;
            }
        }

        // ══════════════════════════════════════════════════════
        // FILTROS
        // ══════════════════════════════════════════════════════

        private void SetupFilters()
        {
            if (_filterStars != null)
                _filterStars.SetInteractive(true, s => _minStars = s);

            if (_filterAgeMax != null)
            {
                _filterAgeMax.minValue = 16; _filterAgeMax.maxValue = 45;
                _filterAgeMax.value = _maxAge;
                _filterAgeMax.onValueChanged.AddListener(v =>
                { _maxAge = (int)v; if (_filterAgeLabel != null) _filterAgeLabel.text = $"Edad máx: {_maxAge}"; });
            }

            if (_filterSalaryMax != null)
            {
                _filterSalaryMax.minValue = 0.5f; _filterSalaryMax.maxValue = 50f;
                _filterSalaryMax.value = _maxSalary;
                _filterSalaryMax.onValueChanged.AddListener(v =>
                { _maxSalary = v; if (_filterSalaryLabel != null) _filterSalaryLabel.text = $"Salario máx: ${_maxSalary:F1}M"; });
            }

            if (_filterApplyBtn != null)
            { _filterApplyBtn.onClick.RemoveAllListeners(); _filterApplyBtn.onClick.AddListener(LoadPilots); }

            if (_filterResetBtn != null)
            {
                _filterResetBtn.onClick.RemoveAllListeners();
                _filterResetBtn.onClick.AddListener(() =>
                {
                    _minStars = 0; _maxAge = 45; _maxSalary = 50f;
                    if (_filterStars != null) _filterStars.SetRating(0);
                    if (_filterAgeMax != null) _filterAgeMax.value = 45;
                    if (_filterSalaryMax != null) _filterSalaryMax.value = 50f;
                    LoadPilots();
                });
            }
        }

        // ══════════════════════════════════════════════════════
        // CARGA DE DATOS
        // ══════════════════════════════════════════════════════

        private void LoadPilots()
        {
            var filtered = _allPilots.FindAll(p =>
                p.currentTeamId != _playerTeam.id &&
                p.stars >= _minStars && p.age <= _maxAge &&
                p.salary <= _maxSalary && !p.isRetired);
            filtered.Sort((a, b) => b.overallRating.CompareTo(a.overallRating));
            PopulatePilotList(filtered, true);
        }

        private void LoadMyTeam()
        {
            var my = _allPilots.FindAll(p => p.currentTeamId == _playerTeam.id);
            PopulatePilotList(my, false);
        }

        private void LoadRegens()
        {
            var regens = _allPilots.FindAll(p => p.isRegen && p.age <= 22 && !p.isRetired);
            regens.Sort((a, b) => b.potential.CompareTo(a.potential));
            PopulatePilotList(regens, true);
        }

        private void LoadStaff()
        {
            ClearList();
            var available = _allStaff.FindAll(s => s.isAvailable && !s.isRetired);
            available.Sort((a, b) => b.stars.CompareTo(a.stars));

            foreach (var s in available)
                CreateStaffRow(s);

            SetResultCount(available.Count, "staff");
        }

        // ══════════════════════════════════════════════════════
        // CREACIÓN DE UI
        // ══════════════════════════════════════════════════════

        private void PopulatePilotList(List<PilotData> pilots, bool showNego)
        {
            ClearList();
            foreach (var p in pilots)
                CreatePilotRow(p, showNego);
            SetResultCount(pilots.Count, "pilotos");
        }

        private void ClearList()
        {
            if (_listContainer == null) return;
            foreach (Transform c in _listContainer) Destroy(c.gameObject);
        }

        private void SetResultCount(int count, string label)
        {
            if (_resultCountText != null)
            {
                _resultCountText.text = $"{count} {label}";
                _resultCountText.color = UITheme.TextSecondary;
            }
        }

        private void CreatePilotRow(PilotData pilot, bool showNego)
        {
            if (_listContainer == null) return;

            GameObject row = new GameObject($"P_{pilot.id}", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_listContainer, false);
            row.GetComponent<Image>().color = UITheme.BackgroundCard;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, UITheme.PILOT_CARD_HEIGHT);

            // Añadir PilotCard
            var card = row.AddComponent<Components.PilotCard>();
            card.Setup(pilot, pilot.currentTeamId);

            // Botón negociar
            if (showNego)
            {
                GameObject btn = new GameObject("Nego", typeof(RectTransform), typeof(Image), typeof(Button));
                btn.transform.SetParent(row.transform, false);
                btn.GetComponent<Image>().color = UITheme.AccentPrimary;
                var rt = btn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.82f, 0.3f); rt.anchorMax = new Vector2(0.98f, 0.7f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                var captured = pilot;
                btn.GetComponent<Button>().onClick.AddListener(() => OpenNegotiation(captured));

                GameObject txt = new GameObject("T", typeof(Text));
                txt.transform.SetParent(btn.transform, false);
                var t = txt.GetComponent<Text>();
                t.text = "FICHAR"; t.color = UITheme.TextPrimary;
                t.fontSize = UITheme.FONT_SIZE_XS; t.alignment = TextAnchor.MiddleCenter;
                var trt = txt.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = Vector2.zero;
            }
        }

        private void CreateStaffRow(StaffData staff)
        {
            if (_listContainer == null) return;

            GameObject row = new GameObject($"S_{staff.id}", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_listContainer, false);
            row.GetComponent<Image>().color = UITheme.BackgroundCard;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 70f);

            AddText(row, $"{staff.firstName} {staff.lastName}", UITheme.TextPrimary,
                UITheme.FONT_SIZE_MD, 0.02f, 0.5f, 0.55f, 1f, TextAnchor.MiddleLeft);
            AddText(row, $"{staff.role} | {new string('⭐', staff.stars)}", UITheme.TextSecondary,
                UITheme.FONT_SIZE_SM, 0.02f, 0.5f, 0.05f, 0.5f, TextAnchor.MiddleLeft);
            AddText(row, $"${staff.salary:F1}M", UITheme.TextSecondary,
                UITheme.FONT_SIZE_SM, 0.55f, 0.78f, 0.3f, 0.7f, TextAnchor.MiddleRight);
        }

        private void AddText(GameObject parent, string text, Color color, int size,
            float xMin, float xMax, float yMin, float yMax, TextAnchor align)
        {
            var obj = new GameObject("T", typeof(Text));
            obj.transform.SetParent(parent.transform, false);
            var t = obj.GetComponent<Text>();
            t.text = text; t.color = color; t.fontSize = size; t.alignment = align;
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin); rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = new Vector2(4, 0); rt.offsetMax = new Vector2(-4, 0);
        }

        // ══════════════════════════════════════════════════════
        // NEGOCIACIÓN
        // ══════════════════════════════════════════════════════

        private void OpenNegotiation(PilotData pilot)
        {
            if (_contractPopup != null && _negoSystem != null)
            {
                _contractPopup.Open(pilot, _playerTeam, _negoSystem, success =>
                {
                    if (success) { LoadPilots(); Components.NotificationToast.ShowSuccess($"¡{pilot.lastName} fichado!"); }
                });
            }
        }

        public void Show() { gameObject.SetActive(true); SetActiveTab(_activeTab); }
        public void Hide() => gameObject.SetActive(false);
    }
}
