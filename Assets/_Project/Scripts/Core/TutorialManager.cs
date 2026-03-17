// ============================================================
// F1 Career Manager — TutorialManager.cs
// Gestor de tutoriales interactivos y tips contextuales
// Utiliza tips.json via DataLoader
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using F1CareerManager.Core;

namespace F1CareerManager.Core
{
    public class TutorialManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static TutorialManager Instance { get; private set; }

        // ── Datos ────────────────────────────────────────────
        private List<TipInfo> allTips = new List<TipInfo>();
        private HashSet<string> shownTips = new HashSet<string>();

        // ── Referencia UI ────────────────────────────────────
        [Header("UI Reference")]
        [SerializeField] private GameObject tipPopupPrefab;
        [SerializeField] private Canvas mainCanvas;

        // ── Modelo ───────────────────────────────────────────
        [Serializable]
        public class TipInfo
        {
            public string id;
            public string screen;   // HUB, PILOTS, RND, RACE, MARKET, FINANCE
            public string trigger;  // first_visit, budget_low, rain_forecast, etc.
            public string title;
            public string text;
            public bool showOnce;
        }

        [Serializable]
        private class TipListWrapper
        {
            public List<TipInfo> tips;
        }

        // ══════════════════════════════════════════════════════
        // UNITY LIFECYCLE
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
        }

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ══════════════════════════════════════════════════════

        public void Initialize()
        {
            LoadTips();
            LoadShownState();
            Debug.Log($"[TutorialManager] ✅ Inicializado — {allTips.Count} tips cargados");
        }

        private void LoadTips()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("Data/tips");
            if (jsonFile == null)
            {
                Debug.LogWarning("[TutorialManager] ✗ tips.json no encontrado");
                return;
            }

            try
            {
                TipListWrapper wrapper = JsonUtility.FromJson<TipListWrapper>(jsonFile.text);
                if (wrapper != null && wrapper.tips != null)
                {
                    allTips = wrapper.tips;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TutorialManager] ✗ Error parseando tips.json: {e.Message}");
            }
        }

        private void LoadShownState()
        {
            // Cargar los tips mostrados desde PlayerPrefs o SaveManager
            string saved = PlayerPrefs.GetString("F1_ShownTips", "");
            if (!string.IsNullOrEmpty(saved))
            {
                string[] ids = saved.Split('|');
                shownTips = new HashSet<string>(ids);
            }
        }

        private void SaveShownState()
        {
            string saved = string.Join("|", shownTips);
            PlayerPrefs.SetString("F1_ShownTips", saved);
            PlayerPrefs.Save();
        }

        // ══════════════════════════════════════════════════════
        // LÓGICA DE ACTIVACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Intenta mostrar un tip para una pantalla y trigger específicos
        /// </summary>
        public void CheckTip(string screen, string trigger)
        {
            var tip = allTips.FirstOrDefault(t => t.screen == screen && t.trigger == trigger);
            
            if (tip == null) return;
            if (tip.showOnce && shownTips.Contains(tip.id)) return;

            ShowTip(tip);
        }

        /// <summary>
        /// Muestra un tip aleatorio de los que aún no se han visto forzada por el juego
        /// </summary>
        public void ShowRandomTip(string screen)
        {
            var candidates = allTips
                .Where(t => t.screen == screen && !shownTips.Contains(t.id))
                .ToList();

            if (candidates.Count > 0)
            {
                ShowTip(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
            }
        }

        private void ShowTip(TipInfo tip)
        {
            Debug.Log($"[TutorialManager] Mostrando Tip: {tip.title}");
            
            // Si es de mostrar una vez, lo guardamos
            if (tip.showOnce)
            {
                shownTips.Add(tip.id);
                SaveShownState();
            }

            // Aquí se instancia el popup visual
            if (tipPopupPrefab != null && mainCanvas != null)
            {
                GameObject popup = Instantiate(tipPopupPrefab, mainCanvas.transform);
                // Configurar textos del popup
                // popup.GetComponent<TipPopup>().Setup(tip.title, tip.text);
            }
        }

        // ══════════════════════════════════════════════════════
        // API PÚBLICA
        // ══════════════════════════════════════════════════════

        public void ResetTutorials()
        {
            shownTips.Clear();
            SaveShownState();
            Debug.Log("[TutorialManager] Todos los tutoriales reseteados");
        }
    }
}
