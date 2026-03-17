// ============================================================
// F1 Career Manager — NewsGenerator.cs
// IA 6 — Generación de noticias con plantillas
// ============================================================
// DEPENDENCIAS: EventBus.cs, NewsData.cs, PilotData.cs,
//               TeamData.cs, StaffManager.cs
// EVENTOS QUE DISPARA: OnNewsGenerated
// CALLABLE DESDE: GameManager post-carrera y semanalmente
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.AI.PressAI
{
    /// <summary>Tipo de tarjeta de noticia</summary>
    public enum NewsCardType
    {
        Urgent,     // 🔴 Requiere acción del jugador
        Important,  // 🟡 Importante, no requiere acción
        News,       // 🔵 Noticia regular
        Rumor,      // ⚪ Rumor (puede ser falso)
        Positive,   // 🟢 Buenas noticias
        Rival       // 🟣 Noticias de rivales
    }

    /// <summary>Medio de comunicación ficticio con sesgo</summary>
    public class MediaOutlet
    {
        public string Name;
        public string Bias;        // "Neutral", "Sensationalist", "Technical", etc.
        public float Credibility;  // 0-1
        public string Style;       // Estilo de escritura
    }

    /// <summary>Noticia generada</summary>
    public class GeneratedNews
    {
        public string NewsId;
        public NewsCardType CardType;
        public string Headline;
        public string Body;
        public string MediaSource;
        public string MediaBias;
        public float Credibility;
        public bool RequiresAction;
        public bool IsRead;
        public int Week;
        public int Season;
        public List<string> RelatedPilotIds;
        public List<string> RelatedTeamIds;
        public int MoodEffect;       // Efecto en humor de pilotos mencionados
        public int DurationWeeks;    // Semanas que dura visible
    }

    /// <summary>
    /// Genera noticias usando plantillas dinámicas.
    /// 8 medios ficticios con sesgo distinto.
    /// 200+ plantillas de titulares.
    /// Genera contenido post-carrera y semanal.
    /// </summary>
    public class NewsGenerator
    {
        // ── Datos ────────────────────────────────────────────
        private EventBus _eventBus;
        private Random _rng;
        private List<GeneratedNews> _allNews;

        // ── 8 Medios ficticios ───────────────────────────────
        private static readonly MediaOutlet[] MEDIA_OUTLETS = new MediaOutlet[]
        {
            new MediaOutlet { Name = "PitWall Post", Bias = "Neutral",
                Credibility = 0.85f, Style = "Objetivo y balanceado" },
            new MediaOutlet { Name = "Formula Insider", Bias = "Sensationalist",
                Credibility = 0.55f, Style = "Dramático y exagerado" },
            new MediaOutlet { Name = "GrandPrix Weekly", Bias = "Technical",
                Credibility = 0.90f, Style = "Análisis técnico detallado" },
            new MediaOutlet { Name = "Paddock Rumors", Bias = "Gossip",
                Credibility = 0.40f, Style = "Chismes y filtraciones" },
            new MediaOutlet { Name = "Racing Tribune", Bias = "Critical",
                Credibility = 0.75f, Style = "Análisis crítico, no perdona" },
            new MediaOutlet { Name = "F1 Analytics", Bias = "Statistical",
                Credibility = 0.92f, Style = "Basado en datos y estadísticas" },
            new MediaOutlet { Name = "Motorsport Daily", Bias = "Sporty",
                Credibility = 0.80f, Style = "Deportivo y entusiasta" },
            new MediaOutlet { Name = "Grid Talk", Bias = "FanOriented",
                Credibility = 0.60f, Style = "Para fans, opiniones fuertes" }
        };

        // ══════════════════════════════════════════════════════
        // PLANTILLAS — POST-CARRERA (80+)
        // ══════════════════════════════════════════════════════

        // {0}=piloto ganador, {1}=equipo ganador, {2}=circuito
        private static readonly string[] WINNER_HEADLINES = {
            "¡{0} domina en {2} y se lleva la victoria!",
            "{0} logra una actuación magistral en {2}",
            "Victoria de {0}: {1} celebra un fin de semana perfecto",
            "Incontestable: {0} arrasa en el GP de {2}",
            "{0} cruza primero la meta en {2} con autoridad",
            "¡Qué carrera! {0} con {1} se impone en {2}",
            "El GP de {2} es para {0}: victoria nro. {3}",
            "{1} brilla en {2}: {0} sube al escalón más alto",
            "Sin rivales: {0} gana con margen en {2}",
            "La consistencia de {0} le da la victoria en {2}",
            "{0} hace valer su clase en {2}",
            "Tremenda victoria de {0} para {1} en {2}",
            "¡Histórico! {0} gana en {2} contra todo pronóstico",
            "{0} aprovecha errores rivales y gana en {2}",
            "Gran estrategia de {1}: {0} vence en {2}"
        };

        // {0}=piloto, {1}=equipo
        private static readonly string[] PODIUM_HEADLINES = {
            "{0} sube al podio: gran resultado para {1}",
            "Podio para {0}, {1} suma puntos valiosos",
            "{0} pelea hasta el final y termina en el podio",
            "Excelente resultado: {0} se mete entre los tres primeros",
            "El esfuerzo de {0} da frutos: podio en casa"
        };

        // {0}=piloto, {1}=equipo, {2}=posición
        private static readonly string[] POOR_RESULT_HEADLINES = {
            "Decepción para {1}: {0} termina P{2}",
            "Fin de semana para olvidar: {0} fuera del top 10",
            "{0} no encuentra el ritmo — {1} en problemas",
            "Carrera gris de {0}: P{2} en un circuito que debía favorecer a {1}",
            "¿Qué le pasa a {0}? Otro resultado pobre para {1}",
            "La frustración crece en {1}: {0} solo alcanza P{2}"
        };

        // {0}=piloto
        private static readonly string[] DNF_HEADLINES = {
            "¡Abandono! {0} se retira con problemas mecánicos",
            "Motor roto: el día de {0} termina antes de tiempo",
            "{0} no termina la carrera — incidente en pista",
            "Drama para {0}: fallo crítico lo deja fuera",
            "Día negro de {0}: abandono y puntos perdidos"
        };

        // {0}=piloto1, {1}=piloto2, {2}=equipo
        private static readonly string[] TEAMMATE_BATTLE_HEADLINES = {
            "Guerra interna en {2}: {0} supera a {1}",
            "Tensión en el garaje de {2}: {0} vs {1}",
            "La rivalidad dentro de {2} se intensifica",
            "{0} deja claro quién manda en {2}: supera a {1}",
            "¿Problemas en {2}? {0} y {1} cada vez más distanciados"
        };

        // {0}=equipo del jugador, {1}=posición constructor
        private static readonly string[] PLAYER_TEAM_ANALYSIS = {
            "Análisis: ¿Está {0} donde debería estar? Actualmente P{1}",
            "Semáforo verde para {0}: las cosas van según lo planeado",
            "Luces y sombras en el proyecto {0}: la temporada se complica",
            "¿Es este el verdadero nivel de {0}? P{1} en constructores",
            "Expertos evalúan a {0}: \"P{1} refleja su inversión en R&D\""
        };

        // ══════════════════════════════════════════════════════
        // PLANTILLAS — ENTRE CARRERAS (70+)
        // ══════════════════════════════════════════════════════

        // {0}=piloto, {1}=equipo
        private static readonly string[] TRANSFER_RUMOR_HEADLINES = {
            "RUMOR: {0} negocia con {1} para la próxima temporada",
            "Fuentes cercanas: {0} podría dejar su equipo y unirse a {1}",
            "¿{0} a {1}? Los rumores cobran fuerza en el paddock",
            "Mercado caliente: {1} apunta a {0} como refuerzo",
            "Se filtra interés de {1} en {0}: oferta millonaria",
            "¿Bomba en el mercado? {0} y {1} habrían tenido reunión secreta",
            "Representantes de {0} exploran opciones: {1} interesado",
            "El futuro de {0} incierto: {1} ofrece proyecto ambicioso"
        };

        // {0}=equipo, {1}=área
        private static readonly string[] RND_HEADLINES = {
            "{0} promete un gran paso adelante en {1}",
            "Según fuentes, {0} prepara una actualización radical en {1}",
            "El departamento de {1} de {0} trabaja a ritmo frenético",
            "¿Salto de rendimiento? {0} trae novedades en {1} para la próxima carrera",
            "Expertos detectan cambios sutiles en el {1} de {0}"
        };

        // {0}=equipo
        private static readonly string[] FIA_HEADLINES = {
            "La FIA investiga a {0} por posible irregularidad técnica",
            "Escrutinio técnico: {0} bajo la lupa de la FIA",
            "¿Componente ilegal? La FIA abre investigación sobre {0}",
            "Tensión en el paddock: {0} podría enfrentar sanción FIA",
            "La FIA emite advertencia a {0} tras inspección post-carrera"
        };

        // {0}=piloto
        private static readonly string[] PILOT_STATE_HEADLINES = {
            "{0} se muestra optimista: \"El equipo está mejorando\"",
            "Preocupación por el estado anímico de {0}",
            "{0}: \"Necesitamos tomar mejores decisiones estratégicas\"",
            "Fuentes internas: {0} frustrado con el rendimiento del auto",
            "{0} entrena duro entre carreras: busca mejorar su ritmo",
            "¿Está {0} rindiendo al máximo? Análisis de sus últimas actuaciones",
            "{0} habla sobre su futuro: \"Mi prioridad es ganar\"",
            "La confianza de {0} crece tras recientes resultados"
        };

        // Genéricas de F1
        private static readonly string[] WORLD_F1_HEADLINES = {
            "Cambios reglamentarios se debaten en la última reunión de la FIA",
            "Presupuesto cap bajo escrutinio: ¿se reduce para la próxima temporada?",
            "Pirelli anuncia nueva gama de compuestos para el próximo año",
            "La F1 confirma nuevo calendario con circuito sorpresa",
            "CEO de la F1: \"El deporte está en su mejor momento\"",
            "Debate: ¿Son las carreras sprint buenas para la F1?",
            "Nuevo circuito urbano propuesto: la F1 podría correr en nuevas calles",
            "Record de audiencia: la F1 alcanza cifras históricas de espectadores",
            "Sostenibilidad: la F1 avanza hacia combustibles 100% renovables",
            "Reunión de directores de equipo: se discuten los coches de próxima generación"
        };

        // ══════════════════════════════════════════════════════
        // PLANTILLAS — EMOCIONALES/DRAMA (50+)
        // ══════════════════════════════════════════════════════

        // {0}=piloto, {1}=equipo
        private static readonly string[] PRAISE_HEADLINES = {
            "\"El mejor piloto de la grilla actualmente\" — Expertos elogian a {0}",
            "{0} recibe el premio al Piloto del Día por tercera vez consecutiva",
            "La prensa internacional se rinde ante la actuación de {0}",
            "El ascenso de {0}: de promesa a referencia en {1}",
            "¡Impresionante! {0} rompe récord de vueltas rápidas esta temporada"
        };

        // {0}=piloto, {1}=equipo
        private static readonly string[] CRITICISM_HEADLINES = {
            "\"¿Está {0} justificando su salario?\" — Columnista cuestiona rendimiento",
            "Críticas al liderazgo de {0} dentro de {1}",
            "\"Hay que tomar decisiones\" — Ex-piloto critica la dirección de {1}",
            "El problema de {1} tiene nombre: la estrategia no funciona",
            "Presión creciente sobre {0}: resultados decepcionantes"
        };

        // {0}=piloto1, {1}=piloto2
        private static readonly string[] CONTROVERSY_HEADLINES = {
            "Polémica en pista: {0} y {1} protagonizan incidente tenso",
            "\"Fue su culpa\" — {0} señala a {1} tras toque en carrera",
            "Tensión entre {0} y {1}: ¿Se les fue de las manos?",
            "La FIA revisa el incidente entre {0} y {1}: posible sanción",
            "Redes sociales explotaron: el encontronazo {0}-{1} divide opiniones"
        };

        // {0}=equipo
        private static readonly string[] TEAM_CRISIS_HEADLINES = {
            "Crisis en {0}: internamente reconocen que \"algo debe cambiar\"",
            "Fuentes de {0}: \"Hay descontento generalizado en la fábrica\"",
            "¿Toca fondo {0}? La peor racha en años del equipo",
            "Restructuración en {0}: cambios en el área técnica",
            "El presupuesto de {0} bajo presión: recortes a la vista"
        };

        // {0}=piloto, {1}=equipo
        private static readonly string[] INJURY_HEADLINES = {
            "URGENTE: {0} sufre lesión — ausencia de varias carreras",
            "Parte médico: {0} deberá perderse las próximas carreras",
            "Preocupación en {1}: {0} no estará disponible temporalmente",
            "Lesión de {0}: el equipo busca piloto sustituto",
            "{1} confirma que {0} será baja por lesión"
        };

        // ══════════════════════════════════════════════════════
        // PLANTILLAS — EXTRAS (30+)
        // ══════════════════════════════════════════════════════

        private static readonly string[] SPONSOR_HEADLINES = {
            "POSITIVO: Nuevo sponsor interesado en el equipo — ingresos extra",
            "Gran acuerdo comercial: patrocinador premium se suma al proyecto",
            "El éxito en pista atrae inversores: nueva oferta de sponsorship recibida",
            "Sponsor principal evalúa su continuidad: resultados por debajo de expectativas",
            "Mercado de sponsors: la competencia por el mejor postor se intensifica"
        };

        private static readonly string[] CONTRACT_HEADLINES = {
            "{0} renueva con {1} por {2} temporadas más",
            "OFICIAL: {0} firma contrato con {1}",
            "Acuerdo cerrado: {0} será piloto de {1} la próxima temporada",
            "¡Bomba en el mercado! {0} deja su equipo y firma con {1}",
            "Confirmado: {0} y {1} llegan a acuerdo por {2} años"
        };

        private static readonly string[] SEASON_START_HEADLINES = {
            "¡Arranca la temporada! ¿Quién será campeón este año?",
            "Predicciones: los expertos eligen a sus favoritos para el título",
            "Se encienden los motores: la F1 vuelve a la acción",
            "Pretemporada: ¿Quién mostró mejor ritmo en los tests?",
            "Nueva temporada, nuevas esperanzas: el paddock bulle de actividad"
        };

        private static readonly string[] SEASON_END_HEADLINES = {
            "Fin de temporada: balance de una campaña intensa",
            "Las notas de fin de curso: ¿Quién aprobó y quién suspendió?",
            "Resumen de temporada: victorias, derrotas y sorpresas",
            "La temporada en números: estadísticas que cuentan la historia",
            "Adiós a una temporada memorable: lo mejor y lo peor"
        };

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public NewsGenerator(Random rng = null)
        {
            _eventBus = EventBus.Instance;
            _rng = rng ?? new Random();
            _allNews = new List<GeneratedNews>();
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN POST-CARRERA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera todas las noticias post-carrera.
        /// Llamar desde GameManager después de cada carrera.
        /// </summary>
        /// <returns>Lista de 3-6 noticias generadas</returns>
        public List<GeneratedNews> GeneratePostRaceNews(
            List<PilotData> racePositions, string circuitName,
            TeamData playerTeam, List<TeamData> allTeams,
            int week, int season)
        {
            var news = new List<GeneratedNews>();

            if (racePositions.Count == 0) return news;

            // 1. Titular del ganador (siempre)
            var winner = racePositions[0];
            var winnerTeam = allTeams.Find(t => t.id == winner.currentTeamId);
            news.Add(CreateNews(
                NewsCardType.News,
                FillTemplate(WINNER_HEADLINES, winner.lastName,
                    winnerTeam?.shortName ?? "???", circuitName,
                    winner.totalWins.ToString()),
                $"Con una actuación sólida, {winner.firstName} {winner.lastName} " +
                $"se lleva la victoria en el GP de {circuitName}.",
                "Motorsport Daily", week, season,
                new List<string> { winner.id },
                new List<string> { winner.currentTeamId },
                0, 2));

            // 2. Análisis del equipo del jugador
            if (playerTeam != null)
            {
                news.Add(CreateNews(
                    NewsCardType.News,
                    FillTemplate(PLAYER_TEAM_ANALYSIS,
                        playerTeam.shortName,
                        playerTeam.constructorPosition.ToString()),
                    GeneratePlayerTeamBody(racePositions, playerTeam),
                    SelectMediaForBias("Neutral"), week, season,
                    new List<string>(),
                    new List<string> { playerTeam.id },
                    0, 1));
            }

            // 3. Si hubo DNFs notables
            var dnfs = racePositions.FindAll(p => p.isInjured || p.bestFinish == 0);
            if (dnfs.Count > 0)
            {
                var dnfPilot = dnfs[_rng.Next(dnfs.Count)];
                news.Add(CreateNews(
                    NewsCardType.Important,
                    FillTemplate(DNF_HEADLINES, dnfPilot.lastName),
                    $"{dnfPilot.firstName} {dnfPilot.lastName} no pudo completar la carrera.",
                    "PitWall Post", week, season,
                    new List<string> { dnfPilot.id },
                    new List<string> { dnfPilot.currentTeamId },
                    -5, 1));
            }

            // 4. Resultado pobre si piloto del jugador fuera del top 10
            if (playerTeam != null)
            {
                var playerPilots = racePositions.FindAll(p =>
                    p.currentTeamId == playerTeam.id);
                foreach (var pp in playerPilots)
                {
                    int idx = racePositions.IndexOf(pp);
                    if (idx > 9) // Fuera del top 10
                    {
                        news.Add(CreateNews(
                            NewsCardType.News,
                            FillTemplate(POOR_RESULT_HEADLINES,
                                pp.lastName, playerTeam.shortName,
                                (idx + 1).ToString()),
                            $"Carrera para olvidar de {pp.firstName} {pp.lastName}.",
                            SelectMediaForBias("Critical"), week, season,
                            new List<string> { pp.id },
                            new List<string> { playerTeam.id },
                            -3, 1));
                        break; // Solo una noticia de resultado pobre
                    }
                }
            }

            // 5. Batalla entre compañeros (si hay diferencia grande)
            foreach (var team in allTeams)
            {
                var teamPilots = racePositions.FindAll(p =>
                    p.currentTeamId == team.id);
                if (teamPilots.Count >= 2)
                {
                    int pos1 = racePositions.IndexOf(teamPilots[0]);
                    int pos2 = racePositions.IndexOf(teamPilots[1]);
                    if (Math.Abs(pos1 - pos2) >= 8 && (float)_rng.NextDouble() < 0.4f)
                    {
                        var ahead = pos1 < pos2 ? teamPilots[0] : teamPilots[1];
                        var behind = pos1 < pos2 ? teamPilots[1] : teamPilots[0];
                        news.Add(CreateNews(
                            NewsCardType.Rival,
                            FillTemplate(TEAMMATE_BATTLE_HEADLINES,
                                ahead.lastName, behind.lastName, team.shortName),
                            $"Gran diferencia entre los pilotos de {team.shortName}: " +
                            $"{ahead.lastName} terminó muy por delante de {behind.lastName}.",
                            "Formula Insider", week, season,
                            new List<string> { ahead.id, behind.id },
                            new List<string> { team.id },
                            -8, 2));
                        break;
                    }
                }
            }

            // Guardar y disparar eventos
            foreach (var n in news)
            {
                _allNews.Add(n);
                FireNewsEvent(n);
            }

            return news;
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN SEMANAL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera noticias semanales entre carreras.
        /// 30% de chance de generar 1-2 noticias por semana.
        /// </summary>
        public List<GeneratedNews> GenerateWeeklyNews(
            List<PilotData> allPilots, List<TeamData> allTeams,
            TeamData playerTeam, int week, int season)
        {
            var news = new List<GeneratedNews>();

            // 30% de chance de noticia normal
            if ((float)_rng.NextDouble() < 0.30f)
            {
                // Elegir tipo aleatorio
                int type = _rng.Next(0, 5);
                switch (type)
                {
                    case 0: // Rumor de transferencia
                        news.Add(GenerateTransferRumor(allPilots, allTeams, week, season));
                        break;
                    case 1: // Noticia de R&D
                        news.Add(GenerateRnDNews(allTeams, week, season));
                        break;
                    case 2: // Estado de piloto
                        news.Add(GeneratePilotStateNews(allPilots, week, season));
                        break;
                    case 3: // Noticia de F1 mundial
                        news.Add(GenerateWorldF1News(week, season));
                        break;
                    case 4: // Elogio o crítica
                        news.Add(GeneratePraiseOrCriticism(allPilots, allTeams, week, season));
                        break;
                }
            }

            // 15% de chance de una segunda noticia
            if ((float)_rng.NextDouble() < 0.15f)
            {
                news.Add(GenerateWorldF1News(week, season));
            }

            // Filtrar nulls
            news.RemoveAll(n => n == null);

            foreach (var n in news)
            {
                _allNews.Add(n);
                FireNewsEvent(n);
            }

            return news;
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN ESPECÍFICA
        // ══════════════════════════════════════════════════════

        /// <summary>Genera noticia por lesión</summary>
        public GeneratedNews GenerateInjuryNews(PilotData pilot,
            string severity, int racesOut, int week, int season)
        {
            var teamId = pilot.currentTeamId ?? "";
            var n = CreateNews(
                NewsCardType.Urgent,
                FillTemplate(INJURY_HEADLINES, pilot.lastName,
                    teamId),
                $"Parte médico: {pilot.firstName} {pilot.lastName} " +
                $"sufre {severity.ToLower()} y estará fuera {racesOut} carreras.",
                "PitWall Post", week, season,
                new List<string> { pilot.id },
                new List<string> { teamId },
                -10, 3);

            n.RequiresAction = true;
            _allNews.Add(n);
            FireNewsEvent(n);
            return n;
        }

        /// <summary>Genera noticia por fichaje</summary>
        public GeneratedNews GenerateTransferNews(string pilotName,
            string fromTeam, string toTeam, int years,
            int week, int season)
        {
            var n = CreateNews(
                NewsCardType.Important,
                FillTemplate(CONTRACT_HEADLINES, pilotName, toTeam,
                    years.ToString()),
                $"{pilotName} firma con {toTeam} por {years} temporadas. " +
                (string.IsNullOrEmpty(fromTeam) ? "Viene como agente libre." :
                    $"Deja {fromTeam} tras negociaciones."),
                "Motorsport Daily", week, season,
                new List<string>(), new List<string>(),
                0, 2);

            _allNews.Add(n);
            FireNewsEvent(n);
            return n;
        }

        /// <summary>Genera noticia por sanción FIA</summary>
        public GeneratedNews GenerateFIASanctionNews(string teamName,
            string sanctionDescription, int week, int season)
        {
            var n = CreateNews(
                NewsCardType.Urgent,
                FillTemplate(FIA_HEADLINES, teamName),
                sanctionDescription,
                "PitWall Post", week, season,
                new List<string>(), new List<string>(),
                0, 3);

            n.RequiresAction = true;
            _allNews.Add(n);
            FireNewsEvent(n);
            return n;
        }

        // ══════════════════════════════════════════════════════
        // GENERADORES INTERNOS
        // ══════════════════════════════════════════════════════

        private GeneratedNews GenerateTransferRumor(List<PilotData> pilots,
            List<TeamData> teams, int week, int season)
        {
            if (pilots.Count < 2 || teams.Count < 2) return null;

            var pilot = pilots[_rng.Next(pilots.Count)];
            var team = teams[_rng.Next(teams.Count)];
            while (team.id == pilot.currentTeamId && teams.Count > 1)
                team = teams[_rng.Next(teams.Count)];

            return CreateNews(
                NewsCardType.Rumor,
                FillTemplate(TRANSFER_RUMOR_HEADLINES,
                    pilot.lastName, team.shortName),
                $"Según fuentes en el paddock, existe interés mutuo entre " +
                $"{pilot.firstName} {pilot.lastName} y {team.shortName}.",
                "Paddock Rumors", week, season,
                new List<string> { pilot.id },
                new List<string> { team.id },
                -3, 3);
        }

        private GeneratedNews GenerateRnDNews(List<TeamData> teams,
            int week, int season)
        {
            if (teams.Count == 0) return null;
            var team = teams[_rng.Next(teams.Count)];
            string[] areas = { "aerodinámica", "motor", "chasis", "fiabilidad" };
            string area = areas[_rng.Next(areas.Length)];

            return CreateNews(
                NewsCardType.Rival,
                FillTemplate(RND_HEADLINES, team.shortName, area),
                $"El departamento de {area} de {team.shortName} estaría " +
                "preparando mejoras significativas.",
                "GrandPrix Weekly", week, season,
                new List<string>(),
                new List<string> { team.id },
                0, 2);
        }

        private GeneratedNews GeneratePilotStateNews(List<PilotData> pilots,
            int week, int season)
        {
            if (pilots.Count == 0) return null;
            var pilot = pilots[_rng.Next(pilots.Count)];

            return CreateNews(
                NewsCardType.News,
                FillTemplate(PILOT_STATE_HEADLINES, pilot.lastName),
                $"Actualización sobre {pilot.firstName} {pilot.lastName}: " +
                $"su estado de ánimo es {pilot.mood.ToLower()}, forma al {pilot.formCurrent}%.",
                SelectMediaForBias("Neutral"), week, season,
                new List<string> { pilot.id },
                new List<string> { pilot.currentTeamId ?? "" },
                0, 1);
        }

        private GeneratedNews GenerateWorldF1News(int week, int season)
        {
            return CreateNews(
                NewsCardType.News,
                WORLD_F1_HEADLINES[_rng.Next(WORLD_F1_HEADLINES.Length)],
                "Últimas novedades del mundo de la Formula 1.",
                "F1 Analytics", week, season,
                new List<string>(), new List<string>(),
                0, 1);
        }

        private GeneratedNews GeneratePraiseOrCriticism(List<PilotData> pilots,
            List<TeamData> teams, int week, int season)
        {
            if (pilots.Count == 0 || teams.Count == 0) return null;

            var pilot = pilots[_rng.Next(pilots.Count)];
            var team = teams.Find(t => t.id == pilot.currentTeamId);
            string teamName = team?.shortName ?? "su equipo";

            bool positive = pilot.formCurrent > 70;

            if (positive)
            {
                return CreateNews(
                    NewsCardType.Positive,
                    FillTemplate(PRAISE_HEADLINES, pilot.lastName, teamName),
                    $"Los expertos elogian la actuación reciente de {pilot.lastName}.",
                    "Grid Talk", week, season,
                    new List<string> { pilot.id },
                    new List<string> { pilot.currentTeamId ?? "" },
                    5, 1);
            }
            else
            {
                return CreateNews(
                    NewsCardType.News,
                    FillTemplate(CRITICISM_HEADLINES, pilot.lastName, teamName),
                    $"Críticas hacia {pilot.lastName} por su rendimiento reciente.",
                    "Racing Tribune", week, season,
                    new List<string> { pilot.id },
                    new List<string> { pilot.currentTeamId ?? "" },
                    -5, 1);
            }
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private GeneratedNews CreateNews(NewsCardType cardType,
            string headline, string body, string mediaSource,
            int week, int season, List<string> pilotIds,
            List<string> teamIds, int moodEffect, int durationWeeks)
        {
            var outlet = Array.Find(MEDIA_OUTLETS, m => m.Name == mediaSource)
                ?? MEDIA_OUTLETS[0];

            return new GeneratedNews
            {
                NewsId = $"news_{_rng.Next(1000000)}",
                CardType = cardType,
                Headline = headline,
                Body = body,
                MediaSource = outlet.Name,
                MediaBias = outlet.Bias,
                Credibility = outlet.Credibility,
                RequiresAction = cardType == NewsCardType.Urgent,
                IsRead = false,
                Week = week,
                Season = season,
                RelatedPilotIds = pilotIds ?? new List<string>(),
                RelatedTeamIds = teamIds ?? new List<string>(),
                MoodEffect = moodEffect,
                DurationWeeks = durationWeeks
            };
        }

        private string FillTemplate(string[] templates, params string[] args)
        {
            string template = templates[_rng.Next(templates.Length)];
            for (int i = 0; i < args.Length; i++)
                template = template.Replace($"{{{i}}}", args[i]);
            return template;
        }

        private string SelectMediaForBias(string bias)
        {
            var matching = Array.FindAll(MEDIA_OUTLETS, m => m.Bias == bias);
            if (matching.Length > 0)
                return matching[_rng.Next(matching.Length)].Name;
            return MEDIA_OUTLETS[_rng.Next(MEDIA_OUTLETS.Length)].Name;
        }

        private string GeneratePlayerTeamBody(List<PilotData> positions,
            TeamData team)
        {
            var teamPilots = positions.FindAll(p => p.currentTeamId == team.id);
            if (teamPilots.Count == 0) return "Sin pilotos del equipo en resultados.";

            string body = $"Resumen de {team.shortName}: ";
            foreach (var p in teamPilots)
            {
                int pos = positions.IndexOf(p) + 1;
                body += $"{p.lastName} P{pos}, ";
            }
            body += $"Posición constructores: P{team.constructorPosition}.";
            return body;
        }

        private void FireNewsEvent(GeneratedNews news)
        {
            _eventBus.FireNewsGenerated(new EventBus.NewsGeneratedArgs
            {
                NewsId = news.NewsId,
                Headline = news.Headline,
                Body = news.Body,
                Type = news.CardType.ToString(),
                MediaOutlet = news.MediaSource,
                IsRumor = news.CardType == NewsCardType.Rumor,
                IsTrue = news.CardType != NewsCardType.Rumor || _rng.NextDouble() < 0.6,
                RelatedPilotIds = news.RelatedPilotIds,
                RelatedTeamIds = news.RelatedTeamIds
            });
        }

        // ══════════════════════════════════════════════════════
        // CONSULTAS
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene noticias no leídas</summary>
        public List<GeneratedNews> GetUnreadNews()
        {
            return _allNews.FindAll(n => !n.IsRead);
        }

        /// <summary>Obtiene noticias urgentes no atendidas</summary>
        public List<GeneratedNews> GetUrgentNews()
        {
            return _allNews.FindAll(n => n.RequiresAction && !n.IsRead);
        }

        /// <summary>Marca una noticia como leída</summary>
        public void MarkAsRead(string newsId)
        {
            var news = _allNews.Find(n => n.NewsId == newsId);
            if (news != null) news.IsRead = true;
        }

        /// <summary>Obtiene todas las noticias de una temporada</summary>
        public List<GeneratedNews> GetSeasonNews(int season)
        {
            return _allNews.FindAll(n => n.Season == season);
        }

        /// <summary>Limpia noticias expiradas</summary>
        public void CleanExpiredNews(int currentWeek)
        {
            _allNews.RemoveAll(n =>
                n.IsRead && currentWeek - n.Week > n.DurationWeeks);
        }
    }
}
