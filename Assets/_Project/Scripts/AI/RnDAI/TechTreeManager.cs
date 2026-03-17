// ============================================================
// F1 Career Manager — TechTreeManager.cs
// Árbol de tecnología R&D con 4 ramas — IA 2
// ============================================================
// DEPENDENCIAS: ComponentData.cs, TeamData.cs, StaffData.cs,
//               BudgetManager.cs, ComponentEvaluator.cs
// EVENTOS QUE DISPARA: (vía ComponentEvaluator)
// ============================================================

using System;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;
using F1CareerManager.AI.EconomyAI;

namespace F1CareerManager.AI.RnDAI
{
    /// <summary>
    /// Nodo del árbol de tecnología. Cada nodo es un componente
    /// que puede ser investigado, desarrollado e instalado.
    /// Prerequisito: versión anterior del mismo componente.
    /// Máximo 3 niveles de profundidad por rama.
    /// </summary>
    [Serializable]
    public class TechNode
    {
        public string NodeId;              // "aero_frontwing_v1"
        public string Name;               // "Alerón Delantero V1"
        public string Area;               // "Aerodynamics", "Engine", etc.
        public string SpecificPart;        // "FrontWing", "RearWing", etc.
        public int Level;                  // 1, 2 o 3
        public string PrerequisiteNodeId;  // ID del nodo previo (null si es nivel 1)
        public bool IsUnlocked;            // Si ya se desbloqueó
        public bool IsInDevelopment;       // Si está en desarrollo
        public bool IsCompleted;           // Si se completó el desarrollo
        public int DevelopmentWeeks;       // Semanas necesarias (1-8)
        public int CurrentProgress;        // Semanas avanzadas
        public float DevelopmentCost;      // Costo en millones
        public int ExpectedPerformance;    // Rendimiento esperado del componente
        public string Legality;            // "Legal", "GreySubtle", etc.
        public string Description;         // Descripción del nodo
    }

    /// <summary>
    /// Gestiona el árbol de tecnología de cada equipo.
    /// 4 ramas (Aero, Motor, Chasis, Fiabilidad) × partes × 3 niveles.
    /// El Director Técnico acelera el desarrollo.
    /// </summary>
    public class TechTreeManager
    {
        // ── Datos ────────────────────────────────────────────
        private Dictionary<string, List<TechNode>> _teamTrees;
        private ComponentEvaluator _evaluator;
        private BudgetManager _budgetManager;
        private Random _rng;

