// ============================================================
// F1 Career Manager — StaffScreen.cs
// Pantalla de gestión de personal del equipo
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using F1CareerManager.Core;

namespace F1CareerManager.UI.Screens
{
    public class StaffScreen : MonoBehaviour
    {
        [Header("Staff List")]
        [SerializeField] private Transform staffListContainer;
        [SerializeField] private GameObject staffCardPrefab;

        [Header("Selected Staff Detail")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text staffNameText;
        [SerializeField] private Text staffRoleText;
        [SerializeField] private Text staffNationalityText;
        [SerializeField] private Text staffLevelText;
        [SerializeField] private Image staffLevelBar;
        [SerializeField] private Text staffSalaryText;
        [SerializeField] private Text staffSpecialityText;
        [SerializeField] private Text staffBonusText;
        [SerializeField] private Text staffPersonalityText;
        [SerializeField] private Text staffAgeText;
        [SerializeField] private Text staffContractText;

        [Header("Actions")]
        [SerializeField] private Button fireButton;
        [SerializeField] private Button renewButton;
        [SerializeField] private Button promoteButton;
        [SerializeField] private Button hireNewButton;

        [Header("Hiring Panel")]
        [SerializeField] private GameObject hiringPanel;
        [SerializeField] private Transform candidateListContainer;
        [SerializeField] private GameObject candidateCardPrefab;
        [SerializeField] private Button closeHiringButton;

        [Header("Overview Stats")]
        [SerializeField] private Text totalSalariesText;
        [SerializeField] private Text avgLevelText;
        [SerializeField] private Text emptyRolesText;

        // ── Modelo ───────────────────────────────────────────
        [System.Serializable]
        public class StaffDisplayInfo
        {
            public string id, name, role, roleName, nationality, flag;
            public int level, age;
            public long salary;
            public string speciality, bonus, personality;
            public int contractYearsLeft;
            public bool isPlayer; // pertenece al equipo del jugador
        }

        private List<StaffDisplayInfo> currentStaff = new List<StaffDisplayInfo>();
        private string selectedStaffId = "";

        // ── Role display names ────────────────────────────────
        private readonly Dictionary<string, string> roleNames = new Dictionary<string, string>
        {
            {"TechnicalDirector", "Director Técnico"},
            {"AeroChief", "Jefe de Aerodinámica"},
            {"EngineChief", "Jefe de Motor"},
            {"RaceEngineer", "Ingeniero de Carrera"},
            {"DataAnalyst", "Analista de Datos"},
            {"Medic", "Médico del Equipo"},
            {"CommsDirector", "Jefe de Comunicaciones"},
            {"AcademyDirector", "Director de Academia"},
            {"FinanceDirector", "Jefe Financiero"},
            {"PitCrewChief", "Jefe de Pit Crew"},
            {"SimDriver", "Piloto de Simulador"}
        };

        private void Awake()
        {
            if (fireButton != null) fireButton.onClick.AddListener(OnFirePressed);
            if (renewButton != null) renewButton.onClick.AddListener(OnRenewPressed);
            if (promoteButton != null) promoteButton.onClick.AddListener(OnPromotePressed);
            if (hireNewButton != null) hireNewButton.onClick.AddListener(OnHireNewPressed);
            if (closeHiringButton != null) closeHiringButton.onClick.AddListener(() => {
                if (hiringPanel != null) hiringPanel.SetActive(false);
            });
        }

        // ══════════════════════════════════════════════════════
        // ACTUALIZACIÓN
        // ══════════════════════════════════════════════════════

        public void Refresh(List<StaffDisplayInfo> staff)
        {
            currentStaff = staff;
            PopulateStaffList();
            UpdateOverviewStats();

            if (currentStaff.Count > 0 && string.IsNullOrEmpty(selectedStaffId))
                SelectStaff(currentStaff[0].id);
        }

        private void PopulateStaffList()
        {
            if (staffListContainer == null || staffCardPrefab == null) return;

            foreach (Transform child in staffListContainer)
                Destroy(child.gameObject);

            foreach (var staff in currentStaff)
            {
                GameObject card = Instantiate(staffCardPrefab, staffListContainer);

                Text nameLabel = card.transform.Find("Name")?.GetComponent<Text>();
                if (nameLabel != null) nameLabel.text = staff.name;

                Text roleLabel = card.transform.Find("Role")?.GetComponent<Text>();
                string displayRole = roleNames.ContainsKey(staff.role) ? roleNames[staff.role] : staff.role;
                if (roleLabel != null) roleLabel.text = displayRole;

                Text levelLabel = card.transform.Find("Level")?.GetComponent<Text>();
                if (levelLabel != null) levelLabel.text = new string('★', staff.level) + new string('☆', 5 - staff.level);

                Text flagLabel = card.transform.Find("Flag")?.GetComponent<Text>();
                if (flagLabel != null) flagLabel.text = staff.flag;

                string sid = staff.id;
                Button btn = card.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => SelectStaff(sid));
            }
        }

