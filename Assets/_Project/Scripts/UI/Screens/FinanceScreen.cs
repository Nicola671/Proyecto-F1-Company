// ============================================================
// F1 Career Manager — FinanceScreen.cs
// Pantalla de finanzas — balance, sponsors, budget cap
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.Data;

namespace F1CareerManager.UI.Screens
{
    /// <summary>
    /// Balance general, desglose ingresos/gastos,
    /// budget cap, sponsors, gráfico evolución.
    /// </summary>
    public class FinanceScreen : MonoBehaviour
    {
        [Header("Balance")]
        [SerializeField] private Text _totalBudgetText;
        [SerializeField] private Text _totalIncomeText;
        [SerializeField] private Text _totalExpensesText;
        [SerializeField] private Image _balanceBar;

        [Header("Desglose Ingresos")]
        [SerializeField] private Components.StatBar _incomePrizes;
        [SerializeField] private Components.StatBar _incomeSponsors;
        [SerializeField] private Components.StatBar _incomeMerch;

        [Header("Desglose Gastos")]
        [SerializeField] private Components.StatBar _expSalaries;
        [SerializeField] private Components.StatBar _expRnD;
        [SerializeField] private Components.StatBar _expLogistics;
        [SerializeField] private Components.StatBar _expFines;

        [Header("Budget Cap")]
        [SerializeField] private Image _capBar;
        [SerializeField] private Image _capDangerZone;
        [SerializeField] private Text _capText;
        [SerializeField] private Text _capWarning;

        [Header("Sponsors")]
        [SerializeField] private Transform _sponsorContainer;

        [Header("Evolución")]
        [SerializeField] private Transform _graphContainer;

        private TeamData _team;
        private List<float> _budgetHistory = new List<float>();

        public void Initialize(TeamData team, List<float> history)
        {
            _team = team;
            _budgetHistory = history ?? new List<float>();
            Refresh();
        }

        public void Refresh()
        {
            if (_team == null) return;

            // Balance total
            if (_totalBudgetText != null)
            {
                _totalBudgetText.text = $"${_team.budget:F1}M";
                _totalBudgetText.color = _team.budget > 0
                    ? UITheme.TextPositive : UITheme.TextNegative;
                _totalBudgetText.fontSize = UITheme.FONT_SIZE_TITLE;
            }

            if (_totalIncomeText != null)
            {
                _totalIncomeText.text = $"▲ ${_team.revenue:F1}M";
                _totalIncomeText.color = UITheme.TextPositive;
            }

            if (_totalExpensesText != null)
            {
                _totalExpensesText.text = $"▼ ${_team.expenses:F1}M";
                _totalExpensesText.color = UITheme.TextNegative;
            }

            // Barras de desglose
            float maxVal = Mathf.Max(_team.revenue, _team.expenses, 1f);
            int normIncome = (int)((_team.sponsorIncome / maxVal) * 100);
            int normSalary = (int)((_team.totalSalaries / maxVal) * 100);

            if (_incomePrizes != null) _incomePrizes.Setup("Premios",
                (int)((_team.revenue * 0.4f / maxVal) * 100));
            if (_incomeSponsors != null) _incomeSponsors.Setup("Sponsors", normIncome);
            if (_incomeMerch != null) _incomeMerch.Setup("Merchandise",
                (int)((_team.revenue * 0.1f / maxVal) * 100));

            if (_expSalaries != null) _expSalaries.Setup("Salarios", normSalary);
            if (_expRnD != null) _expRnD.Setup("R&D",
                (int)((_team.rndBudget / maxVal) * 100));
            if (_expLogistics != null) _expLogistics.Setup("Logística", 25);

            // Budget Cap
            float budgetCap = _team.baseBudget;
            float capRatio = _team.expenses / Mathf.Max(budgetCap, 1f);

            if (_capBar != null)
            {
                _capBar.fillAmount = Mathf.Clamp01(capRatio);
                _capBar.color = capRatio > 0.9f ? UITheme.TextNegative :
                    capRatio > 0.75f ? UITheme.TextWarning : UITheme.TextPositive;
            }

            if (_capText != null)
            {
                _capText.text = $"Cap: ${_team.expenses:F1}M / ${budgetCap:F1}M";
                _capText.color = UITheme.TextSecondary;
            }

            if (_capWarning != null)
            {
                if (capRatio > 0.9f)
                {
                    _capWarning.text = "⚠️ ¡PELIGRO! Cerca del budget cap";
                    _capWarning.color = UITheme.TextNegative;
                    _capWarning.gameObject.SetActive(true);
                }
                else
                {
                    _capWarning.gameObject.SetActive(false);
                }
            }

            // Evolución (barras simples)
            DrawBudgetGraph();
        }

        private void DrawBudgetGraph()
        {
            if (_graphContainer == null) return;

            foreach (Transform child in _graphContainer)
                Destroy(child.gameObject);

            float maxBudget = 1f;
            foreach (var b in _budgetHistory)
                if (b > maxBudget) maxBudget = b;

            int count = Mathf.Min(_budgetHistory.Count, 8);
            int start = _budgetHistory.Count - count;

            for (int i = 0; i < count; i++)
            {
                float val = _budgetHistory[start + i];
                float height = (val / maxBudget) * 100f;

                GameObject bar = new GameObject($"Week_{start + i}",
                    typeof(RectTransform), typeof(Image));
                bar.transform.SetParent(_graphContainer, false);

                Image img = bar.GetComponent<Image>();
                img.color = val > _team.baseBudget * 0.2f
                    ? UITheme.AccentTertiary : UITheme.TextNegative;

                RectTransform rt = bar.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(i / (float)count, 0);
                rt.anchorMax = new Vector2((i + 0.8f) / count,
                    height / 100f);
                rt.offsetMin = new Vector2(2, 0);
                rt.offsetMax = new Vector2(-2, 0);
            }
        }

        private void OnEnable()
        {
            EventBus.Instance.OnBudgetChanged += OnBudgetChanged;
        }

        private void OnDisable()
        {
            EventBus.Instance.OnBudgetChanged -= OnBudgetChanged;
        }

        private void OnBudgetChanged(object s, EventBus.BudgetChangedArgs a)
        {
            if (a.TeamId == _team?.id) Refresh();
        }

        public void Show() { gameObject.SetActive(true); Refresh(); }
        public void Hide() => gameObject.SetActive(false);
    }
}