        // ── Constantes ───────────────────────────────────────
        private const int MAX_PARALLEL_PROJECTS_BASE = 2;
        private const int MAX_PARALLEL_PER_FACTORY = 1;  // +1 por nivel fábrica

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public TechTreeManager(ComponentEvaluator evaluator,
            BudgetManager budgetManager, Random rng = null)
        {
            _evaluator = evaluator;
            _budgetManager = budgetManager;
            _rng = rng ?? new Random();
            _teamTrees = new Dictionary<string, List<TechNode>>();
        }

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN DEL ÁRBOL
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera el árbol de tecnología para un equipo.
        /// Equipos top empiezan con algunos nodos ya desbloqueados.
        /// </summary>
        public void InitializeTreeForTeam(TeamData team)
        {
            var tree = new List<TechNode>();

            // ── Rama Aerodinámica ────────────────────────────
            AddBranch(tree, "Aerodynamics", "FrontWing", "Alerón Delantero",
                new int[] { 60, 75, 90 }, new float[] { 3f, 5f, 8f },
                new int[] { 2, 4, 7 });

            AddBranch(tree, "Aerodynamics", "RearWing", "Alerón Trasero",
                new int[] { 55, 70, 85 }, new float[] { 3f, 5f, 7f },
                new int[] { 2, 4, 6 });

            AddBranch(tree, "Aerodynamics", "FloorBody", "Fondo Plano",
                new int[] { 65, 80, 95 }, new float[] { 4f, 7f, 12f },
                new int[] { 3, 5, 8 });

            AddBranch(tree, "Aerodynamics", "DRS", "Sistema DRS",
                new int[] { 50, 65, 80 }, new float[] { 2f, 4f, 6f },
                new int[] { 2, 3, 5 });

            // ── Rama Motor ───────────────────────────────────
            AddBranch(tree, "Engine", "ICE", "Motor Combustión",
                new int[] { 60, 78, 93 }, new float[] { 5f, 8f, 14f },
                new int[] { 3, 5, 8 });

            AddBranch(tree, "Engine", "ERS", "Sistema ERS",
                new int[] { 55, 70, 85 }, new float[] { 4f, 6f, 10f },
                new int[] { 2, 4, 6 });

            AddBranch(tree, "Engine", "Turbo", "Turbocompresor",
                new int[] { 50, 68, 82 }, new float[] { 3f, 5f, 8f },
                new int[] { 2, 4, 6 });

            // ── Rama Chasis ──────────────────────────────────
            AddBranch(tree, "Chassis", "Suspension", "Suspensión",
                new int[] { 55, 72, 88 }, new float[] { 3f, 5f, 9f },
                new int[] { 2, 4, 7 });

            AddBranch(tree, "Chassis", "Brakes", "Sistema de Frenos",
                new int[] { 50, 65, 80 }, new float[] { 2f, 4f, 6f },
                new int[] { 2, 3, 5 });

            AddBranch(tree, "Chassis", "Gearbox", "Caja de Cambios",
                new int[] { 55, 70, 85 }, new float[] { 4f, 6f, 10f },
                new int[] { 3, 5, 7 });

            // ── Rama Fiabilidad ──────────────────────────────
            AddBranch(tree, "Reliability", "Cooling", "Sistema Refrigeración",
                new int[] { 50, 65, 80 }, new float[] { 2f, 4f, 6f },
                new int[] { 2, 3, 5 });

            AddBranch(tree, "Reliability", "Electronics", "Electrónica",
                new int[] { 55, 70, 85 }, new float[] { 3f, 5f, 7f },
                new int[] { 2, 4, 6 });

            AddBranch(tree, "Reliability", "Hydraulics", "Sistema Hidráulico",
                new int[] { 50, 68, 82 }, new float[] { 2f, 4f, 7f },
                new int[] { 2, 3, 6 });

            // Equipos top empiezan con nivel 1 desbloqueado en algunas ramas
            int nodesToUnlock = team.carPerformance / 20; // 3-4 para top, 1-2 para bajos
            UnlockStartingNodes(tree, nodesToUnlock);

            _teamTrees[team.id] = tree;
        }

        /// <summary>
        /// Agrega una rama de 3 niveles al árbol
        /// </summary>
        private void AddBranch(List<TechNode> tree, string area,
            string part, string baseName,
            int[] performances, float[] costs, int[] weeks)
        {
            string prevId = null;
            for (int level = 1; level <= 3; level++)
            {
                string nodeId = $"{area.ToLower()}_{part.ToLower()}_v{level}";

                // Niveles más altos pueden ser zona gris
                string legality = "Legal";
                if (level == 3 && (float)_rng.NextDouble() < 0.25f)
                    legality = "GreySubtle";

                var node = new TechNode
                {
                    NodeId = nodeId,
                    Name = $"{baseName} V{level}",
                    Area = area,
                    SpecificPart = part,
                    Level = level,
                    PrerequisiteNodeId = prevId,
                    IsUnlocked = false,
                    IsInDevelopment = false,
                    IsCompleted = false,
                    DevelopmentWeeks = weeks[level - 1],
                    CurrentProgress = 0,
                    DevelopmentCost = costs[level - 1],
                    ExpectedPerformance = performances[level - 1],
                    Legality = legality,
                    Description = $"{baseName} nivel {level} — {GetLevelDescription(level)}"
                };

                tree.Add(node);
                prevId = nodeId;
            }
        }