        public void SelectStaff(string staffId)
        {
            selectedStaffId = staffId;
            StaffDisplayInfo staff = currentStaff.Find(s => s.id == staffId);
            if (staff == null) return;

            string displayRole = roleNames.ContainsKey(staff.role) ? roleNames[staff.role] : staff.role;

            if (staffNameText != null) staffNameText.text = staff.name;
            if (staffRoleText != null) staffRoleText.text = displayRole;
            if (staffNationalityText != null) staffNationalityText.text = $"{staff.flag} {staff.nationality}";
            if (staffLevelText != null) staffLevelText.text = $"Nivel {staff.level}/5";
            if (staffLevelBar != null) staffLevelBar.fillAmount = staff.level / 5f;
            if (staffSalaryText != null) staffSalaryText.text = $"${staff.salary / 1000000f:F1}M/año";
            if (staffSpecialityText != null) staffSpecialityText.text = staff.speciality;
            if (staffBonusText != null) staffBonusText.text = staff.bonus;
            if (staffPersonalityText != null) staffPersonalityText.text = staff.personality;
            if (staffAgeText != null) staffAgeText.text = $"{staff.age} años";
            if (staffContractText != null)
                staffContractText.text = staff.contractYearsLeft > 0 ? $"{staff.contractYearsLeft} años restantes" : "Sin contrato";

            if (detailPanel != null) detailPanel.SetActive(true);
        }

        private void UpdateOverviewStats()
        {
            if (currentStaff.Count == 0) return;

            long totalSalary = 0;
            int totalLevel = 0;
            int emptyCount = 0;
            HashSet<string> filledRoles = new HashSet<string>();

            foreach (var s in currentStaff)
            {
                totalSalary += s.salary;
                totalLevel += s.level;
                filledRoles.Add(s.role);
            }

            int expectedRoles = 10; // 10 roles definidos
            emptyCount = expectedRoles - filledRoles.Count;

            if (totalSalariesText != null) totalSalariesText.text = $"${totalSalary / 1000000f:F1}M/año";
            if (avgLevelText != null) avgLevelText.text = $"Promedio: {(float)totalLevel / currentStaff.Count:F1}/5";
            if (emptyRolesText != null) emptyRolesText.text = emptyCount > 0 ? $"⚠ {emptyCount} roles vacantes" : "✅ Todos cubiertos";
        }

        // ══════════════════════════════════════════════════════
        // ACCIONES
        // ══════════════════════════════════════════════════════

        private void OnFirePressed()
        {
            if (string.IsNullOrEmpty(selectedStaffId)) return;
            Debug.Log($"[StaffScreen] Despedir: {selectedStaffId}");
            // StaffManager.Instance.FireStaff(selectedStaffId);
        }

        private void OnRenewPressed()
        {
            if (string.IsNullOrEmpty(selectedStaffId)) return;
            Debug.Log($"[StaffScreen] Renovar: {selectedStaffId}");
            // StaffManager.Instance.RenewContract(selectedStaffId);
        }

        private void OnPromotePressed()
        {
            if (string.IsNullOrEmpty(selectedStaffId)) return;
            Debug.Log($"[StaffScreen] Promover: {selectedStaffId}");
            // StaffManager.Instance.PromoteStaff(selectedStaffId);
        }

        private void OnHireNewPressed()
        {
            if (hiringPanel != null) hiringPanel.SetActive(true);
            Debug.Log("[StaffScreen] Abriendo panel de contratación");
            // PopulateCandidates(StaffManager.Instance.GetAvailableCandidates());
        }

        public void PopulateCandidates(List<StaffDisplayInfo> candidates)
        {
            if (candidateListContainer == null) return;

            foreach (Transform child in candidateListContainer)
                Destroy(child.gameObject);

            foreach (var cand in candidates)
            {
                if (candidateCardPrefab == null) continue;
                GameObject card = Instantiate(candidateCardPrefab, candidateListContainer);

                Text nameLabel = card.transform.Find("Name")?.GetComponent<Text>();
                if (nameLabel != null) nameLabel.text = $"{cand.flag} {cand.name}";

                Text roleLabel = card.transform.Find("Role")?.GetComponent<Text>();
                string displayRole = roleNames.ContainsKey(cand.role) ? roleNames[cand.role] : cand.role;
                if (roleLabel != null) roleLabel.text = displayRole;

                Text levelLabel = card.transform.Find("Level")?.GetComponent<Text>();
                if (levelLabel != null) levelLabel.text = $"Lvl {cand.level}";

                Text salaryLabel = card.transform.Find("Salary")?.GetComponent<Text>();
                if (salaryLabel != null) salaryLabel.text = $"${cand.salary / 1000000f:F1}M";

                Text bonusLabel = card.transform.Find("Bonus")?.GetComponent<Text>();
                if (bonusLabel != null) bonusLabel.text = cand.bonus;

                string cid = cand.id;
                Button hireBtn = card.transform.Find("HireButton")?.GetComponent<Button>();
                if (hireBtn != null) hireBtn.onClick.AddListener(() => HireCandidate(cid));
            }
        }

        private void HireCandidate(string candidateId)
        {
            Debug.Log($"[StaffScreen] Contratar: {candidateId}");
            // StaffManager.Instance.HireStaff(candidateId);
        }
    }
}
