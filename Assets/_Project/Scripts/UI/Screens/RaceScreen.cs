// ============================================================
// F1 Career Manager — RaceScreen.cs
// Pantalla de carrera en vivo — simulación con narración
// ============================================================
// PREFAB: RaceScreen_Prefab
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
    /// Vista de carrera en vivo.
    /// - Circuito pixel art (top-down simplificado)
    /// - Posiciones en tiempo real (20 pilotos)
    /// - Botones de estrategia: Pit Stop, Motor, Orden
    /// - Feed de eventos (narración IA)
    /// - Vista rápida vs narrada
    /// - Clima actual
    /// </summary>
    public class RaceScreen : MonoBehaviour
    {
        // ── Referencias: Circuito ────────────────────────────
        [Header("Circuito")]
        [SerializeField] private Image _circuitImage;
        [SerializeField] private Text _circuitNameText;
        [SerializeField] private Text _lapCounterText;

        // ── Referencias: Posiciones ──────────────────────────
        [Header("Posiciones")]
        [SerializeField] private ScrollRect _positionsScroll;
        [SerializeField] private Transform _positionsContainer;

        // ── Referencias: Estrategia ──────────────────────────
        [Header("Estrategia")]
        [SerializeField] private Button _pitStopBtn;
        [SerializeField] private Text _pitStopBtnText;
        [SerializeField] private Button _engineModeBtn;
        [SerializeField] private Text _engineModeBtnText;
        [SerializeField] private Button _teamOrderBtn;
        [SerializeField] private Text _teamOrderBtnText;

        // ── Referencias: Narración ───────────────────────────
        [Header("Narración")]
        [SerializeField] private ScrollRect _narrationScroll;
        [SerializeField] private Transform _narrationContainer;

        // ── Referencias: Clima ───────────────────────────────
        [Header("Clima")]
        [SerializeField] private Text _weatherText;
        [SerializeField] private Text _weatherChangeText;
        [SerializeField] private Image _weatherIcon;

        // ── Referencias: Toggle vista ────────────────────────
        [Header("Vista")]
        [SerializeField] private Button _toggleViewBtn;
        [SerializeField] private Text _toggleViewText;

        // ── Estado ───────────────────────────────────────────
        private CircuitData _circuit;
        private int _currentLap;
        private int _totalLaps;
        private bool _isNarratedView = true;
        private string _engineMode = "Normal";
        private List<RacePositionEntry> _positions = new List<RacePositionEntry>();
        private List<string> _narrationLog = new List<string>();

        // ── Callbacks ────────────────────────────────────────
        private System.Action _onPitStop;
        private System.Action<string> _onEngineMode;
        private System.Action _onTeamOrder;

        // ══════════════════════════════════════════════════════
        // DATOS
        // ══════════════════════════════════════════════════════

        private class RacePositionEntry
        {
            public int Position;
            public string PilotName;
            public string TeamId;
            public string Gap;
            public bool IsPlayer;
            public bool IsDNF;
            public string TireType;
            public int PitStops;
        }

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ══════════════════════════════════════════════════════

        public void Initialize(CircuitData circuit, int totalLaps,
            List<PilotData> allPilots, string playerTeamId,
            System.Action onPit, System.Action<string> onEngine,
            System.Action onOrder)
        {
            _circuit = circuit;
            _totalLaps = totalLaps;
            _currentLap = 0;
            _onPitStop = onPit;
            _onEngineMode = onEngine;
            _onTeamOrder = onOrder;

            SetupCircuit();
            SetupPositions(allPilots, playerTeamId);
            SetupStrategyButtons();
            SetupWeather();
            SetupViewToggle();
        }

        // ══════════════════════════════════════════════════════
        // SETUP
        // ══════════════════════════════════════════════════════

        private void SetupCircuit()
        {
            if (_circuitNameText != null)
            {
                _circuitNameText.text = $"🏁 {_circuit.name}";
                _circuitNameText.color = UITheme.TextPrimary;
                _circuitNameText.fontSize = UITheme.FONT_SIZE_LG;
            }

            UpdateLapCounter();
        }

        private void SetupPositions(List<PilotData> pilots, string playerTeamId)
        {
            _positions.Clear();

            for (int i = 0; i < pilots.Count; i++)
            {
                _positions.Add(new RacePositionEntry
                {
                    Position = i + 1,
                    PilotName = $"{pilots[i].firstName[0]}. {pilots[i].lastName}",
                    TeamId = pilots[i].currentTeamId ?? "",
                    Gap = i == 0 ? "LÍDER" : $"+{(i * 1.2f):F1}s",
                    IsPlayer = pilots[i].currentTeamId == playerTeamId,
                    IsDNF = false,
                    TireType = "M",
                    PitStops = 0
                });
            }

            RefreshPositionsList();
        }

        private void SetupStrategyButtons()
        {
            // Pit Stop
            if (_pitStopBtn != null)
            {
                _pitStopBtn.onClick.RemoveAllListeners();
                _pitStopBtn.onClick.AddListener(OnPitStopClicked);

                Image bg = _pitStopBtn.GetComponent<Image>();
                if (bg != null) bg.color = UITheme.AccentPrimary;
            }
            if (_pitStopBtnText != null) _pitStopBtnText.text = "🔧 PIT STOP";

            // Engine Mode
            if (_engineModeBtn != null)
            {
                _engineModeBtn.onClick.RemoveAllListeners();
                _engineModeBtn.onClick.AddListener(OnEngineModeClicked);

                Image bg = _engineModeBtn.GetComponent<Image>();
                if (bg != null) bg.color = UITheme.AccentTertiary;
            }
            if (_engineModeBtnText != null)
                _engineModeBtnText.text = $"⚡ {_engineMode}";

            // Team Order
            if (_teamOrderBtn != null)
            {
                _teamOrderBtn.onClick.RemoveAllListeners();
                _teamOrderBtn.onClick.AddListener(OnTeamOrderClicked);

                Image bg = _teamOrderBtn.GetComponent<Image>();
                if (bg != null) bg.color = UITheme.AccentSecondary;
            }
            if (_teamOrderBtnText != null)
                _teamOrderBtnText.text = "📢 ORDEN EQUIPO";
        }

        private void SetupWeather()
        {
            if (_weatherText != null)
            {
                _weatherText.text = "☀️ Seco";
                _weatherText.color = UITheme.TextPrimary;
            }

            if (_weatherChangeText != null)
            {
                _weatherChangeText.text = "Prob. lluvia: 10%";
                _weatherChangeText.color = UITheme.TextSecondary;
                _weatherChangeText.fontSize = UITheme.FONT_SIZE_XS;
            }
        }

        private void SetupViewToggle()
        {
            if (_toggleViewBtn != null)
            {
                _toggleViewBtn.onClick.RemoveAllListeners();
                _toggleViewBtn.onClick.AddListener(ToggleView);
            }

            UpdateViewToggleText();
        }

        // ══════════════════════════════════════════════════════
        // ACTUALIZACIÓN EN VIVO
        // ══════════════════════════════════════════════════════

        /// <summary>Actualiza posiciones para una vuelta nueva</summary>
        public void UpdateLap(int lap, List<EventBus.RacePositionInfo> positions,
            string narrationText, string weather, float rainChance)
        {
            _currentLap = lap;
            UpdateLapCounter();

            // Actualizar posiciones
            for (int i = 0; i < positions.Count && i < _positions.Count; i++)
            {
                var pos = positions[i];
                _positions[i].Position = pos.Position;
                _positions[i].Gap = pos.Position == 1 ? "LÍDER" :
                    $"+{((pos.Position - 1) * 0.8f):F1}s";
                _positions[i].IsDNF = pos.DNF;
            }

            RefreshPositionsList();

            // Agregar narración
            if (!string.IsNullOrEmpty(narrationText))
                AddNarration(narrationText);

            // Clima
            UpdateWeather(weather, rainChance);
        }

        /// <summary>Agrega un evento de carrera narrado</summary>
        public void AddNarration(string text)
        {
            _narrationLog.Add(text);

            if (_narrationContainer != null)
            {
                GameObject entry = new GameObject("Narration",
                    typeof(RectTransform), typeof(Text));
                entry.transform.SetParent(_narrationContainer, false);

                Text entryText = entry.GetComponent<Text>();
                entryText.text = $"[V{_currentLap}] {text}";
                entryText.color = UITheme.TextSecondary;
                entryText.fontSize = UITheme.FONT_SIZE_SM;

                RectTransform rt = entry.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(280f, 30f);

                // Auto-scroll al final
                if (_narrationScroll != null)
                    StartCoroutine(ScrollToBottom());
            }
        }

        private void UpdateLapCounter()
        {
            if (_lapCounterText != null)
            {
                _lapCounterText.text = $"Vuelta {_currentLap} / {_totalLaps}";
                _lapCounterText.color = _currentLap >= _totalLaps - 3
                    ? UITheme.AccentPrimary : UITheme.TextPrimary;
                _lapCounterText.fontSize = UITheme.FONT_SIZE_LG;
            }
        }

        private void UpdateWeather(string weather, float rainChance)
        {
            if (_weatherText != null)
            {
                string icon = weather == "Wet" ? "🌧️" :
                    weather == "Damp" ? "🌥️" : "☀️";
                _weatherText.text = $"{icon} {weather}";
            }

            if (_weatherChangeText != null)
            {
                _weatherChangeText.text = $"Prob. lluvia: {rainChance * 100:F0}%";
                _weatherChangeText.color = rainChance > 0.5f
                    ? UITheme.TextWarning : UITheme.TextSecondary;
            }
        }

        // ══════════════════════════════════════════════════════
        // LISTA DE POSICIONES
        // ══════════════════════════════════════════════════════

        private void RefreshPositionsList()
        {
            if (_positionsContainer == null) return;

            foreach (Transform child in _positionsContainer)
                Destroy(child.gameObject);

            _positions.Sort((a, b) => a.Position.CompareTo(b.Position));

            foreach (var entry in _positions)
            {
                CreatePositionRow(entry);
            }
        }

        private void CreatePositionRow(RacePositionEntry entry)
        {
            GameObject row = new GameObject($"P{entry.Position}",
                typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_positionsContainer, false);

            Image bg = row.GetComponent<Image>();
            bg.color = entry.IsPlayer
                ? UITheme.WithAlpha(UITheme.AccentPrimary, 0.15f)
                : UITheme.BackgroundCard;

            RectTransform rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250f, 28f);

            // Posición
            CreateRowText(row, $"P{entry.Position}", UITheme.TextPrimary,
                0f, 0.15f, TextAnchor.MiddleRight);

            // Barra de color del equipo
            GameObject colorBar = new GameObject("TeamColor",
                typeof(RectTransform), typeof(Image));
            colorBar.transform.SetParent(row.transform, false);
            Image barImg = colorBar.GetComponent<Image>();
            barImg.color = UITheme.GetTeamColor(entry.TeamId);
            RectTransform barRt = colorBar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.17f, 0.2f);
            barRt.anchorMax = new Vector2(0.18f, 0.8f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;

            // Nombre piloto
            Color nameColor = entry.IsDNF
                ? UITheme.TextMuted : UITheme.TextPrimary;
            CreateRowText(row, entry.PilotName, nameColor,
                0.20f, 0.65f, TextAnchor.MiddleLeft);

            // Gap
            CreateRowText(row, entry.IsDNF ? "DNF" : entry.Gap,
                entry.IsDNF ? UITheme.TextNegative : UITheme.TextSecondary,
                0.65f, 0.85f, TextAnchor.MiddleRight);

            // Neumático
            CreateRowText(row, entry.TireType,
                GetTireColor(entry.TireType),
                0.87f, 1f, TextAnchor.MiddleCenter);
        }

        private void CreateRowText(GameObject parent, string text, Color color,
            float anchorMinX, float anchorMaxX, TextAnchor alignment)
        {
            GameObject textObj = new GameObject("Text", typeof(Text));
            textObj.transform.SetParent(parent.transform, false);
            Text t = textObj.GetComponent<Text>();
            t.text = text;
            t.color = color;
            t.fontSize = UITheme.FONT_SIZE_XS;
            t.alignment = alignment;
            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, 0);
            rt.anchorMax = new Vector2(anchorMaxX, 1);
            rt.offsetMin = new Vector2(2, 0);
            rt.offsetMax = new Vector2(-2, 0);
        }

        private Color GetTireColor(string tireType)
        {
            switch (tireType)
            {
                case "S": return UITheme.TextNegative;    // Rojo
                case "M": return UITheme.TextWarning;     // Amarillo
                case "H": return UITheme.TextPrimary;     // Blanco
                case "I": return UITheme.AccentTertiary;  // Azul (intermedio)
                case "W": return UITheme.AccentTertiary;  // Azul (lluvia)
                default:  return UITheme.TextSecondary;
            }
        }

        // ══════════════════════════════════════════════════════
        // ESTRATEGIA
        // ══════════════════════════════════════════════════════

        private void OnPitStopClicked()
        {
            _onPitStop?.Invoke();
            Components.NotificationToast.ShowInfo("🔧 Pit stop ordenado");
        }

        private void OnEngineModeClicked()
        {
            // Ciclar: Normal → Push → Conserve → Normal
            switch (_engineMode)
            {
                case "Normal":  _engineMode = "Push"; break;
                case "Push":    _engineMode = "Conserve"; break;
                default:        _engineMode = "Normal"; break;
            }

            if (_engineModeBtnText != null)
                _engineModeBtnText.text = $"⚡ {_engineMode}";

            _onEngineMode?.Invoke(_engineMode);
        }

        private void OnTeamOrderClicked()
        {
            _onTeamOrder?.Invoke();
            Components.NotificationToast.ShowInfo(
                "📢 Orden de equipo enviada");
        }

        private void ToggleView()
        {
            _isNarratedView = !_isNarratedView;
            UpdateViewToggleText();
        }

        private void UpdateViewToggleText()
        {
            if (_toggleViewText != null)
                _toggleViewText.text = _isNarratedView
                    ? "🐢 Vista narrada" : "⚡ Vista rápida";
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            if (_narrationScroll != null)
                _narrationScroll.normalizedPosition = Vector2.zero;
        }

        // ══════════════════════════════════════════════════════
        // SHOW / HIDE
        // ══════════════════════════════════════════════════════

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
