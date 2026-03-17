// ============================================================
// F1 Career Manager — UIManager.cs
// Singleton controlador de navegación y popups
// ============================================================
// DEPENDENCIAS: EventBus, UITheme, todas las Screens,
//               ScreenTransition, NotificationToast
// ============================================================

using UnityEngine;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.UI.Screens;
using F1CareerManager.UI.Components;
using F1CareerManager.UI.Animations;

namespace F1CareerManager.UI
{
    /// <summary>
    /// Tipos de pantalla
    /// </summary>
    public enum ScreenType
    {
        Hub, Pilots, RnD, Race, Results, Market, Finance, Staff
    }

    /// <summary>
    /// Tipos de popup
    /// </summary>
    public enum PopupType
    {
        Contract, PressConference, RandomEvent, SeasonEnd, Settings
    }

    /// <summary>
    /// Controlador central de UI.
    /// Stack de navegación, transiciones, toasts automáticos.
    /// Escucha EventBus para mostrar notificaciones.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static UIManager Instance { get; private set; }

        // ── Referencias: Pantallas ───────────────────────────
        [Header("Pantallas")]
        [SerializeField] private HubScreen _hubScreen;
        [SerializeField] private PilotScreen _pilotScreen;
        [SerializeField] private RnDScreen _rndScreen;
        [SerializeField] private RaceScreen _raceScreen;
        [SerializeField] private ResultsScreen _resultsScreen;
        [SerializeField] private MarketScreen _marketScreen;
        [SerializeField] private FinanceScreen _financeScreen;

        // ── Referencias: Popups ──────────────────────────────
        [Header("Popups")]
        [SerializeField] private ContractPopup _contractPopup;
        [SerializeField] private GameObject _pressConfPopup;
        [SerializeField] private GameObject _randomEventPopup;
        [SerializeField] private GameObject _seasonEndPopup;
        [SerializeField] private GameObject _settingsPopup;

        // ── Referencias: Transición ──────────────────────────
        [Header("Sistema")]
        [SerializeField] private ScreenTransition _transition;

        // ── Estado ───────────────────────────────────────────
        private Stack<ScreenType> _navStack;
        private ScreenType _currentScreen;
        private Dictionary<ScreenType, GameObject> _screenMap;
        private Dictionary<PopupType, GameObject> _popupMap;

        // ══════════════════════════════════════════════════════
        // LIFECYCLE
        // ══════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _navStack = new Stack<ScreenType>();
            BuildMaps();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        // ══════════════════════════════════════════════════════
        // SETUP
        // ══════════════════════════════════════════════════════

        private void BuildMaps()
        {
            _screenMap = new Dictionary<ScreenType, GameObject>();

            if (_hubScreen != null) _screenMap[ScreenType.Hub] = _hubScreen.gameObject;
            if (_pilotScreen != null) _screenMap[ScreenType.Pilots] = _pilotScreen.gameObject;
            if (_rndScreen != null) _screenMap[ScreenType.RnD] = _rndScreen.gameObject;
            if (_raceScreen != null) _screenMap[ScreenType.Race] = _raceScreen.gameObject;
            if (_resultsScreen != null) _screenMap[ScreenType.Results] = _resultsScreen.gameObject;
            if (_marketScreen != null) _screenMap[ScreenType.Market] = _marketScreen.gameObject;
            if (_financeScreen != null) _screenMap[ScreenType.Finance] = _financeScreen.gameObject;

            _popupMap = new Dictionary<PopupType, GameObject>();

            if (_contractPopup != null) _popupMap[PopupType.Contract] = _contractPopup.gameObject;
            if (_pressConfPopup != null) _popupMap[PopupType.PressConference] = _pressConfPopup;
            if (_randomEventPopup != null) _popupMap[PopupType.RandomEvent] = _randomEventPopup;
            if (_seasonEndPopup != null) _popupMap[PopupType.SeasonEnd] = _seasonEndPopup;
            if (_settingsPopup != null) _popupMap[PopupType.Settings] = _settingsPopup;
        }

        // ══════════════════════════════════════════════════════
        // NAVEGACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>Navega a una pantalla. Agrega al stack.</summary>
        public void NavigateTo(ScreenType screen)
        {
            if (_transition != null && _transition.IsTransitioning) return;

            GameObject fromObj = GetCurrentScreenObj();
            GameObject toObj = GetScreenObj(screen);

            if (toObj == null)
            {
                Debug.LogWarning($"[UIManager] Pantalla no asignada: {screen}");
                return;
            }

            // Push al stack
            _navStack.Push(_currentScreen);
            _currentScreen = screen;

            // Determinar tipo de transición
            var transType = GetTransitionType(_currentScreen, screen);

            if (_transition != null && fromObj != null)
            {
                _transition.Transition(fromObj, toObj, transType);
            }
            else
            {
                // Sin transición
                if (fromObj != null) fromObj.SetActive(false);
                toObj.SetActive(true);
            }

            // Actualizar NavBar del hub
            if (_hubScreen != null)
            {
                var nav = _hubScreen.GetComponentInChildren<NavigationBar>();
                if (nav != null)
                    nav.SetActiveSection(ScreenTypeToNavId(screen));
            }
        }

