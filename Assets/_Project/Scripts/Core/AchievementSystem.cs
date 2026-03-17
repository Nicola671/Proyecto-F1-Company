// ============================================================
// F1 Career Manager — AchievementSystem.cs
// Gestiona logros desbloqueables, escucha eventos del EventBus,
// y coordina con LegacyTracker y BudgetManager para premios.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using F1CareerManager.Core;
using F1CareerManager.Data;
using F1CareerManager.AI.EconomyAI;

namespace F1CareerManager.Core
{
    /// <summary>
    /// Sistema central de logros. Carga achievements.json al inicio,
    /// escucha eventos del juego, desbloquea logros cuando se cumplen
    /// las condiciones, y notifica a UI, Legacy y Economía.
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static AchievementSystem Instance { get; private set; }

        // ── Datos ────────────────────────────────────────────
        private List<AchievementData> allAchievements = new List<AchievementData>();
        private HashSet<string> unlockedIds = new HashSet<string>();
        private Dictionary<string, AchievementProgress> progressMap = new Dictionary<string, AchievementProgress>();

        // ── Eventos propios ──────────────────────────────────
        /// <summary>Se dispara cuando un logro es desbloqueado</summary>
        public event Action<AchievementData> OnAchievementUnlocked;

        // ══════════════════════════════════════════════════════
        // MODELOS INTERNOS
        // ══════════════════════════════════════════════════════

        [Serializable]
        public class AchievementData
        {
            public string id;
            public string title;
            public string description;
            public string icon;
            public string category;    // RACING, MANAGEMENT, FINANCIAL, LEGACY, SECRET
            public string rarity;      // COMMON, UNCOMMON, RARE, EPIC, LEGENDARY
            public bool secret;
            public AchievementCondition condition;
            public AchievementReward reward;
            public string flavorText;
        }

        [Serializable]
        public class AchievementCondition
        {
            public string type;        // race_wins, podiums, season_no_dnf, etc.
            public string stringValue; // Para circuitos específicos, rangos, etc.
            public int intValue;       // Para contadores numéricos
        }

        [Serializable]
        public class AchievementReward
        {
            public int legacyPoints;
            public float budgetBonus; // En millones
        }

        [Serializable]
        public class AchievementProgress
        {
            public string achievementId;
            public int currentValue;
            public int targetValue;
            public bool unlocked;
            public string unlockedDate;
        }

        // ── JSON wrapper ─────────────────────────────────────
        [Serializable]
        private class AchievementListWrapper
        {
            public List<AchievementData> achievements;
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

        /// <summary>
        /// Carga achievements.json y suscribe a eventos del EventBus.
        /// </summary>
        public void Initialize()
        {
            LoadAchievements();
            SubscribeToEvents();
            Debug.Log($"[AchievementSystem] ✅ Inicializado — {allAchievements.Count} logros cargados");
        }

        public void SetAllAchievements(List<AchievementData> list)
        {
            allAchievements = list;
            RebuildProgressMap();
        }

        private void RebuildProgressMap()
        {
            progressMap.Clear();
            foreach (var ach in allAchievements)
            {
                int targetVal = 1;
                if (ach.condition != null)
                {
                    if (int.TryParse(ach.condition.stringValue, out int parsed))
                        targetVal = parsed;
                    else
                        targetVal = ach.condition.intValue > 0 ? ach.condition.intValue : 1;
                }

                progressMap[ach.id] = new AchievementProgress
                {
                    achievementId = ach.id,
                    currentValue = 0,
                    targetValue = targetVal,
                    unlocked = unlockedIds.Contains(ach.id),
                    unlockedDate = ""
                };
            }
        }

        /// <summary>
        /// Restaura logros desbloqueados desde datos de guardado.
        /// </summary>
        public void LoadUnlockedFromSave(List<string> savedUnlockedIds)
        {
            unlockedIds.Clear();
            if (savedUnlockedIds != null)
            {
                foreach (var id in savedUnlockedIds)
                {
                    unlockedIds.Add(id);
                    if (progressMap.ContainsKey(id))
                        progressMap[id].unlocked = true;
                }
            }
            Debug.Log($"[AchievementSystem] Restaurados {unlockedIds.Count} logros desbloqueados");
        }