        private string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "Mejora incremental, bajo riesgo";
                case 2: return "Avance significativo, riesgo moderado";
                case 3: return "Tecnología de punta, alto riesgo y recompensa";
                default: return "";
            }
        }

        /// <summary>
        /// Desbloquea nodos iniciales aleatoriamente para equipos buenos
        /// </summary>
        private void UnlockStartingNodes(List<TechNode> tree, int count)
        {
            var level1Nodes = tree.FindAll(n => n.Level == 1);
            int unlocked = 0;

            // Mezclar para aleatorizar
            for (int i = level1Nodes.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var temp = level1Nodes[i];
                level1Nodes[i] = level1Nodes[j];
                level1Nodes[j] = temp;
            }

            foreach (var node in level1Nodes)
            {
                if (unlocked >= count) break;
                node.IsUnlocked = true;
                node.IsCompleted = true;
                unlocked++;
            }
        }

        // ══════════════════════════════════════════════════════
        // DESARROLLO
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Inicia el desarrollo de un nodo del árbol.
        /// Verifica prerequisitos, presupuesto y límite de proyectos paralelos.
        /// </summary>
        public bool StartDevelopment(string teamId, string nodeId, TeamData team)
        {
            if (!_teamTrees.ContainsKey(teamId)) return false;

            var tree = _teamTrees[teamId];
            var node = tree.Find(n => n.NodeId == nodeId);
            if (node == null) return false;

            // Verificar que no esté ya completado o en desarrollo
            if (node.IsCompleted || node.IsInDevelopment) return false;

            // Verificar prerequisito
            if (node.PrerequisiteNodeId != null)
            {
                var prereq = tree.Find(n => n.NodeId == node.PrerequisiteNodeId);
                if (prereq == null || !prereq.IsCompleted) return false;
            }

            // Verificar límite de proyectos paralelos
            int currentProjects = 0;
            foreach (var n in tree)
                if (n.IsInDevelopment) currentProjects++;

            int maxProjects = MAX_PARALLEL_PROJECTS_BASE +
                (team.factoryLevel * MAX_PARALLEL_PER_FACTORY);
            if (currentProjects >= maxProjects) return false;

            // Verificar presupuesto
            if (team.financialStatus == "Crisis") return false;
            if (!_budgetManager.CanAfford(team, node.DevelopmentCost)) return false;

            // Descontar costo
            _budgetManager.ProcessRnDExpense(team, node.DevelopmentCost, node.Name);

            // Iniciar desarrollo
            node.IsInDevelopment = true;
            node.CurrentProgress = 0;
            node.IsUnlocked = true;

            return true;
        }

        /// <summary>
        /// Avanza una semana de desarrollo para todos los proyectos de un equipo.
        /// El Director Técnico acelera el desarrollo según su nivel.
        /// </summary>
        /// <returns>Lista de nodos que se completaron esta semana</returns>
        public List<TechNode> AdvanceWeek(string teamId, List<StaffData> staff)
        {
            var completed = new List<TechNode>();
            if (!_teamTrees.ContainsKey(teamId)) return completed;

            var tree = _teamTrees[teamId];

            // Calcular bonus del Director Técnico
            float techDirBonus = 0f;
            foreach (var member in staff)
            {
                if (member.teamId == teamId && member.role == "TechnicalDirector"
                    && !member.isBurnedOut)
                {
                    techDirBonus = member.GetRoleBonus();
                    break;
                }
            }

            foreach (var node in tree)
            {
                if (!node.IsInDevelopment) continue;

                // Progreso base: 1 semana
                float progress = 1f;

                // Bonus del Director Técnico (reduce tiempo efectivo)
                // techDirBonus de ~0.10 = +10% más rápido
                progress *= (1f + techDirBonus);

                // Variación aleatoria pequeña
                progress *= 1f + ((float)_rng.NextDouble() * 0.1f - 0.05f);

                node.CurrentProgress += (int)Math.Ceiling(progress);

                // ¿Se completó?
                if (node.CurrentProgress >= node.DevelopmentWeeks)
                {
                    node.IsInDevelopment = false;
                    node.IsCompleted = true;
                    completed.Add(node);
                }
            }

            return completed;
        }

        // ══════════════════════════════════════════════════════
        // CREACIÓN DE COMPONENTE DESDE NODO
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Crea un ComponentData a partir de un nodo completado del tech tree,
        /// listo para ser instalado por el ComponentEvaluator.
        /// </summary>
        public ComponentData CreateComponentFromNode(TechNode node, string teamId)
        {
            if (!node.IsCompleted) return null;

            return new ComponentData
            {
                id = $"comp_{node.NodeId}_{teamId}",
                name = node.Name,
                description = node.Description,
                area = node.Area,
                specificPart = node.SpecificPart,
                expectedPerformance = node.ExpectedPerformance,
                actualPerformance = 0,
                performanceGain = node.ExpectedPerformance / 5,
                reliability = 80 + _rng.Next(0, 15),
                developmentCost = node.DevelopmentCost,
                developmentWeeks = node.DevelopmentWeeks,
                developmentProgress = 100,
                status = "Available",
                legality = node.Legality,
                hasBeenInvestigated = false,
                isBanned = false,
                installResult = "",
                hasBeenInstalled = false,
                marketPrice = node.DevelopmentCost * 1.5f,
                isForSale = false,
                ownerTeamId = teamId
            };
        }

        // ══════════════════════════════════════════════════════
        // CONSULTAS
        // ══════════════════════════════════════════════════════

        /// <summary>Obtiene el árbol completo de un equipo</summary>
        public List<TechNode> GetTeamTree(string teamId)
        {
            if (_teamTrees.ContainsKey(teamId))
                return _teamTrees[teamId];
            return new List<TechNode>();
        }

        /// <summary>Obtiene los nodos disponibles para desarrollo (prerequisitos OK)</summary>
        public List<TechNode> GetAvailableNodes(string teamId)
        {
            var available = new List<TechNode>();
            if (!_teamTrees.ContainsKey(teamId)) return available;

            var tree = _teamTrees[teamId];

            foreach (var node in tree)
            {
                if (node.IsCompleted || node.IsInDevelopment) continue;

                // Verificar prerequisito
                if (node.PrerequisiteNodeId == null)
                {
                    available.Add(node);
                    continue;
                }

                var prereq = tree.Find(n => n.NodeId == node.PrerequisiteNodeId);
                if (prereq != null && prereq.IsCompleted)
                    available.Add(node);
            }

            return available;
        }

        /// <summary>Obtiene los proyectos actualmente en desarrollo</summary>
        public List<TechNode> GetActiveProjects(string teamId)
        {
            var active = new List<TechNode>();
            if (!_teamTrees.ContainsKey(teamId)) return active;

            foreach (var node in _teamTrees[teamId])
            {
                if (node.IsInDevelopment)
                    active.Add(node);
            }
            return active;
        }

        /// <summary>Obtiene cuántos slots de proyectos quedan</summary>
        public int GetRemainingProjectSlots(string teamId, TeamData team)
        {
            int maxProjects = MAX_PARALLEL_PROJECTS_BASE +
                (team.factoryLevel * MAX_PARALLEL_PER_FACTORY);
            int current = GetActiveProjects(teamId).Count;
            return maxProjects - current;
        }

        /// <summary>Obtiene el progreso total del equipo en el tech tree (%)</summary>
        public float GetTotalProgress(string teamId)
        {
            if (!_teamTrees.ContainsKey(teamId)) return 0f;

            var tree = _teamTrees[teamId];
            int total = tree.Count;
            int completed = 0;
            foreach (var node in tree)
                if (node.IsCompleted) completed++;

            return total > 0 ? (float)completed / total * 100f : 0f;
        }
    }
}