        /// <summary>Vuelve a la pantalla anterior</summary>
        public void GoBack()
        {
            if (_navStack.Count == 0)
            {
                NavigateTo(ScreenType.Hub);
                return;
            }

            var previousScreen = _navStack.Pop();
            GameObject fromObj = GetCurrentScreenObj();
            GameObject toObj = GetScreenObj(previousScreen);

            _currentScreen = previousScreen;

            if (_transition != null && fromObj != null && toObj != null)
            {
                _transition.Transition(fromObj, toObj,
                    ScreenTransition.TransitionType.SlideRight);
            }
            else
            {
                if (fromObj != null) fromObj.SetActive(false);
                if (toObj != null) toObj.SetActive(true);
            }
        }

        /// <summary>Navega al hub limpiando el stack</summary>
        public void GoHome()
        {
            _navStack.Clear();

            GameObject fromObj = GetCurrentScreenObj();
            _currentScreen = ScreenType.Hub;

            if (fromObj != null) fromObj.SetActive(false);
            if (_hubScreen != null) _hubScreen.Show();
        }

        // ══════════════════════════════════════════════════════
        // POPUPS
        // ══════════════════════════════════════════════════════

        /// <summary>Muestra un popup</summary>
        public void ShowPopup(PopupType popup)
        {
            if (_popupMap.TryGetValue(popup, out GameObject obj))
                obj.SetActive(true);
        }

        /// <summary>Oculta un popup</summary>
        public void HidePopup(PopupType popup)
        {
            if (_popupMap.TryGetValue(popup, out GameObject obj))
                obj.SetActive(false);
        }

        /// <summary>Oculta todos los popups</summary>
        public void HideAllPopups()
        {
            foreach (var kvp in _popupMap)
                if (kvp.Value != null) kvp.Value.SetActive(false);
        }

        // ══════════════════════════════════════════════════════
        // EVENTBUS → TOASTS AUTOMÁTICOS
        // ══════════════════════════════════════════════════════

        private void SubscribeToEvents()
        {
            var eb = EventBus.Instance;
            eb.OnBudgetChanged += HandleBudgetToast;
            eb.OnPilotMoodChanged += HandleMoodToast;
            eb.OnFIAInvestigation += HandleFIAToast;
            eb.OnInjuryOccurred += HandleInjuryToast;
            eb.OnContractSigned += HandleContractToast;
            eb.OnRivalTransfer += HandleTransferToast;
            eb.OnStaffChanged += HandleStaffToast;
            eb.OnComponentInstalled += HandleComponentToast;
        }

        private void UnsubscribeFromEvents()
        {
            var eb = EventBus.Instance;
            eb.OnBudgetChanged -= HandleBudgetToast;
            eb.OnPilotMoodChanged -= HandleMoodToast;
            eb.OnFIAInvestigation -= HandleFIAToast;
            eb.OnInjuryOccurred -= HandleInjuryToast;
            eb.OnContractSigned -= HandleContractToast;
            eb.OnRivalTransfer -= HandleTransferToast;
            eb.OnStaffChanged -= HandleStaffToast;
            eb.OnComponentInstalled -= HandleComponentToast;
        }

        // ── Handlers ─────────────────────────────────────────

        private void HandleBudgetToast(object s, EventBus.BudgetChangedArgs a)
        {
            if (a.FinancialStatus == "Crisis")
                NotificationToast.ShowError($"💰 ¡CRISIS! Presupuesto: ${a.NewBudget:F1}M");
            else if (a.FinancialStatus == "Struggling")
                NotificationToast.ShowWarning($"💰 Presupuesto bajo: ${a.NewBudget:F1}M");

            // Badge en finanzas
            if (_hubScreen != null)
            {
                var nav = _hubScreen.GetComponentInChildren<NavigationBar>();
                if (nav != null && a.FinancialStatus == "Crisis")
                    nav.SetBadge("finance", 1);
            }
        }

        private void HandleMoodToast(object s, EventBus.PilotMoodChangedArgs a)
        {
            if (a.NewMood == "WantsOut")
                NotificationToast.ShowError(
                    $"😡 {a.PilotName} quiere irse del equipo");
            else if (a.NewMood == "Furious")
                NotificationToast.ShowWarning(
                    $"😤 {a.PilotName} está furioso: {a.Reason}");

            // Badge en pilotos
            if (a.NewMood == "WantsOut" || a.NewMood == "Furious")
            {
                var nav = _hubScreen?.GetComponentInChildren<NavigationBar>();
                nav?.SetBadge("pilots", 1);
            }
        }

