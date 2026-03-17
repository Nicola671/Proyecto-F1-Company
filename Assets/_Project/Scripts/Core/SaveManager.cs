// ============================================================
// F1 Career Manager — SaveManager.cs
// 3 slots manuales + 1 autoguardado
// ============================================================
// DEPENDENCIAS: GameManager.cs, SaveData (SecondaryData.cs)
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using F1CareerManager.Data;

namespace F1CareerManager.Core
{
    /// <summary>
    /// Sistema de guardado: 3 slots manuales + 1 autosave.
    /// JSON serializado con encriptación básica XOR.
    /// Autoguardado post-carrera, fichajes, componentes, cierre.
    /// Migración de versión incluida.
    /// </summary>
    public class SaveManager
    {
        // ── Constantes ───────────────────────────────────────
        private const int MAX_MANUAL_SLOTS = 3;
        private const int AUTOSAVE_SLOT = 0;
        private const string SAVE_FOLDER = "saves";
        private const string SAVE_EXTENSION = ".f1save";
        private const int CURRENT_SAVE_VERSION = 1;
        private const string ENCRYPTION_KEY = "F1CM2025_S3CR3T";

        // ── Estado ───────────────────────────────────────────
        private string _savePath;
        private GameManager _gameManager;
        private bool _autoSaveEnabled = true;

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public SaveManager(GameManager gameManager)
        {
            _gameManager = gameManager;

            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            _savePath = Path.Combine(
                UnityEngine.Application.persistentDataPath, SAVE_FOLDER);
            #else
            _savePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "F1CareerManager", SAVE_FOLDER);
            #endif

            EnsureSaveDirectory();
        }

        // ══════════════════════════════════════════════════════
        // GUARDAR
        // ══════════════════════════════════════════════════════

        /// <summary>Guarda en un slot manual (1-3)</summary>
        public bool SaveToSlot(int slot)
        {
            if (slot < 1 || slot > MAX_MANUAL_SLOTS)
            {
                LogError($"Slot inválido: {slot}. Usar 1-{MAX_MANUAL_SLOTS}");
                return false;
            }

            return SaveInternal(slot, "Manual");
        }

        /// <summary>Autoguardado (slot 0)</summary>
        public bool AutoSave()
        {
            if (!_autoSaveEnabled) return false;
            return SaveInternal(AUTOSAVE_SLOT, "AutoSave");
        }

        /// <summary>Autoguardado de emergencia (al cerrar app)</summary>
        public bool EmergencySave()
        {
            return SaveInternal(AUTOSAVE_SLOT, "Emergency");
        }

        private bool SaveInternal(int slot, string saveType)
        {
            try
            {
                // Crear SaveData desde GameManager
                SaveData data = _gameManager.CreateSaveData(slot, saveType);
                data.saveVersion = CURRENT_SAVE_VERSION;
                data.realDateSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                data.weeksPlayed = GetWeeksPlayed();

                // Serializar
                string json = JsonParser.ToJson(data, true);

                // Encriptar
                string encrypted = EncryptXOR(json);

                // Escribir archivo
                string filePath = GetSlotPath(slot);
                File.WriteAllText(filePath, encrypted, Encoding.UTF8);

                Log($"Guardado exitoso: Slot {slot} ({saveType}) — {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error al guardar slot {slot}: {ex.Message}");
                return false;
            }
        }

        // ══════════════════════════════════════════════════════
        // CARGAR
        // ══════════════════════════════════════════════════════

        /// <summary>Carga un slot (0=auto, 1-3=manual)</summary>
        public SaveData LoadFromSlot(int slot)
        {
            string filePath = GetSlotPath(slot);

            if (!File.Exists(filePath))
            {
                Log($"Slot {slot} vacío — no hay archivo");
                return null;
            }

            try
            {
                string encrypted = File.ReadAllText(filePath, Encoding.UTF8);
                string json = DecryptXOR(encrypted);
                SaveData data = JsonParser.FromJson<SaveData>(json);

                if (data == null)
                {
                    LogError($"Error al deserializar slot {slot}");
                    return null;
                }

                // Migración de versión
                if (data.saveVersion < CURRENT_SAVE_VERSION)
                {
                    data = MigrateSave(data);
                    Log($"Save migrado de v{data.saveVersion} a v{CURRENT_SAVE_VERSION}");
                }

                Log($"Cargado: Slot {slot} — Temporada {data.currentSeason}");
                return data;
            }
            catch (Exception ex)
            {
                LogError($"Error al cargar slot {slot}: {ex.Message}");
                return null;
            }
        }

