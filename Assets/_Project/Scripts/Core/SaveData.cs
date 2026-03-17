// ============================================================
// F1 Career Manager — SaveData.cs
// Estructura serializable para guardar el progreso completo
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Data;
using F1CareerManager.Academy;

namespace F1CareerManager.Core
{
    [Serializable]
    public class SaveData
    {
        // ── Información de Usuario ───────────────────────────
        public string playerName;
        public string teamId;
        public int currentSeason;
        public int currentWeek;
        public long budget;

        // ── Estado de Pilotos & Equipos ──────────────────────
        public List<PilotData> pilots;
        public List<TeamData> teams;

        // ── Academia Junior ──────────────────────────────────
        public List<AcademyManager.JuniorPilotInfo> academyPilots;
        public long academyBudget;

        // ── Relaciones & Rivalidades ─────────────────────────
        public Dictionary<string, int> relationshipMatrix; // pilot1_pilot2 -> int

        // ── Progreso de Juego ────────────────────────────────
        public List<string> unlockedAchievements;
        public Dictionary<string, int> challengeRating;
        
        // ── Historial de Temporadas ──────────────────────────
        public List<SeasonHistoryEntry> seasonHistory;

        // ── I+D (Research & Development) ─────────────────────
        public List<string> researchedComponentIds;
        public List<string> installedComponentIds;

        [Serializable]
        public class SeasonHistoryEntry
        {
            public int seasonNumber;
            public int constructorsPos;
            public string p1Name;
            public int p1Pos;
            public string p2Name;
            public int p2Pos;
            public long finalBudget;
        }

        public SaveData()
        {
            pilots = new List<PilotData>();
            teams = new List<TeamData>();
            academyPilots = new List<AcademyManager.JuniorPilotInfo>();
            relationshipMatrix = new Dictionary<string, int>();
            unlockedAchievements = new List<string>();
            seasonHistory = new List<SeasonHistoryEntry>();
            researchedComponentIds = new List<string>();
            installedComponentIds = new List<string>();
        }
    }
}
