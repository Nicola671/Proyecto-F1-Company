# 🏎️ GUÍA DE CONFIGURACIÓN F1 CAREER MANAGER (UNITY)

Esta guía detalla cómo conectar los **78 archivos C#** y **14 JSONs** dentro de Unity para que el simulador funcione correctamente sin excepciones de referencia nula.

---

## 1. 🏗️ ESTRUCTURA DE LA JERARQUÍA (Escena: `Game.unity`)

Creá estos GameObjects vacíos en tu escena y arrastrá los scripts correspondientes:

### [ CORE_SYSTEMS ]
*   **`_Bootstrapper`**: `Bootstrapper.cs` (Este es el motor de arranque).
*   **`_EventBus`**: `EventBus.cs` (Singleton central).
*   **`_GameManager`**: `GameManager.cs`, `DataLoader.cs`, `SaveManager.cs`.
*   **`_SeasonControl`**: `SeasonManager.cs`, `CalendarManager.cs`, `EpochManager.cs`.
*   **`_Tutorials`**: `TutorialManager.cs`, `NotificationSystem.cs`.

### [ ECONOMY_INTEL ]
*   **`_Economy`**: `BudgetManager.cs`, `SponsorManager.cs`, `EngineSupplierSystem.cs`.
*   **`_Market`**: `TransferManager.cs`, `NegotiationSystem.cs`, `RivalAITransfers.cs`.
*   **`_Academy`**: `AcademyManager.cs`.

### [ RACING_LOGIC ]
*   **`_RaceSim`**: `RaceSimulator.cs`, `RegulationChecker.cs`, `SanctionSystem.cs`.
*   **`_TechDevelopment`**: `TechTreeManager.cs`, `ComponentEvaluator.cs`, `SpySystem.cs`.
*   **`_RegenSystem`**: `NameGenerator.cs`, `RegenGenerator.cs`.

### [ PILOT_STAFF_AI ]
*   **`_Personnel`**: `StaffManager.cs`, `StaffEventSystem.cs`.
*   **`_PilotAI`**: `MoodSystem.cs`, `PilotBehavior.cs`, `RivalrySystem.cs`.
*   **`_Health`**: `InjuryManager.cs`, `RecoverySystem.cs`.

### [ MEDIA_SYSTEMS ]
*   **`_Press`**: `NewsGenerator.cs`, `RumorSystem.cs`, `PressConference.cs`.

### [ UI_MANAGEMENT ] (Bajo un Canvas)
*   **`UIManager`**: `UIManager.cs` (Gestor de navegación).
*   **`Screens`**: (Hijos del UIManager con sus respectivos scripts)
    *   `HubScreen`, `PilotsScreen`, `RnDScreen`, `RaceScreen`, `MarketScreen`, `FinanceScreen`, `LegacyScreen`, `StaffScreen`, `CalendarScreen`, `TeamSelectionScreen`, `SeasonEndScreen`, `SettingsScreen`.

---

## 2. 🔗 REFERENCIAS MANUALES CRÍTICAS (Inspector)

| GameObject | Script | Campo | Arrastrá aquí... |
| :--- | :--- | :--- | :--- |
| `_Bootstrapper` | `Bootstrapper` | `ProgressBar` | Slider de UI de carga |
| `_Bootstrapper` | `Bootstrapper` | `LoadingText` | Texto de UI de carga |
| `_GameManager` | `GameManager` | `DifficultyData` | `difficulty.json` (desde Recursos) |
| `_RaceSim` | `RaceSimulator` | `EventBus` | GameObject `_EventBus` |
| `_Press` | `NewsGenerator` | `NewsFeedUI` | Componente `NewsFeed.cs` en la UI |
| `UIManager` | `UIManager` | `Screens` | Lista de todos los GameObjects en `Screens` |
| `_Personnel` | `StaffManager` | `StaffEventSystem` | GameObject `_Personnel` |

> **IMPORTANTE**: La mayoría de los scripts usan `Instance` (Singletons). Asegurate de que **solo haya una copia** de cada script en la escena.

---

## 3. ⚠️ ERRORES COMUNES Y SOLUCIONES

1.  **"CS0246: The type or namespace 'Newtonsoft' could not be found"**
    *   *Solución*: Instalar el paquete `JSON .NET` desde el Package Manager de Unity o usar `JsonUtility` (nuestros scripts son compatibles con ambos).
2.  **"NullReferenceException: EventBus.Instance is null"**
    *   *Solución*: Asegurate de que `EventBus` esté el primero en la lista de inicialización del `Bootstrapper.cs`.
3.  **"JSON File not found in Resources/Data"**
    *   *Solución*: Verificá que los 14 archivos JSON estén en la carpeta `Assets/_Project/Resources/Data/` (exactamente con ese nombre).
4.  **Error de Compilación por 'Android Build Support'**
    *   *Solución*: Si no vas a exportar a móvil todavía, eliminá los bloques `#if UNITY_ANDROID` en `NotificationSystem.cs`.
5.  **"Screen not found in UIManager dictionary"**
    *   *Solución*: Asegurate de que el nombre del GameObject de la pantalla coincida con el ID que pide el script (ej: "HubScreen" vs "HUB").

---

## 4. 🧪 ORDEN DE PRUEBAS RECOMENDADO

1.  **Play Mode (Carga)**: Verificá que la barra de progreso del `Bootstrapper` llegue al 100% sin errores rojos en la consola.
2.  **Test de Datos**: Abrí la consola y buscá: `[DataLoader] 750 items loaded`.
3.  **Test de Selección**: Empezá una "Nueva Partida" en la `TeamSelectionScreen`. Verificá que los stats del equipo cambien al hacer click.
4.  **Test de Hub**: Verificá que el presupuesto y la semana se muestren correctamente.
5.  **Test de Simulación**: Mantené presionado el botón "Avanzar Semana" y verificá que los eventos aleatorios y noticias aparezcan en el feed.

---

## 5. ✅ CHECKLIST FINAL (Antes de dar Play)

*   [ ] ¿Ejecutaste `F1 Career Manager > Setup Completo` en el menú superior?
*   [ ] ¿Están los 14 JSONs en `Resources/Data`?
*   [ ] ¿El `Bootstrapper` está asignado al objeto superior de la escena?
*   [ ] ¿Tenés un `EventSystem` en la UI (creado por Unity automáticamente)?
*   [ ] ¿Configuraste el `Build Settings` para plataforma **Android** o **iOS** (Portrait)?

---
*F1 Career Manager — Documentación de Ingeniería v1.0*