        private void HandleFIAToast(object s, EventBus.FIAInvestigationArgs a)
        {
            if (a.WasDetected)
                NotificationToast.ShowError(
                    $"🚨 FIA: {a.Description} — Multa: ${a.FineAmount:F1}M");
            else
                NotificationToast.ShowWarning(
                    $"⚠️ Investigación FIA: {a.InvestigationReason}");
        }

        private void HandleInjuryToast(object s, EventBus.InjuryOccurredArgs a)
        {
            NotificationToast.ShowError(
                $"🏥 {a.PilotName} lesionado: {a.Description} ({a.RacesOut} carreras)");

            var nav = _hubScreen?.GetComponentInChildren<NavigationBar>();
            nav?.SetBadge("pilots", 1);
        }

        private void HandleContractToast(object s, EventBus.ContractSignedArgs a)
        {
            if (a.IsRenewal)
                NotificationToast.ShowSuccess(
                    $"📝 {a.PilotName} renovó con {a.TeamName} ({a.Years} años)");
            else
                NotificationToast.ShowSuccess(
                    $"🤝 {a.PilotName} ficha por {a.TeamName}");
        }

        private void HandleTransferToast(object s, EventBus.RivalTransferArgs a)
        {
            NotificationToast.ShowInfo(
                $"📰 {a.PilotName} ficha por {a.ToTeamName} (${a.Salary:F1}M)");
        }

        private void HandleStaffToast(object s, EventBus.StaffChangedArgs a)
        {
            switch (a.ChangeType)
            {
                case "Hired":
                    NotificationToast.ShowSuccess(
                        $"👔 {a.StaffName} contratado como {a.Role}");
                    break;
                case "Stolen":
                    NotificationToast.ShowWarning(
                        $"👔 {a.StaffName} robado por {a.TeamId}");
                    break;
                case "Retired":
                    NotificationToast.ShowInfo(
                        $"👔 {a.StaffName} se retira");
                    break;
            }
        }

        private void HandleComponentToast(object s, EventBus.ComponentInstalledArgs a)
        {
            switch (a.InstallResult)
            {
                case "BetterThanExpected":
                    NotificationToast.ShowSuccess(
                        $"🏗️ {a.ComponentName}: ¡Mejor de lo esperado! +{a.ActualPerformance}");
                    break;
                case "Failed":
                    NotificationToast.ShowError(
                        $"🏗️ {a.ComponentName}: FALLÓ — daños al auto");
                    break;
                default:
                    NotificationToast.ShowInfo(
                        $"🏗️ {a.ComponentName} instalado: +{a.ActualPerformance}");
                    break;
            }
        }

        // ══════════════════════════════════════════════════════
        // NEWS FEED
        // ══════════════════════════════════════════════════════

        /// <summary>Agrega una noticia al feed del hub</summary>
        public void ShowNewsItem(Data.NewsData newsData)
        {
            if (_hubScreen == null) return;

            var feed = _hubScreen.GetComponentInChildren<NewsFeed>();
            if (feed != null)
            {
                // El NewsFeed ya escucha EventBus.OnNewsGenerated
                // Este método es para agregar noticias manuales
                Debug.Log($"[UIManager] Noticia agregada: {newsData?.headline}");
            }
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private GameObject GetCurrentScreenObj()
        {
            return GetScreenObj(_currentScreen);
        }

        private GameObject GetScreenObj(ScreenType screen)
        {
            if (_screenMap.TryGetValue(screen, out GameObject obj))
                return obj;
            return null;
        }

        private string ScreenTypeToNavId(ScreenType screen)
        {
            switch (screen)
            {
                case ScreenType.Hub: return "hub";
                case ScreenType.Pilots: return "pilots";
                case ScreenType.RnD: return "rnd";
                case ScreenType.Race: return "race";
                case ScreenType.Results: return "race";
                case ScreenType.Market: return "market";
                case ScreenType.Finance: return "finance";
                case ScreenType.Staff: return "staff";
                default: return "hub";
            }
        }

        private ScreenTransition.TransitionType GetTransitionType(
            ScreenType from, ScreenType to)
        {
            // Race y Results usan slide up (más dramatic)
            if (to == ScreenType.Race || to == ScreenType.Results)
                return ScreenTransition.TransitionType.SlideUp;

            // Volver al hub = fade
            if (to == ScreenType.Hub)
                return ScreenTransition.TransitionType.Fade;

            // Default = slide left
            return ScreenTransition.TransitionType.SlideLeft;
        }

        /// <summary>Pantalla actual</summary>
        public ScreenType CurrentScreen => _currentScreen;

        /// <summary>¿Puede volver?</summary>
        public bool CanGoBack => _navStack.Count > 0;
    }
}
