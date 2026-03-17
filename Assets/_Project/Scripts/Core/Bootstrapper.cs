// ============================================================
// F1 Career Manager — Bootstrapper.cs (Versión Final)
// Motor de inicialización de TODOS los sistemas del juego.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using F1CareerManager.Core;
using F1CareerManager.Simulation;
using F1CareerManager.AI.EconomyAI;
using F1CareerManager.AI.PilotAI;
using F1CareerManager.AI.RnDAI;
using F1CareerManager.Academy;

namespace F1CareerManager.Core
{
    public class Bootstrapper : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private Slider loadingBar;
        [SerializeField] private Text loadingText;

        private void Start()
        {
            StartCoroutine(InitializeAllSystems());
        }

        private IEnumerator InitializeAllSystems()
        {
            float total = 22f;
            float current = 0f;

            void UpdateProgress(string msg)
            {
                current++;
                if (loadingBar != null) loadingBar.value = current / total;
                if (loadingText != null) loadingText.text = msg;
                Debug.Log($"[Bootstrapper] {msg} ({current}/{total})");
            }

            // 1. EventBus (Base de comunicación)
            UpdateProgress("Inicializando EventBus...");
            yield return null;

            // 2. DataLoader (Carga de JSONs)
            UpdateProgress("Cargando base de datos...");
            DataLoader.Instance.LoadAllData();
            yield return new WaitForSeconds(0.1f);

            // 3. DifficultyManager (Balance global)
            UpdateProgress("Aplicando nivel de dificultad...");
            DifficultyManager.Instance.Initialize("STANDARD");

            // 4. Managers Base
            UpdateProgress("Iniciando Calendario...");
            CalendarManager.Instance.Initialize();
            
            UpdateProgress("Iniciando Gestor de Juego...");
            GameManager.Instance.Initialize();

            UpdateProgress("Sincronizando Guardado...");
            SaveManager.Instance.Initialize();

            // 5. Economía & Sponsors
            UpdateProgress("Calculando Presupuestos...");
            BudgetManager.Instance.Initialize();
            
            UpdateProgress("Cargando Patrocinadores...");
            SponsorManager.Instance.Initialize();

            UpdateProgress("Asignando Suministradores de Motor...");
            EngineSupplierSystem.Instance.Initialize();

            // 6. I+D & Espionaje
            UpdateProgress("Cargando Árbol Tecnológico...");
            TechTreeManager.Instance.Initialize();
            ComponentEvaluator.Instance.Initialize();
            
            UpdateProgress("Activando Red de Espionaje...");
            SpySystem.Instance.Initialize();

            // 7. Personal & Academia
            UpdateProgress("Iniciando Academia Junior...");
            AcademyManager.Instance.Initialize();

            UpdateProgress("Cargando Staff del Paddock...");
            StaffManager.Instance.Initialize();

            // 8. Pilotos & Rivalidades
            UpdateProgress("Generando Rivalidades...");
            RivalrySystem.Instance.Initialize();
            
            UpdateProgress("Iniciando IAs de Pilotos...");
            MoodSystem.Instance.Initialize();
            PilotBehavior.Instance.Initialize();

            // 9. Mercado & Traspasos
            UpdateProgress("Abriendo Mercado de Transferencias...");
            TransferManager.Instance.Initialize();
            RivalAITransfers.Instance.Initialize();

            // 10. Simulación de Carreras (Weekend Flow)
            UpdateProgress("Calibrando Simulador de Prácticas...");
            PracticeSimulator.Instance.Initialize();
            
            UpdateProgress("Calibrando Simulador de Qualy...");
            QualifyingSimulator.Instance.Initialize();
            
            UpdateProgress("Configurando Formato Sprint...");
            SprintSimulator.Instance.Initialize();

            UpdateProgress("Sincronizando Simulador de Carrera...");
            RaceSimulator.Instance.Initialize();

            // 11. Logros
            UpdateProgress("Iniciando Sistema de Logros...");
            AchievementSystem.Instance.Initialize();

            // 12. Finalización
            UpdateProgress("Lanzando GameHub...");
            yield return new WaitForSeconds(0.5f);
            
            EventBus.Instance.FireGameReady();
            Debug.Log("<color=green>[Bootstrapper] TODO INICIALIZADO CORRECTAMENTE</color>");
        }
    }
}