        private void LoadAchievements()
        {
            // Intentar cargar desde DataLoader primero si es posible, o Resources directos
            TextAsset jsonFile = Resources.Load<TextAsset>("Data/achievements");
            if (jsonFile == null)
            {
                Debug.LogWarning("[AchievementSystem] ✗ achievements.json no encontrado en Resources/Data/. Usando lista vacía.");
                allAchievements = new List<AchievementData>();
                return;
            }

            try
            {
                AchievementListWrapper wrapper = JsonUtility.FromJson<AchievementListWrapper>(jsonFile.text);
                if (wrapper != null && wrapper.achievements != null)
                {
                    allAchievements = wrapper.achievements;
                    RebuildProgressMap();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AchievementSystem] ✗ Error parseando achievements.json: {e.Message}");
            }
        }

        // ══════════════════════════════════════════════════════
        // SUSCRIPCIÓN A EVENTOS
        // ══════════════════════════════════════════════════════

        private void SubscribeToEvents()
        {
            var bus = EventBus.Instance;
            if (bus == null) return;

            bus.OnRaceFinished += OnRaceFinishedHandler;
            bus.OnSprintFinished += OnSprintFinishedHandler;
            bus.OnSeasonEnd += OnSeasonEndHandler;
            bus.OnTransferCompleted += OnTransferCompletedHandler;
            bus.OnContractSigned += OnContractSignedHandler;
            bus.OnComponentInstalled += OnComponentInstalledHandler;
            bus.OnBudgetChanged += OnBudgetChangedHandler;
            bus.OnStaffChanged += OnStaffChangedHandler;
            bus.OnAcademyPilotPromoted += OnAcademyPilotPromotedHandler;

            Debug.Log("[AchievementSystem] Suscrito a eventos del EventBus");
        }

        private void OnDestroy()
        {
            var bus = EventBus.Instance;
            if (bus != null)
            {
                bus.OnRaceFinished -= OnRaceFinishedHandler;
                bus.OnSprintFinished -= OnSprintFinishedHandler;
                bus.OnSeasonEnd -= OnSeasonEndHandler;
                bus.OnTransferCompleted -= OnTransferCompletedHandler;
                bus.OnContractSigned -= OnContractSignedHandler;
                bus.OnComponentInstalled -= OnComponentInstalledHandler;
                bus.OnBudgetChanged -= OnBudgetChangedHandler;
                bus.OnStaffChanged -= OnStaffChangedHandler;
                bus.OnAcademyPilotPromoted -= OnAcademyPilotPromotedHandler;
            }
        }

        // ══════════════════════════════════════════════════════
        // HANDLERS DE EVENTOS
        // ══════════════════════════════════════════════════════

        private void OnRaceFinishedHandler(object sender, EventBus.RaceFinishedArgs args)
        {
            var playerTeam = GameManager.Instance?.GetPlayerTeam();
            if (playerTeam == null) return;

            // ¿Ganó un piloto del jugador?
            var winner = GameManager.Instance.GetPilotById(args.WinnerId);
            bool playerWon = winner != null && winner.currentTeamId == playerTeam.id;

            if (playerWon)
            {
                IncrementProgress("first_win", 1);
                IncrementProgress("ten_wins", 1);
                IncrementProgress("fifty_wins", 1);
                IncrementProgress("hundred_wins", 1);

                if (args.CircuitId == "monaco") TryUnlock("win_monaco");
                if (args.CircuitId == "monza") TryUnlock("win_monza");
                if (args.CircuitId == "spa") TryUnlock("win_spa");
            }

            // Podios
            if (args.FinalPositions != null)
            {
                int playerPodiums = 0;
                foreach (var pos in args.FinalPositions)
                {
                    if (pos.Position <= 3 && !pos.DNF)
                    {
                        var p = GameManager.Instance.GetPilotById(pos.PilotId);
                        if (p != null && p.currentTeamId == playerTeam.id)
                            playerPodiums++;
                    }
                }

                if (playerPodiums > 0)
                {
                    IncrementProgress("first_podium", playerPodiums);
                    IncrementProgress("fifty_podiums", playerPodiums);
                }

                if (playerPodiums == 2) IncrementProgress("one_two_finish", 1);
            }
        }

        private void OnSprintFinishedHandler(object sender, EventBus.SprintFinishedArgs args)
        {
            // Lógica para logros específicos de Sprint si existen
            IncrementProgress("sprint_points", 1);
        }

