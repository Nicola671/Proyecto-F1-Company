// ============================================================
// F1 Career Manager — EditorSetupTool.cs
// Herramienta de editor que configura TODO el proyecto
// desde el menú de Unity con UN SOLO CLICK
// ============================================================
// USO: Menú Unity → F1 Career Manager → Setup Completo
// ============================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace F1CareerManager.Editor
{
    public class EditorSetupTool : EditorWindow
    {
        // ── Constantes ───────────────────────────────────────
        private const string PROJECT_ROOT = "Assets/_Project";
        private const string SCENES_PATH = "Assets/_Project/Scenes";
        private const string PREFABS_PATH = "Assets/_Project/Prefabs";
        private const string SPRITES_PATH = "Assets/_Project/Sprites";
        private const string RESOURCES_PATH = "Assets/Resources/Data";
        private const string FONTS_PATH = "Assets/_Project/Fonts";
        private const string MATERIALS_PATH = "Assets/_Project/Materials";

        // ── Resolución mobile ────────────────────────────────
        private const int CANVAS_WIDTH = 1080;
        private const int CANVAS_HEIGHT = 1920;

        // ── Colores del UITheme ──────────────────────────────
        private static readonly Color BG_DARK = new Color(0.051f, 0.051f, 0.051f, 1f);     // #0D0D0D
        private static readonly Color BG_PANEL = new Color(0.102f, 0.102f, 0.102f, 1f);    // #1A1A1A
        private static readonly Color BG_CARD = new Color(0.141f, 0.141f, 0.141f, 1f);     // #242424
        private static readonly Color ACCENT_RED = new Color(0.91f, 0f, 0.176f, 1f);       // #E8002D
        private static readonly Color ACCENT_GOLD = new Color(0.831f, 0.627f, 0.09f, 1f);  // #D4A017
        private static readonly Color TEXT_WHITE = Color.white;
        private static readonly Color TEXT_GREY = new Color(0.541f, 0.541f, 0.541f, 1f);   // #8A8A8A

        // ══════════════════════════════════════════════════════
        // MENÚ DE UNITY
        // ══════════════════════════════════════════════════════

        [MenuItem("F1 Career Manager/🏗️ Setup Completo (Todo Automático)", false, 0)]
        public static void RunFullSetup()
        {
            if (!EditorUtility.DisplayDialog(
                "F1 Career Manager — Setup Completo",
                "Esto va a crear:\n\n" +
                "• 4 Escenas (Boot, MainMenu, Game, Loading)\n" +
                "• Canvas mobile 1080x1920\n" +
                "• Todos los GameObjects con managers\n" +
                "• Prefabs de UI con placeholders\n" +
                "• Carpetas de Resources y Sprites\n" +
                "• Sprites placeholder\n\n" +
                "¿Continuar?",
                "Sí, crear todo", "Cancelar"))
            {
                return;
            }

            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("  F1 CAREER MANAGER — SETUP COMPLETO");
            Debug.Log("═══════════════════════════════════════════");

            CreateFolderStructure();
            CreatePlaceholderSprites();
            CreatePlaceholderJsons();
            CreateScenes();
            SetupMainGameScene();
            CreateUIPrefabs();
            SetBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("  ✅ SETUP COMPLETO — Todo listo!");
            Debug.Log("═══════════════════════════════════════════");

            EditorUtility.DisplayDialog(
                "✅ Setup Completo",
                "Todo creado exitosamente:\n\n" +
                "• 4 Escenas\n" +
                "• Canvas mobile configurado\n" +
                "• Managers conectados\n" +
                "• Prefabs creados\n" +
                "• Sprites placeholder\n" +
                "• JSONs de ejemplo\n\n" +
                "Abrí la escena 'Game' para empezar.",
                "¡Genial!");
        }

        [MenuItem("F1 Career Manager/📁 Solo Carpetas", false, 10)]
        public static void CreateFoldersOnly()
        {
            CreateFolderStructure();
            AssetDatabase.Refresh();
            Debug.Log("✅ Carpetas creadas");
        }

        [MenuItem("F1 Career Manager/🎨 Solo Sprites Placeholder", false, 11)]
        public static void CreateSpritesOnly()
        {
            CreateFolderStructure();
            CreatePlaceholderSprites();
            AssetDatabase.Refresh();
            Debug.Log("✅ Sprites placeholder creados");
        }

        [MenuItem("F1 Career Manager/🎬 Solo Escenas", false, 12)]
        public static void CreateScenesOnly()
        {
            CreateFolderStructure();
            CreateScenes();
            AssetDatabase.Refresh();
            Debug.Log("✅ Escenas creadas");
        }

        // ══════════════════════════════════════════════════════
        // 1. ESTRUCTURA DE CARPETAS
        // ══════════════════════════════════════════════════════

        private static void CreateFolderStructure()
        {
            Debug.Log("[Setup] Creando carpetas...");

            string[] folders = {
                SCENES_PATH,
                PREFABS_PATH,
                PREFABS_PATH + "/UI",
                PREFABS_PATH + "/UI/Components",
                PREFABS_PATH + "/UI/Screens",
                SPRITES_PATH,
                SPRITES_PATH + "/Pilots",
                SPRITES_PATH + "/Teams",
                SPRITES_PATH + "/Circuits",
                SPRITES_PATH + "/UI",
                SPRITES_PATH + "/Tires",
                SPRITES_PATH + "/Stars",
                SPRITES_PATH + "/Icons",
                RESOURCES_PATH,
                FONTS_PATH,
                MATERIALS_PATH,
                PROJECT_ROOT + "/Animations"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
                    string name = Path.GetFileName(folder);

                    // Crear padres si no existen
                    EnsureFolder(folder);
                }
            }

            Debug.Log("  ✅ Carpetas creadas");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            string folderName = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        // ══════════════════════════════════════════════════════
        // 2. SPRITES PLACEHOLDER (Texture2D pixel art)
        // ══════════════════════════════════════════════════════

        private static void CreatePlaceholderSprites()
        {
            Debug.Log("[Setup] Creando sprites placeholder...");

            // Estrellas (16x16)
            CreatePixelSprite("Stars/star_full", 16, 16, ACCENT_GOLD);
            CreatePixelSprite("Stars/star_half", 16, 16,
                new Color(ACCENT_GOLD.r, ACCENT_GOLD.g, ACCENT_GOLD.b, 0.5f));
            CreatePixelSprite("Stars/star_empty", 16, 16, TEXT_GREY);

            // Piloto placeholder (32x48)
            CreatePilotPlaceholder();

            // Logo equipo (48x48)
            CreatePixelSprite("Teams/team_logo_placeholder", 48, 48, ACCENT_RED);

            // Circuito (256x128)
            CreateCircuitPlaceholder();

            // Auto silueta (128x64)
            CreateCarSilhouette();

            // Neumáticos (16x16)
            CreatePixelSprite("Tires/tire_soft", 16, 16,
                new Color(1f, 0.2f, 0.2f)); // Rojo
            CreatePixelSprite("Tires/tire_medium", 16, 16,
                new Color(1f, 0.85f, 0f));   // Amarillo
            CreatePixelSprite("Tires/tire_hard", 16, 16,
                Color.white);                 // Blanco

            // Íconos de navegación (24x24)
            string[] navIcons = { "hub", "garage", "pilots", "race",
                "finance", "market", "rnd", "staff" };
            Color[] navColors = { ACCENT_RED, TEXT_GREY, TEXT_WHITE, ACCENT_GOLD,
                new Color(0.13f, 0.77f, 0.37f), new Color(0.38f, 0.58f, 1f),
                new Color(0.96f, 0.62f, 0.04f), TEXT_GREY };

            for (int i = 0; i < navIcons.Length; i++)
            {
                CreateNavIcon(navIcons[i], i < navColors.Length ? navColors[i] : TEXT_WHITE);
            }

            Debug.Log("  ✅ Sprites placeholder creados");
        }

        private static void CreatePixelSprite(string name, int w, int h, Color color)
        {
            string path = $"{SPRITES_PATH}/{name}.png";
            if (File.Exists(path)) return;

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            SetSpriteImportSettings(path);
        }

        private static void CreatePilotPlaceholder()
        {
            string path = $"{SPRITES_PATH}/Pilots/pilot_placeholder.png";
            if (File.Exists(path)) return;

            int w = 32, h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[w * h];
            Color skin = new Color(0.93f, 0.78f, 0.65f);
            Color helmet = ACCENT_RED;
            Color suit = BG_CARD;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = Color.clear;
                    int cx = w / 2, cy = h / 2;

                    // Casco (parte superior)
                    if (y >= 34 && y < 46)
                    {
                        float dx = Mathf.Abs(x - cx);
                        if (dx < 10) c = helmet;
                        // Visor
                        if (y >= 36 && y < 40 && dx < 7)
                            c = new Color(0.2f, 0.6f, 0.9f, 0.8f);
                    }
                    // Cuello
                    else if (y >= 30 && y < 34)
                    {
                        if (Mathf.Abs(x - cx) < 5) c = skin;
                    }
                    // Traje (cuerpo)
                    else if (y >= 10 && y < 30)
                    {
                        float dx = Mathf.Abs(x - cx);
                        if (dx < 12 - (30 - y) * 0.2f) c = suit;
                    }
                    // Piernas
                    else if (y >= 2 && y < 10)
                    {
                        if ((Mathf.Abs(x - cx - 4) < 3) || (Mathf.Abs(x - cx + 4) < 3))
                            c = suit;
                    }

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            SetSpriteImportSettings(path);
        }

        private static void CreateCircuitPlaceholder()
        {
            string path = $"{SPRITES_PATH}/Circuits/circuit_placeholder.png";
            if (File.Exists(path)) return;

            int w = 256, h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color track = TEXT_GREY;
            Color bg = new Color(0.15f, 0.3f, 0.15f); // Verde pasto

            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = bg;

            // Dibujar pista simple (óvalo)
            int cx = w / 2, cy = h / 2;
            int rx = 90, ry = 40;
            int thickness = 6;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (float)(x - cx) / rx;
                    float dy = (float)(y - cy) / ry;
                    float dist = dx * dx + dy * dy;
                    if (dist >= 0.7f && dist <= 1.0f)
                        pixels[y * w + x] = track;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            SetSpriteImportSettings(path);
        }

        private static void CreateCarSilhouette()
        {
            string path = $"{SPRITES_PATH}/UI/car_silhouette.png";
            if (File.Exists(path)) return;

            int w = 128, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[w * h];
            Color car = TEXT_WHITE;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = Color.clear;

                    // Cuerpo del auto (forma F1 simplificada)
                    // Nariz
                    if (y >= 28 && y <= 36 && x >= 5 && x <= 40)
                        c = car;
                    // Cockpit
                    if (y >= 24 && y <= 40 && x >= 40 && x <= 80)
                        c = car;
                    // Motor trasero
                    if (y >= 26 && y <= 38 && x >= 80 && x <= 110)
                        c = car;
                    // Alerón delantero
                    if (y >= 22 && y <= 42 && x >= 2 && x <= 8)
                        c = car;
                    // Alerón trasero
                    if (y >= 18 && y <= 46 && x >= 108 && x <= 115)
                        c = car;
                    // Ruedas
                    if (((x >= 20 && x <= 30) || (x >= 90 && x <= 100)) &&
                        ((y >= 18 && y <= 24) || (y >= 40 && y <= 46)))
                        c = new Color(0.2f, 0.2f, 0.2f);

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            SetSpriteImportSettings(path);
        }

        private static void CreateNavIcon(string name, Color color)
        {
            string path = $"{SPRITES_PATH}/Icons/nav_{name}.png";
            if (File.Exists(path)) return;

            int w = 24, h = 24;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[w * h];

            // Ícono genérico: cuadrado con borde
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x >= 3 && x < w - 3 && y >= 3 && y < h - 3)
                    {
                        // Borde exterior
                        if (x == 3 || x == w - 4 || y == 3 || y == h - 4)
                            pixels[y * w + x] = color;
                        // Interior con patrón simple
                        else if ((x + y) % 4 == 0)
                            pixels[y * w + x] = new Color(color.r, color.g, color.b, 0.3f);
                        else
                            pixels[y * w + x] = Color.clear;
                    }
                    else
                        pixels[y * w + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            SetSpriteImportSettings(path);
        }

        private static void SetSpriteImportSettings(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            // Mobile: max 256
            TextureImporterPlatformSettings mobile = new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 256,
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed
            };
            importer.SetPlatformTextureSettings(mobile);

            var ios = new TextureImporterPlatformSettings
            {
                name = "iPhone",
                overridden = true,
                maxTextureSize = 256,
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed
            };
            importer.SetPlatformTextureSettings(ios);

            importer.SaveAndReimport();
        }

        // ══════════════════════════════════════════════════════
        // 3. JSONs PLACEHOLDER
        // ══════════════════════════════════════════════════════

        private static void CreatePlaceholderJsons()
        {
            Debug.Log("[Setup] Creando JSONs placeholder...");

            // Verificar si ya existen (no sobrescribir)
            string pilotsPath = $"{RESOURCES_PATH}/pilots.json";
            if (!File.Exists(pilotsPath))
            {
                // Archivo mínimo de ejemplo
                string pilotsJson = @"{
  ""pilots"": [
    {
      ""id"": ""verstappen"", ""firstName"": ""Max"", ""lastName"": ""Verstappen"",
      ""nationality"": ""Dutch"", ""age"": 27, ""currentTeamId"": ""redbull"",
      ""speed"": 95, ""racePace"": 96, ""overtaking"": 94, ""defence"": 93,
      ""tireManagement"": 90, ""wetDriving"": 95, ""consistency"": 92,
      ""racecraft"": 97, ""qualifying"": 94, ""fitness"": 88,
      ""potential"": 98, ""stars"": 5, ""salary"": 45.0,
      ""contractYearsLeft"": 4, ""moodValue"": 80,
      ""isRegen"": false, ""isRetired"": false
    },
    {
      ""id"": ""hamilton"", ""firstName"": ""Lewis"", ""lastName"": ""Hamilton"",
      ""nationality"": ""British"", ""age"": 40, ""currentTeamId"": ""ferrari"",
      ""speed"": 88, ""racePace"": 92, ""overtaking"": 93, ""defence"": 90,
      ""tireManagement"": 95, ""wetDriving"": 96, ""consistency"": 88,
      ""racecraft"": 95, ""qualifying"": 89, ""fitness"": 82,
      ""potential"": 85, ""stars"": 5, ""salary"": 40.0,
      ""contractYearsLeft"": 2, ""moodValue"": 70,
      ""isRegen"": false, ""isRetired"": false
    }
  ]
}";
                File.WriteAllText(pilotsPath, pilotsJson);
            }

            string teamsPath = $"{RESOURCES_PATH}/teams.json";
            if (!File.Exists(teamsPath))
            {
                string teamsJson = @"{
  ""teams"": [
    {
      ""id"": ""redbull"", ""fullName"": ""Oracle Red Bull Racing"",
      ""shortName"": ""Red Bull"", ""nationality"": ""Austrian"",
      ""budget"": 145.0, ""baseBudget"": 145.0, ""reputation"": 95,
      ""aeroRating"": 90, ""engineRating"": 92, ""chassisRating"": 88,
      ""reliabilityRating"": 85, ""constructorPoints"": 0,
      ""constructorPosition"": 1, ""isPlayerControlled"": false,
      ""totalWins"": 0, ""totalPodiums"": 0
    },
    {
      ""id"": ""ferrari"", ""fullName"": ""Scuderia Ferrari HP"",
      ""shortName"": ""Ferrari"", ""nationality"": ""Italian"",
      ""budget"": 140.0, ""baseBudget"": 140.0, ""reputation"": 93,
      ""aeroRating"": 88, ""engineRating"": 90, ""chassisRating"": 87,
      ""reliabilityRating"": 82, ""constructorPoints"": 0,
      ""constructorPosition"": 2, ""isPlayerControlled"": false,
      ""totalWins"": 0, ""totalPodiums"": 0
    }
  ]
}";
                File.WriteAllText(teamsPath, teamsJson);
            }

            string circuitsPath = $"{RESOURCES_PATH}/circuits.json";
            if (!File.Exists(circuitsPath))
            {
                string circuitsJson = @"{
  ""circuits"": [
    {
      ""id"": ""bahrain"", ""name"": ""Bahrain International Circuit"",
      ""shortName"": ""Bahrain"", ""city"": ""Sakhir"", ""country"": ""Bahrain"",
      ""countryCode"": ""BH"", ""roundNumber"": 1, ""totalLaps"": 57,
      ""lapDistanceKm"": 5.412, ""circuitType"": ""Mixed"", ""drsZones"": 3,
      ""isNightRace"": true, ""rainChance"": 0.02, ""safetyCarChance"": 0.35,
      ""tireDegradation"": ""High"", ""overtakingDifficulty"": 35,
      ""baseTemperature"": 28, ""humidityFactor"": 0.3,
      ""favors"": [""potencia_motor""], ""hinders"": []
    }
  ]
}";
                File.WriteAllText(circuitsPath, circuitsJson);
            }

            Debug.Log("  ✅ JSONs placeholder creados en Resources/Data/");
        }

        // ══════════════════════════════════════════════════════
        // 4. ESCENAS
        // ══════════════════════════════════════════════════════

        private static void CreateScenes()
        {
            Debug.Log("[Setup] Creando escenas...");

            CreateScene("Boot", true);
            CreateScene("MainMenu", false);
            CreateScene("Game", false);
            CreateScene("Loading", false);

            Debug.Log("  ✅ 4 escenas creadas");
        }

        private static void CreateScene(string sceneName, bool isFirst)
        {
            string scenePath = $"{SCENES_PATH}/{sceneName}.unity";
            if (File.Exists(scenePath)) return;

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Cámara
            GameObject cam = new GameObject("Main Camera");
            var camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.backgroundColor = BG_DARK;
            camera.clearFlags = CameraClearFlags.SolidColor;
            cam.tag = "MainCamera";
            cam.AddComponent<AudioListener>();

            // Luz
            GameObject light = new GameObject("Directional Light");
            var lightComp = light.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // ══════════════════════════════════════════════════════
        // 5. SETUP DE ESCENA GAME (LA PRINCIPAL)
        // ══════════════════════════════════════════════════════

        private static void SetupMainGameScene()
        {
            Debug.Log("[Setup] Configurando escena Game...");

            string scenePath = $"{SCENES_PATH}/Game.unity";
            var scene = EditorSceneManager.OpenScene(scenePath);

            // ── 5.1 CANVAS MOBILE ────────────────────────────

            GameObject canvasObj = new GameObject("MainCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CANVAS_WIDTH, CANVAS_HEIGHT);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // ── 5.2 MANAGERS ─────────────────────────────────

            // GameSystemsManager (contenedor)
            GameObject systemsRoot = new GameObject("═══ GAME SYSTEMS ═══");

            // Nota: GameManager y EventBus son singletons C# puros (no MonoBehaviour)
            // pero necesitamos un MonoBehaviour que los inicialice

            GameObject bootstrapper = CreateChild(systemsRoot, "GameBootstrapper");
            // Se le agrega un script que llame GameManager.Instance + DataLoader.LoadAll()
            AddNote(bootstrapper, "Agregar script Bootstrapper que inicialice todos los sistemas");

            // UI Systems
            GameObject uiRoot = new GameObject("═══ UI SYSTEMS ═══");

            // UIManager
            GameObject uiMgr = CreateChild(uiRoot, "UIManager");
            AddNote(uiMgr, "Agregar UIManager.cs y asignar referencias a pantallas");

            // ScreenTransition
            GameObject transObj = CreateChild(uiRoot, "ScreenTransition");
            AddNote(transObj, "Agregar ScreenTransition.cs");

            // NotificationToast
            GameObject toastObj = CreateChild(uiRoot, "NotificationToast");
            AddNote(toastObj, "Agregar NotificationToast.cs + crear UI de toast");

            // ── 5.3 PANTALLAS ────────────────────────────────

            GameObject screensRoot = CreateChild(canvasObj, "Screens");

            // Crear cada pantalla como hijo del canvas
            string[] screenNames = { "HubScreen", "PilotScreen", "RnDScreen",
                "RaceScreen", "ResultsScreen", "MarketScreen", "FinanceScreen" };

            foreach (var screenName in screenNames)
            {
                GameObject screenObj = CreateScreenPanel(screensRoot, screenName);

                // Solo HubScreen visible al inicio
                if (screenName != "HubScreen")
                    screenObj.SetActive(false);
            }

            // ── 5.4 POPUPS ──────────────────────────────────

            GameObject popupsRoot = CreateChild(canvasObj, "Popups");

            string[] popupNames = { "ContractPopup", "PressConfPopup",
                "RandomEventPopup", "SeasonEndPopup", "SettingsPopup" };

            foreach (var popupName in popupNames)
            {
                GameObject popup = CreatePopupPanel(popupsRoot, popupName);
                popup.SetActive(false);
            }

            // ── 5.5 HUD PERMANENTE ──────────────────────────

            GameObject hudRoot = CreateChild(canvasObj, "HUD");

            // Barra superior (equipo + presupuesto + semana)
            CreateTopBar(hudRoot);

            // Barra de navegación inferior
            CreateBottomNavBar(hudRoot);

            // ── 5.6 GUARDAR ─────────────────────────────────

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("  ✅ Escena Game configurada");
        }

        // ══════════════════════════════════════════════════════
        // 5.x HELPERS DE UI
        // ══════════════════════════════════════════════════════

        private static GameObject CreateScreenPanel(GameObject parent, string name)
        {
            GameObject panel = CreateChild(parent, name);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = BG_DARK;

            // Título de la pantalla
            GameObject titleObj = CreateChild(panel, "Title");
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = name.Replace("Screen", "");
            titleText.color = TEXT_WHITE;
            titleText.fontSize = 28;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.1f, 0.9f);
            titleRt.anchorMax = new Vector2(0.9f, 0.95f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            // Placeholder text
            GameObject placeholder = CreateChild(panel, "Placeholder");
            Text placeholderText = placeholder.AddComponent<Text>();
            placeholderText.text = $"[{name}]\nAgregar componente: {name}.cs";
            placeholderText.color = TEXT_GREY;
            placeholderText.fontSize = 18;
            placeholderText.alignment = TextAnchor.MiddleCenter;
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform phRt = placeholder.GetComponent<RectTransform>();
            phRt.anchorMin = new Vector2(0.1f, 0.4f);
            phRt.anchorMax = new Vector2(0.9f, 0.6f);
            phRt.offsetMin = Vector2.zero;
            phRt.offsetMax = Vector2.zero;

            return panel;
        }

        private static GameObject CreatePopupPanel(GameObject parent, string name)
        {
            // Overlay oscuro
            GameObject overlay = CreateChild(parent, name);
            RectTransform rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image overlayBg = overlay.AddComponent<Image>();
            overlayBg.color = new Color(0, 0, 0, 0.7f);

            // Panel del popup
            GameObject popupPanel = CreateChild(overlay, "Panel");
            RectTransform popRt = popupPanel.GetComponent<RectTransform>();
            popRt.anchorMin = new Vector2(0.05f, 0.15f);
            popRt.anchorMax = new Vector2(0.95f, 0.85f);
            popRt.offsetMin = Vector2.zero;
            popRt.offsetMax = Vector2.zero;

            Image popBg = popupPanel.AddComponent<Image>();
            popBg.color = BG_PANEL;

            // Título
            GameObject titleObj = CreateChild(popupPanel, "Title");
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = name;
            titleText.color = TEXT_WHITE;
            titleText.fontSize = 24;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.88f);
            titleRt.anchorMax = new Vector2(0.95f, 0.98f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            // Botón cerrar
            GameObject closeBtn = CreateChild(popupPanel, "CloseButton");
            RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.85f, 0.88f);
            closeRt.anchorMax = new Vector2(0.98f, 0.98f);
            closeRt.offsetMin = Vector2.zero;
            closeRt.offsetMax = Vector2.zero;

            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = ACCENT_RED;
            closeBtn.AddComponent<Button>();

            GameObject closeText = CreateChild(closeBtn, "Text");
            Text ct = closeText.AddComponent<Text>();
            ct.text = "X";
            ct.color = TEXT_WHITE;
            ct.fontSize = 20;
            ct.alignment = TextAnchor.MiddleCenter;
            ct.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform ctRt = closeText.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = Vector2.zero;
            ctRt.offsetMax = Vector2.zero;

            return overlay;
        }

        private static void CreateTopBar(GameObject parent)
        {
            GameObject topBar = CreateChild(parent, "TopBar");
            RectTransform rt = topBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.95f);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = topBar.AddComponent<Image>();
            bg.color = new Color(BG_PANEL.r, BG_PANEL.g, BG_PANEL.b, 0.95f);

            // Elementos
            AddUIText(topBar, "TeamName", "[EQUIPO]", TEXT_WHITE, 20,
                0.02f, 0.5f, 0.1f, 0.9f, TextAnchor.MiddleLeft);
            AddUIText(topBar, "Season", "T1 — S12", TEXT_GREY, 16,
                0.5f, 0.7f, 0.1f, 0.9f, TextAnchor.MiddleCenter);
            AddUIText(topBar, "Budget", "💰 $145M", ACCENT_GOLD, 16,
                0.7f, 0.98f, 0.1f, 0.9f, TextAnchor.MiddleRight);
        }

        private static void CreateBottomNavBar(GameObject parent)
        {
            GameObject navBar = CreateChild(parent, "BottomNavBar");
            RectTransform rt = navBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0.065f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = navBar.AddComponent<Image>();
            bg.color = new Color(BG_PANEL.r, BG_PANEL.g, BG_PANEL.b, 0.98f);

            // Botones de navegación
            string[] labels = { "🏠", "🏎️", "👤", "🏁", "💰", "📦", "🔧", "👔" };
            string[] names = { "Hub", "Garage", "Pilots", "Race",
                "Finance", "Market", "RnD", "Staff" };

            for (int i = 0; i < labels.Length; i++)
            {
                float xMin = (float)i / labels.Length;
                float xMax = (float)(i + 1) / labels.Length;

                GameObject btn = CreateChild(navBar, $"Nav_{names[i]}");
                RectTransform btnRt = btn.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(xMin + 0.005f, 0.05f);
                btnRt.anchorMax = new Vector2(xMax - 0.005f, 0.95f);
                btnRt.offsetMin = Vector2.zero;
                btnRt.offsetMax = Vector2.zero;

                Image btnBg = btn.AddComponent<Image>();
                btnBg.color = i == 0 ? ACCENT_RED : Color.clear;
                btn.AddComponent<Button>();

                GameObject label = CreateChild(btn, "Label");
                Text t = label.AddComponent<Text>();
                t.text = labels[i];
                t.fontSize = 22;
                t.color = TEXT_WHITE;
                t.alignment = TextAnchor.MiddleCenter;
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                RectTransform lRt = label.GetComponent<RectTransform>();
                lRt.anchorMin = Vector2.zero;
                lRt.anchorMax = Vector2.one;
                lRt.offsetMin = Vector2.zero;
                lRt.offsetMax = Vector2.zero;
            }
        }

        // ══════════════════════════════════════════════════════
        // 6. PREFABS DE UI
        // ══════════════════════════════════════════════════════

        private static void CreateUIPrefabs()
        {
            Debug.Log("[Setup] Creando prefabs...");

            CreateStatBarPrefab();
            CreateStarRatingPrefab();
            CreateToastPrefab();
            CreatePilotCardPrefab();
            CreateNewsItemPrefab();

            Debug.Log("  ✅ Prefabs creados");
        }

        private static void CreateStatBarPrefab()
        {
            string path = $"{PREFABS_PATH}/UI/Components/StatBar.prefab";
            if (File.Exists(path)) return;

            GameObject root = new GameObject("StatBar", typeof(RectTransform), typeof(Image));
            root.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 24);

            // Fill
            GameObject fill = CreateChild(root, "Fill");
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = ACCENT_RED;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.7f;
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);

            // Label
            AddUIText(root, "Label", "SPD", TEXT_WHITE, 11,
                0.02f, 0.3f, 0, 1, TextAnchor.MiddleLeft);

            // Value
            AddUIText(root, "Value", "85", TEXT_WHITE, 12,
                0.7f, 0.98f, 0, 1, TextAnchor.MiddleRight);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateStarRatingPrefab()
        {
            string path = $"{PREFABS_PATH}/UI/Components/StarRating.prefab";
            if (File.Exists(path)) return;

            GameObject root = new GameObject("StarRating", typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 20);

            for (int i = 0; i < 5; i++)
            {
                GameObject star = CreateChild(root, $"Star_{i + 1}");
                Image img = star.AddComponent<Image>();
                img.color = i < 3 ? ACCENT_GOLD : TEXT_GREY;

                RectTransform rt = star.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(i * 0.2f, 0);
                rt.anchorMax = new Vector2((i + 1) * 0.2f, 1);
                rt.offsetMin = new Vector2(2, 2);
                rt.offsetMax = new Vector2(-2, -2);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateToastPrefab()
        {
            string path = $"{PREFABS_PATH}/UI/Components/Toast.prefab";
            if (File.Exists(path)) return;

            GameObject root = new GameObject("Toast", typeof(RectTransform), typeof(Image));
            root.GetComponent<Image>().color = BG_CARD;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 60);

            // Color bar
            GameObject colorBar = CreateChild(root, "ColorBar");
            Image barImg = colorBar.AddComponent<Image>();
            barImg.color = ACCENT_RED;
            RectTransform barRt = colorBar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0);
            barRt.anchorMax = new Vector2(0.015f, 1);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;

            // Message
            AddUIText(root, "Message", "Notificación de ejemplo",
                TEXT_WHITE, 14, 0.04f, 0.95f, 0.1f, 0.9f, TextAnchor.MiddleLeft);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreatePilotCardPrefab()
        {
            string path = $"{PREFABS_PATH}/UI/Components/PilotCard.prefab";
            if (File.Exists(path)) return;

            GameObject root = new GameObject("PilotCard", typeof(RectTransform), typeof(Image));
            root.GetComponent<Image>().color = BG_CARD;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 120);

            // Sprite piloto
            GameObject sprite = CreateChild(root, "PilotSprite");
            Image spriteImg = sprite.AddComponent<Image>();
            spriteImg.color = TEXT_GREY;
            RectTransform spriteRt = sprite.GetComponent<RectTransform>();
            spriteRt.anchorMin = new Vector2(0.02f, 0.1f);
            spriteRt.anchorMax = new Vector2(0.18f, 0.9f);
            spriteRt.offsetMin = Vector2.zero;
            spriteRt.offsetMax = Vector2.zero;

            // Nombre
            AddUIText(root, "Name", "M. Verstappen", TEXT_WHITE, 18,
                0.2f, 0.7f, 0.6f, 0.95f, TextAnchor.MiddleLeft);

            // Número
            AddUIText(root, "Number", "#1", ACCENT_RED, 22,
                0.7f, 0.85f, 0.6f, 0.95f, TextAnchor.MiddleCenter);

            // Nacionalidad
            AddUIText(root, "Nationality", "🇳🇱 Dutch", TEXT_GREY, 12,
                0.2f, 0.5f, 0.4f, 0.6f, TextAnchor.MiddleLeft);

            // Stats mini
            AddUIText(root, "Stats", "OVR: 95 | ⭐⭐⭐⭐⭐", TEXT_GREY, 12,
                0.2f, 0.7f, 0.05f, 0.35f, TextAnchor.MiddleLeft);

            // Salario
            AddUIText(root, "Salary", "$45M/año", ACCENT_GOLD, 13,
                0.7f, 0.98f, 0.05f, 0.35f, TextAnchor.MiddleRight);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateNewsItemPrefab()
        {
            string path = $"{PREFABS_PATH}/UI/Components/NewsItem.prefab";
            if (File.Exists(path)) return;

            GameObject root = new GameObject("NewsItem", typeof(RectTransform), typeof(Image));
            root.GetComponent<Image>().color = BG_CARD;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 80);

            // Type indicator bar
            GameObject typeBar = CreateChild(root, "TypeIndicator");
            Image typeImg = typeBar.AddComponent<Image>();
            typeImg.color = ACCENT_RED;
            RectTransform typeRt = typeBar.GetComponent<RectTransform>();
            typeRt.anchorMin = new Vector2(0, 0);
            typeRt.anchorMax = new Vector2(0.012f, 1);
            typeRt.offsetMin = Vector2.zero;
            typeRt.offsetMax = Vector2.zero;

            // Badge
            AddUIText(root, "TypeBadge", "🔴 URGENTE", ACCENT_RED, 10,
                0.03f, 0.3f, 0.7f, 0.95f, TextAnchor.MiddleLeft);

            // Headline
            AddUIText(root, "Headline", "Titular de la noticia",
                TEXT_WHITE, 14, 0.03f, 0.95f, 0.35f, 0.7f, TextAnchor.MiddleLeft);

            // Source
            AddUIText(root, "Source", "F1 Press — hace 2h",
                TEXT_GREY, 10, 0.03f, 0.7f, 0.05f, 0.3f, TextAnchor.MiddleLeft);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ══════════════════════════════════════════════════════
        // 7. BUILD SETTINGS
        // ══════════════════════════════════════════════════════

        private static void SetBuildSettings()
        {
            Debug.Log("[Setup] Configurando Build Settings...");

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene($"{SCENES_PATH}/Boot.unity", true),
                new EditorBuildSettingsScene($"{SCENES_PATH}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{SCENES_PATH}/Game.unity", true),
                new EditorBuildSettingsScene($"{SCENES_PATH}/Loading.unity", true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();

            // Player Settings para mobile
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            Debug.Log("  ✅ Build Settings configurados (Portrait, 4 escenas)");
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static void AddUIText(GameObject parent, string name,
            string text, Color color, int fontSize,
            float xMin, float xMax, float yMin, float yMax,
            TextAnchor anchor)
        {
            GameObject obj = CreateChild(parent, name);
            Text t = obj.AddComponent<Text>();
            t.text = text;
            t.color = color;
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddNote(GameObject obj, string note)
        {
            // Usar un componente dummy para dejar notas en el editor
            // (Se puede ver en el Inspector como un campo de texto)
            var noteComp = obj.AddComponent<EditorNote>();
            if (noteComp != null)
                noteComp.note = note;
        }
    }

    // ══════════════════════════════════════════════════════
    // COMPONENTE DE NOTA PARA EL EDITOR
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Componente auxiliar para dejar notas en GameObjects.
    /// Solo informativo, no hace nada en runtime.
    /// </summary>
    public class EditorNote : MonoBehaviour
    {
        [TextArea(2, 5)]
        public string note = "";
    }

    // ══════════════════════════════════════════════════════
    // DRAWER CUSTOM PARA EditorNote
    // ══════════════════════════════════════════════════════

    [CustomEditor(typeof(EditorNote))]
    public class EditorNoteDrawer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorNote note = (EditorNote)target;

            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(1f, 0.9f, 0.4f);
            EditorGUILayout.HelpBox(note.note, MessageType.Info);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(3);
            note.note = EditorGUILayout.TextArea(note.note, GUILayout.MinHeight(40));

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }
    }
}
#endif