        // ══════════════════════════════════════════════════════
        // INFO DE SLOTS (para la UI de carga)
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene info resumida de todos los slots</summary>
        public List<SaveSlotInfo> GetAllSlotInfo()
        {
            var slots = new List<SaveSlotInfo>();

            // Slot 0 = Autoguardado
            slots.Add(GetSlotInfo(AUTOSAVE_SLOT));

            // Slots 1-3 = Manuales
            for (int i = 1; i <= MAX_MANUAL_SLOTS; i++)
                slots.Add(GetSlotInfo(i));

            return slots;
        }

        /// <summary>Info de un slot específico</summary>
        public SaveSlotInfo GetSlotInfo(int slot)
        {
            var info = new SaveSlotInfo
            {
                SlotNumber = slot,
                IsAutoSave = (slot == AUTOSAVE_SLOT),
                IsEmpty = true
            };

            string filePath = GetSlotPath(slot);
            if (!File.Exists(filePath)) return info;

            try
            {
                string encrypted = File.ReadAllText(filePath, Encoding.UTF8);
                string json = DecryptXOR(encrypted);
                SaveData data = JsonParser.FromJson<SaveData>(json);

                if (data != null)
                {
                    info.IsEmpty = false;
                    info.TeamId = data.teamId;
                    info.Season = data.currentSeason;
                    info.ConstructorPosition = data.constructorPosition;
                    info.DateSaved = data.realDateSaved;
                    info.WeeksPlayed = data.weeksPlayed;
                    info.SaveType = data.saveType;
                }
            }
            catch { /* slot corrupto, mantener vacío */ }

            return info;
        }

        // ══════════════════════════════════════════════════════
        // BORRADO
        // ══════════════════════════════════════════════════════

        /// <summary>Borra un slot</summary>
        public bool DeleteSlot(int slot)
        {
            string filePath = GetSlotPath(slot);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Log($"Slot {slot} borrado");
                return true;
            }
            return false;
        }

        /// <summary>¿Existe un autoguardado?</summary>
        public bool HasAutoSave()
        {
            return File.Exists(GetSlotPath(AUTOSAVE_SLOT));
        }

        // ══════════════════════════════════════════════════════
        // MIGRACIÓN
        // ══════════════════════════════════════════════════════

        private SaveData MigrateSave(SaveData data)
        {
            // v0 → v1: agregar campos nuevos
            if (data.saveVersion < 1)
            {
                // Los campos nuevos ya tienen defaults
                if (data.legacy == null)
                    data.legacy = new LegacyData();
                if (data.pastSeasons == null)
                    data.pastSeasons = new List<SeasonData>();
            }

            data.saveVersion = CURRENT_SAVE_VERSION;
            return data;
        }

        // ══════════════════════════════════════════════════════
        // ENCRIPTACIÓN XOR (básica, no criptográfica)
        // ══════════════════════════════════════════════════════

        private string EncryptXOR(string plainText)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);

            for (int i = 0; i < textBytes.Length; i++)
                textBytes[i] = (byte)(textBytes[i] ^ keyBytes[i % keyBytes.Length]);

            return Convert.ToBase64String(textBytes);
        }

        private string DecryptXOR(string cipherText)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);

            for (int i = 0; i < cipherBytes.Length; i++)
                cipherBytes[i] = (byte)(cipherBytes[i] ^ keyBytes[i % keyBytes.Length]);

            return Encoding.UTF8.GetString(cipherBytes);
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private string GetSlotPath(int slot)
        {
            string filename = slot == AUTOSAVE_SLOT
                ? $"autosave{SAVE_EXTENSION}"
                : $"save_slot{slot}{SAVE_EXTENSION}";
            return Path.Combine(_savePath, filename);
        }

        private void EnsureSaveDirectory()
        {
            if (!Directory.Exists(_savePath))
                Directory.CreateDirectory(_savePath);
        }

        private int GetWeeksPlayed()
        {
            return (_gameManager.CurrentSeason - 1) * 52 + _gameManager.CurrentRound;
        }

        public void SetAutoSaveEnabled(bool enabled)
        {
            _autoSaveEnabled = enabled;
        }

        private void Log(string msg)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            UnityEngine.Debug.Log($"[SaveManager] {msg}");
            #else
            Console.WriteLine($"[SaveManager] {msg}");
            #endif
        }

        private void LogError(string msg)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            UnityEngine.Debug.LogError($"[SaveManager] {msg}");
            #else
            Console.Error.WriteLine($"[SaveManager] ERROR: {msg}");
            #endif
        }
    }

    // ══════════════════════════════════════════════════════
    // INFO DE SLOT (para mostrar en UI)
    // ══════════════════════════════════════════════════════

    [Serializable]
    public class SaveSlotInfo
    {
        public int SlotNumber;
        public bool IsAutoSave;
        public bool IsEmpty;
        public string TeamId;
        public int Season;
        public int ConstructorPosition;
        public string DateSaved;
        public int WeeksPlayed;
        public string SaveType;
    }
}