        private void OnSeasonEndHandler(object sender, EventBus.SeasonEndArgs args)
        {
            if (args.ObjectiveMet)
            {
                IncrementProgress("objectives_met", 1);
            }

            if (args.FinalPosition == 1)
            {
                TryUnlock("constructors_championship");
            }

            IncrementProgress("seasons_completed", 1);
            
            // Verificar puntos de legado totales para rangos
            var legacy = GameManager.Instance?.Legacy;
            if (legacy != null)
            {
                if (legacy.totalLegacyPoints >= 1000) TryUnlock("rank_contender");
                if (legacy.totalLegacyPoints >= 5000) TryUnlock("rank_established");
                if (legacy.totalLegacyPoints >= 10000) TryUnlock("rank_legend");
            }
        }

        private void OnTransferCompletedHandler(object sender, EventBus.ContractSignedArgs args)
        {
            if (args.IsSuccess)
            {
                var pilot = GameManager.Instance?.GetPilotById(args.PilotId);
                if (pilot != null && pilot.isRegen)
                {
                    TryUnlock("hire_regen");
                }
                IncrementProgress("transfers_completed", 1);
            }
        }

        private void OnContractSignedHandler(object sender, EventBus.ContractSignedArgs args)
        {
            if (args.IsSuccess) IncrementProgress("contracts_signed", 1);
        }

        private void OnComponentInstalledHandler(object sender, EventBus.ComponentInstalledArgs args)
        {
            IncrementProgress("components_installed", 1);
            if (args.PerformanceGain > 5.0f) TryUnlock("mega_upgrade");
        }

        private void OnBudgetChangedHandler(object sender, EventBus.BudgetChangedArgs args)
        {
            if (args.NewBalance >= 200_000_000) TryUnlock("rich_team"); // $200M
            if (args.NewBalance < 1_000_000) TryUnlock("broke_team"); // $1M
        }

        private void OnStaffChangedHandler(object sender, EventBus.StaffChangedArgs args)
        {
            IncrementProgress("staff_changes", 1);
        }

        private void OnAcademyPilotPromotedHandler(string pilotId)
        {
            TryUnlock("academy_graduate");
        }

        // ══════════════════════════════════════════════════════
        // LÓGICA DE PROGRESO Y DESBLOQUEO
        // ══════════════════════════════════════════════════════

        public void IncrementProgress(string achievementId, int amount)
        {
            if (!progressMap.ContainsKey(achievementId)) return;
            if (unlockedIds.Contains(achievementId)) return;

            var progress = progressMap[achievementId];
            progress.currentValue += amount;

            if (progress.currentValue >= progress.targetValue)
            {
                UnlockAchievement(achievementId);
            }
        }

        public void TryUnlock(string achievementId)
        {
            if (unlockedIds.Contains(achievementId)) return;
            UnlockAchievement(achievementId);
        }

        public void UnlockAchievement(string achievementId)
        {
            if (unlockedIds.Contains(achievementId)) return;

            var achievement = allAchievements.FirstOrDefault(a => a.id == achievementId);
            if (achievement == null) return;

            unlockedIds.Add(achievementId);
            if (progressMap.ContainsKey(achievementId))
            {
                progressMap[achievementId].unlocked = true;
                progressMap[achievementId].unlockedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }

            // Otorgar recompensas
            if (achievement.reward != null)
            {
                // Puntos de Legado
                if (achievement.reward.legacyPoints > 0 && GameManager.Instance != null)
                {
                    GameManager.Instance.Legacy.totalLegacyPoints += achievement.reward.legacyPoints;
                }

                // Bonus de Presupuesto
                if (achievement.reward.budgetBonus > 0)
                {
                    var playerTeam = GameManager.Instance?.GetPlayerTeam();
                    if (playerTeam != null)
                    {
                        // Usar BudgetManager si está disponible via singleton o referencia
                        // Por ahora asumimos que el BudgetManager es accesible o el GM lo permite
                        playerTeam.budget += achievement.reward.budgetBonus;
                    }
                }
            }

            // Notificar
            OnAchievementUnlocked?.Invoke(achievement);
            EventBus.Instance?.FireAchievementUnlocked(achievementId);
            
            Debug.Log($"[AchievementSystem] 🏆 Logro Desbloqueado: {achievement.title}");
        }

        // ══════════════════════════════════════════════════════
        // API PÚBLICA
        // ══════════════════════════════════════════════════════

        public List<string> GetUnlockedIds() => unlockedIds.ToList();
        public List<AchievementData> GetAllAchievements() => allAchievements;
        public AchievementProgress GetProgress(string id) => progressMap.ContainsKey(id) ? progressMap[id] : null;
    }
}
