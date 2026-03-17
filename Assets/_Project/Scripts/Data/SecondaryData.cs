// ============================================================
// F1 Career Manager — LegacyData.cs, SaveData.cs, 
//                      RaceResultData.cs, SponsorData.cs, SeasonData.cs
// Modelos de datos secundarios
// ============================================================

using System;
using System.Collections.Generic;

namespace F1CareerManager.Data
{
    [Serializable]
    public class LegacyData
    {
        public int totalLegacyPoints;
        public int totalSeasons;
        public int constructorChampionships;
        public int driverChampionships;
        public int totalWins;
        public int totalPodiums;
        public int regensToChampion;
        public List<string> hallOfFameEntries;
        public List<string> memorableEvents;
    }

    [Serializable]
    public class SaveData
    {
        public string saveId;
        public string saveType;                // "AutoSave", "Manual", "Emergency"
        public int slotNumber;                 // 0=auto, 1-3=manual
        public string teamId;
        public int currentSeason;
        public int constructorPosition;
        public string realDateSaved;           // Fecha real del guardado
        public string carSpriteId;
        public SeasonData currentSeasonData;
        public List<SeasonData> pastSeasons;
        public LegacyData legacy;
        public List<PilotData> allPilots;
        public List<TeamData> allTeams;
        public List<StaffData> allStaff;
    }

    [Serializable]
    public class RaceResultData
    {
        public string circuitId;
        public int roundNumber;
        public int seasonNumber;
        public List<RacePositionData> finalPositions;
        public string fastestLapPilotId;
        public float fastestLapTime;
        public int totalOvertakes;
        public int safetyCars;
        public bool hadRain;
        public List<string> incidentDescriptions;
        public List<string> narratorSummary;
    }

    [Serializable]
    public class RacePositionData
    {
        public int position;
        public string pilotId;
        public string teamId;
        public int pitStops;
        public float gapToLeader;
        public int pointsEarned;
        public bool hasFastestLap;
        public bool dnf;
        public string dnfReason;
    }

    [Serializable]
    public class SponsorData
    {
        public string id;
        public string name;
        public string tier;                    // "Main", "Secondary", "Minor"
        public float annualPayment;
        public int contractYears;
        public int yearsRemaining;
        public int minimumConstructorPos;      // Si el equipo baja de aquí, se van
        public float performanceBonus;
        public bool isHappy;
    }

    [Serializable]
    public class SeasonData
    {
        public int seasonNumber;
        public int currentRound;
        public int totalRounds;
        public string driverChampionId;
        public string constructorChampionId;
        public List<RaceResultData> raceResults;
        public List<string> majorEvents;       // Eventos memorables de la temporada
        public string playerBestResult;        // "Victoria en Monza"
        public string playerPilotOfYear;
    }
}
