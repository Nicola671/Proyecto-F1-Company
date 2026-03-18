// ============================================================
// F1 Career Manager — EditorDiagnostics.cs
// Herramienta de diagnóstico de errores de compilación.
// Menú: F1 Manager > Diagnostics > Run Diagnostics
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace F1CareerManager.Editor
{
    public static class EditorDiagnostics
    {
        // ══════════════════════════════════════════════════════
        // MENÚ PRINCIPAL
        // ══════════════════════════════════════════════════════

        [MenuItem("F1 Manager/Diagnostics/Run Diagnostics %#d")]
        public static void RunDiagnostics()
        {
            Debug.Log("════════════════════════════════════════════════════");
            Debug.Log("    F1 CAREER MANAGER — DIAGNÓSTICO DE ERRORES");
            Debug.Log("════════════════════════════════════════════════════");

            var allLogs = GetAllCompilerMessages();

            if (allLogs.Count == 0)
            {
                Debug.Log("<color=green>✅ ¡Sin errores ni advertencias! El proyecto compila limpio.</color>");
                return;
            }

            // Separar por tipo
            var errors   = allLogs.Where(l => l.type == LogType.Error   || l.type == LogType.Exception).ToList();
            var warnings = allLogs.Where(l => l.type == LogType.Warning).ToList();

            PrintSection("🔴 ERRORES DE COMPILACIÓN", errors,   "<color=red>");
            PrintSection("🟡 ADVERTENCIAS",           warnings, "<color=yellow>");
            PrintGroupedByErrorCode(errors);
            PrintGroupedByFile(errors);

            Debug.Log($"════ RESUMEN: {errors.Count} errores | {warnings.Count} advertencias ════");
        }

        [MenuItem("F1 Manager/Diagnostics/Clear Console")]
        public static void ClearConsole()
        {
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntries != null)
            {
                var method = logEntries.GetMethod("Clear",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                method?.Invoke(null, null);
            }
            Debug.Log("[EditorDiagnostics] Consola limpiada.");
        }

        [MenuItem("F1 Manager/Diagnostics/Show Error Summary Only")]
        public static void ShowErrorSummaryOnly()
        {
            var allLogs = GetAllCompilerMessages();
            var errors  = allLogs.Where(l => l.type == LogType.Error || l.type == LogType.Exception).ToList();

            if (errors.Count == 0)
            {
                Debug.Log("<color=green>✅ Sin errores de compilación.</color>");
                return;
            }

            PrintGroupedByErrorCode(errors);
        }

        // ══════════════════════════════════════════════════════
        // HELPERS: LECTURA DE LOGS
        // ══════════════════════════════════════════════════════

        private struct LogEntry
        {
            public string message;
            public string stackTrace;
            public LogType type;
        }

        private static List<LogEntry> GetAllCompilerMessages()
        {
            var result = new List<LogEntry>();

            // Usamos la API interna de Unity para leer todas las entradas del log
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntries == null)
            {
                Debug.LogWarning("[EditorDiagnostics] No se pudo acceder a LogEntries internos de Unity.");
                return result;
            }

            var startMethod = logEntries.GetMethod("StartGettingEntries",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var endMethod = logEntries.GetMethod("EndGettingEntries",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var getCountMethod = logEntries.GetMethod("GetCount",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var getEntryMethod = logEntries.GetMethod("GetEntryInternal",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            if (startMethod == null || endMethod == null || getCountMethod == null || getEntryMethod == null)
            {
                // Fallback: sólo analizamos los logs conocidos
                Debug.LogWarning("[EditorDiagnostics] API interna de Unity no disponible. Usá el método alternativo.");
                return GetFallbackMessages();
            }

            startMethod.Invoke(null, null);
            int count = (int)getCountMethod.Invoke(null, null);

            var logEntryType = System.Type.GetType("UnityEditor.LogEntry, UnityEditor");
            if (logEntryType == null) { endMethod.Invoke(null, null); return result; }

            for (int i = 0; i < count; i++)
            {
                var entry = System.Activator.CreateInstance(logEntryType);
                object[] args = { i, entry };
                getEntryMethod.Invoke(null, args);
                entry = args[1];

                var msgField   = logEntryType.GetField("message");
                var modeField  = logEntryType.GetField("mode");

                if (msgField == null) continue;

                string msg  = msgField.GetValue(entry) as string ?? "";
                int    mode = modeField != null ? (int)modeField.GetValue(entry) : 0;

                // Filtrar solo entradas de compilación (contienen CS#### o error/warning)
                if (!msg.Contains("CS") && !msg.Contains("error") && !msg.Contains("warning")) continue;

                LogType logType = LogType.Log;
                if ((mode & 1) != 0)  logType = LogType.Error;
                if ((mode & 4) != 0)  logType = LogType.Warning;
                if ((mode & 128) != 0) logType = LogType.Exception;

                result.Add(new LogEntry { message = msg, type = logType });
            }

            endMethod.Invoke(null, null);
            return result;
        }

        /// <summary>
        /// Fallback: muestra instrucciones cuando la API interna no está disponible.
        /// </summary>
        private static List<LogEntry> GetFallbackMessages()
        {
            Debug.Log("[EditorDiagnostics] Para ver errores agrupados: compila el proyecto primero " +
                      "y luego ejecutá este diagnóstico.");
            return new List<LogEntry>();
        }

        // ══════════════════════════════════════════════════════
        // HELPERS: IMPRESIÓN
        // ══════════════════════════════════════════════════════

        private static void PrintSection(string title, List<LogEntry> entries, string colorTag)
        {
            if (entries.Count == 0) return;

            Debug.Log($"\n{colorTag}{title} ({entries.Count})</color>");
            Debug.Log("─────────────────────────────────────────────────────");

            foreach (var entry in entries)
            {
                Debug.Log($"{colorTag}  • {TrimMessage(entry.message)}</color>");
            }
        }

        private static void PrintGroupedByErrorCode(List<LogEntry> errors)
        {
            if (errors.Count == 0) return;

            var byCode = new Dictionary<string, List<string>>();

            foreach (var e in errors)
            {
                string code = ExtractErrorCode(e.message);
                if (!byCode.ContainsKey(code))
                    byCode[code] = new List<string>();
                byCode[code].Add(TrimMessage(e.message));
            }

            Debug.Log("\n<color=cyan>📊 ERRORES AGRUPADOS POR CÓDIGO:</color>");
            Debug.Log("─────────────────────────────────────────────────────");

            // Ordenar: primero los más frecuentes
            foreach (var kvp in byCode.OrderByDescending(x => x.Value.Count))
            {
                string codeLabel = kvp.Key == "OTHER" ? "Otros" : kvp.Key;
                Debug.Log($"<color=cyan>  [{codeLabel}] — {kvp.Value.Count} ocurrencia(s):</color>");
                foreach (var msg in kvp.Value.Take(3)) // max 3 por código
                    Debug.Log($"      {msg}");
                if (kvp.Value.Count > 3)
                    Debug.Log($"      ... y {kvp.Value.Count - 3} más");
            }
        }

        private static void PrintGroupedByFile(List<LogEntry> errors)
        {
            if (errors.Count == 0) return;

            var byFile = new Dictionary<string, int>();

            foreach (var e in errors)
            {
                string file = ExtractFileName(e.message);
                if (!byFile.ContainsKey(file))
                    byFile[file] = 0;
                byFile[file]++;
            }

            Debug.Log("\n<color=magenta>📁 ARCHIVOS CON MÁS ERRORES (prioridad de resolución):</color>");
            Debug.Log("─────────────────────────────────────────────────────");

            foreach (var kvp in byFile.OrderByDescending(x => x.Value))
            {
                Debug.Log($"<color=magenta>  {kvp.Key}</color> — {kvp.Value} error(es)");
            }
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private static string ExtractErrorCode(string message)
        {
            // Busca patrón como CS0104, CS0426, etc.
            int csIdx = message.IndexOf("CS");
            if (csIdx >= 0 && csIdx + 6 <= message.Length)
            {
                string candidate = message.Substring(csIdx, 6);
                if (candidate.Length == 6 && char.IsDigit(candidate[2]))
                    return candidate;
            }
            return "OTHER";
        }

        private static string ExtractFileName(string message)
        {
            // Busca patrón Scripts\...\Archivo.cs(línea,col)
            int scriptsIdx = message.IndexOf("Scripts\\");
            if (scriptsIdx < 0) scriptsIdx = message.IndexOf("Scripts/");
            if (scriptsIdx < 0) return "Desconocido";

            string sub = message.Substring(scriptsIdx + 8);
            int parenIdx = sub.IndexOf('(');
            if (parenIdx > 0) sub = sub.Substring(0, parenIdx);

            return sub.Trim();
        }

        private static string TrimMessage(string message)
        {
            // Limitar a 120 chars para legibilidad
            if (message.Length > 120)
                return message.Substring(0, 120) + "...";
            return message;
        }
    }
}
#endif
