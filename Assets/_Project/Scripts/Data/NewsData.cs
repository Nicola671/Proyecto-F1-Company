// ============================================================
// F1 Career Manager — NewsData.cs
// Modelo de datos de noticias y prensa
// ============================================================

using System;
using System.Collections.Generic;

namespace F1CareerManager.Data
{
    [Serializable]
    public class NewsData
    {
        // ── Noticia ──────────────────────────────────────────
        public string id;                      // ID único
        public string headline;                // Titular principal
        public string body;                    // Cuerpo corto de la noticia
        public string type;                    // "PostRaceHeadline", "TransferRumor", etc.
        public string mediaOutlet;             // "PitWall Post", "Formula Insider", etc.
        public string mediaIcon;               // Sprite del medio
        public int seasonNumber;               // Temporada en la que ocurre
        public int weekNumber;                 // Semana de la temporada
        public string relatedCircuit;          // Circuito relacionado (si aplica)
        public List<string> relatedPilotIds;   // Pilotos mencionados
        public List<string> relatedTeamIds;    // Equipos mencionados
        public bool isRumor;                   // Si es un rumor
        public bool isTrue;                    // Si el rumor es verdadero (oculto)
        public float credibility;              // 0-1 qué tan creíble es
        public bool hasBeenRead;               // Si el jugador ya la leyó
    }

    [Serializable]
    public class MediaOutletData
    {
        public string id;                      // "pitwall_post"
        public string name;                    // "PitWall Post"
        public string bias;                    // "Technical", "Sensationalist", etc.
        public string description;             // "Cobertura técnica y objetiva"
        public float rumorAccuracy;            // 0-1 (qué % de sus rumores son verdad)
        public string style;                   // Tono de escritura
        public string spriteId;                // Icono del medio
    }

    [Serializable]
    public class PressConferenceData
    {
        public string id;
        public string context;                 // "Post-race", "Pre-season", "Scandal"
        public int seasonNumber;
        public int weekNumber;
        public string triggerEvent;            // Qué disparó la rueda de prensa
        public List<PressQuestionData> questions;
    }

    [Serializable]
    public class PressQuestionData
    {
        public string questionText;            // La pregunta del periodista
        public string askedByMedia;            // Qué medio la hace
        public List<PressAnswerOption> options; // 4 opciones de respuesta
    }

    [Serializable]
    public class PressAnswerOption
    {
        public string label;                   // "A", "B", "C", "D"
        public string answerText;              // Texto de la respuesta
        public string pressEffect;             // "PressNeutral", "PressPositive", etc.
        public int pilotMoodChange;            // Cambio en humor del piloto (-20 a +10)
        public int fiaAttentionChange;         // Cambio en vigilancia FIA (0 a +20)
        public int rumorChanceModifier;        // Cambio en probabilidad de rumores
        public string consequence;             // Descripción de la consecuencia
    }
}
