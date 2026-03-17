// ============================================================
// F1 Career Manager — PilotCard.cs
// Tarjeta visual de piloto reutilizable
// ============================================================
// PREFAB: PilotCard_Prefab  (120px alto × ancho flexible)
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using F1CareerManager.Data;

namespace F1CareerManager.UI.Components
{
    /// <summary>
    /// Tarjeta visual completa de un piloto. Incluye:
    /// - Sprite pixel art 32x32
    /// - Nombre, número, bandera
    /// - Estrellas (1-5) con color según nivel
    /// - Barra de forma actual
    /// - Icono estado emocional
    /// - Mini barras de stats principales
    /// - Contrato: años + salario
    /// Toque → abre PilotDetailPopup
    /// </summary>
    public class PilotCard : MonoBehaviour
    {
        // ── Referencias UI ───────────────────────────────────
        [Header("Identidad")]
        [SerializeField] private Image _pilotSprite;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _numberText;
        [SerializeField] private Text _nationalityText;
        [SerializeField] private Image _teamColorBar;

        [Header("Rating")]
        [SerializeField] private StarRating _starRating;
        [SerializeField] private Text _overallText;

        [Header("Estado")]
        [SerializeField] private Image _formBar;
        [SerializeField] private Image _formBarBg;
        [SerializeField] private Text _moodEmoji;
        [SerializeField] private Text _moodLabel;

        [Header("Stats Mini")]
        [SerializeField] private StatBar _speedBar;
        [SerializeField] private StatBar _consistencyBar;
        [SerializeField] private StatBar _rainBar;

        [Header("Contrato")]
        [SerializeField] private Text _contractText;
        [SerializeField] private Text _salaryText;

        [Header("Badges")]
        [SerializeField] private GameObject _injuredBadge;
        [SerializeField] private GameObject _newSigningBadge;
        [SerializeField] private Text _roleBadge;

        [Header("Interacción")]
        [SerializeField] private Button _cardButton;

        // ── Estado ───────────────────────────────────────────
        private PilotData _pilotData;
        private System.Action<PilotData> _onCardClicked;

        // ══════════════════════════════════════════════════════
        // SETUP
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Configura la tarjeta completa con datos del piloto
        /// </summary>
        public void Setup(PilotData pilot, string teamId = null,
            System.Action<PilotData> onClick = null)
        {
            _pilotData = pilot;
            _onCardClicked = onClick;

            SetupIdentity(pilot, teamId);
            SetupRating(pilot);
            SetupState(pilot);
            SetupMiniStats(pilot);
            SetupContract(pilot);
            SetupBadges(pilot);
            SetupInteraction();
        }

        // ══════════════════════════════════════════════════════
        // SECCIONES DE SETUP
        // ══════════════════════════════════════════════════════

        private void SetupIdentity(PilotData pilot, string teamId)
        {
            // Nombre
            if (_nameText != null)
            {
                _nameText.text = $"{pilot.firstName[0]}. {pilot.lastName}";
                _nameText.color = UITheme.TextPrimary;
                _nameText.fontSize = UITheme.FONT_SIZE_MD;
            }

            // Número
            if (_numberText != null)
            {
                _numberText.text = $"#{pilot.number}";
                _numberText.color = UITheme.TextSecondary;
                _numberText.fontSize = UITheme.FONT_SIZE_SM;
            }

            // Nacionalidad con bandera
            if (_nationalityText != null)
            {
                _nationalityText.text = GetFlagEmoji(pilot.countryCode);
                _nationalityText.fontSize = UITheme.FONT_SIZE_LG;
            }

            // Color del equipo
            string colorTeamId = teamId ?? pilot.currentTeamId ?? "";
            if (_teamColorBar != null)
                _teamColorBar.color = UITheme.GetTeamColor(colorTeamId);
        }

        private void SetupRating(PilotData pilot)
        {
            // Estrellas
            if (_starRating != null)
                _starRating.SetRating(pilot.stars);

            // Overall
            if (_overallText != null)
            {
                _overallText.text = pilot.overallRating.ToString();
                _overallText.color = UITheme.GetStatColor(pilot.overallRating);
                _overallText.fontSize = UITheme.FONT_SIZE_XL;
            }
        }

        private void SetupState(PilotData pilot)
        {
            // Barra de forma
            if (_formBar != null && _formBarBg != null)
            {
                _formBarBg.color = UITheme.BackgroundInput;
                float formRatio = pilot.formCurrent / 100f;
                _formBar.fillAmount = formRatio;
                _formBar.color = UITheme.GetFormColor(pilot.formCurrent);
            }

            // Emoji de estado emocional
            if (_moodEmoji != null)
            {
                _moodEmoji.text = UITheme.GetMoodEmoji(pilot.mood);
                _moodEmoji.fontSize = UITheme.FONT_SIZE_LG;
            }

            if (_moodLabel != null)
            {
                _moodLabel.text = GetMoodSpanish(pilot.mood);
                _moodLabel.color = UITheme.GetMoodColor(pilot.mood);
                _moodLabel.fontSize = UITheme.FONT_SIZE_XS;
            }
        }

        private void SetupMiniStats(PilotData pilot)
        {
            // Stats en mini barras compactas
            if (_speedBar != null)
            {
                _speedBar.SetCompact(true);
                _speedBar.Setup("VEL", pilot.speed);
            }

            if (_consistencyBar != null)
            {
                _consistencyBar.SetCompact(true);
                _consistencyBar.Setup("CON", pilot.consistency);
            }

            if (_rainBar != null)
            {
                _rainBar.SetCompact(true);
                _rainBar.Setup("LLU", pilot.rainSkill);
            }
        }

        private void SetupContract(PilotData pilot)
        {
            if (_contractText != null)
            {
                if (pilot.contractYearsLeft > 0)
                {
                    _contractText.text = $"{pilot.contractYearsLeft} año{(pilot.contractYearsLeft > 1 ? "s" : "")}";
                    _contractText.color = pilot.contractYearsLeft <= 1
                        ? UITheme.TextWarning : UITheme.TextSecondary;
                }
                else
                {
                    _contractText.text = "Libre";
                    _contractText.color = UITheme.TextPositive;
                }
                _contractText.fontSize = UITheme.FONT_SIZE_XS;
            }

            if (_salaryText != null)
            {
                _salaryText.text = $"${pilot.salary:F1}M";
                _salaryText.color = UITheme.TextSecondary;
                _salaryText.fontSize = UITheme.FONT_SIZE_XS;
            }
        }

        private void SetupBadges(PilotData pilot)
        {
            // Badge lesión
            if (_injuredBadge != null)
                _injuredBadge.SetActive(pilot.isInjured);

            // Badge nuevo fichaje
            if (_newSigningBadge != null)
                _newSigningBadge.SetActive(false); // Se activa externamente

            // Rol
            if (_roleBadge != null)
            {
                _roleBadge.text = GetRoleLabel(pilot.role);
                _roleBadge.color = pilot.role == "First"
                    ? UITheme.AccentGold : UITheme.TextSecondary;
                _roleBadge.fontSize = UITheme.FONT_SIZE_XS;
            }
        }

        private void SetupInteraction()
        {
            if (_cardButton != null)
            {
                _cardButton.onClick.RemoveAllListeners();
                _cardButton.onClick.AddListener(() =>
                    _onCardClicked?.Invoke(_pilotData));
            }
        }

        // ══════════════════════════════════════════════════════
        // ACTUALIZACIÓN EN TIEMPO REAL
        // ══════════════════════════════════════════════════════

        /// <summary>Actualiza solo el estado emocional y forma</summary>
        public void RefreshState()
        {
            if (_pilotData == null) return;
            SetupState(_pilotData);
        }

        /// <summary>Actualiza la tarjeta completa</summary>
        public void Refresh()
        {
            if (_pilotData == null) return;
            Setup(_pilotData, _pilotData.currentTeamId, _onCardClicked);
        }

        /// <summary>Marca como nuevo fichaje</summary>
        public void SetNewSigning(bool isNew)
        {
            if (_newSigningBadge != null)
                _newSigningBadge.SetActive(isNew);
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private string GetFlagEmoji(string countryCode)
        {
            switch (countryCode?.ToUpper())
            {
                case "NL": return "🇳🇱"; case "MX": return "🇲🇽";
                case "GB": return "🇬🇧"; case "MC": return "🇲🇨";
                case "ES": return "🇪🇸"; case "AU": return "🇦🇺";
                case "FR": return "🇫🇷"; case "CA": return "🇨🇦";
                case "DE": return "🇩🇪"; case "FI": return "🇫🇮";
                case "JP": return "🇯🇵"; case "TH": return "🇹🇭";
                case "CN": return "🇨🇳"; case "DK": return "🇩🇰";
                case "US": return "🇺🇸"; case "IT": return "🇮🇹";
                case "AR": return "🇦🇷"; case "BR": return "🇧🇷";
                case "NZ": return "🇳🇿"; case "BE": return "🇧🇪";
                default: return "🏁";
            }
        }

        private string GetMoodSpanish(string mood)
        {
            switch (mood)
            {
                case "Happy":    return "Contento";
                case "Neutral":  return "Neutral";
                case "Upset":    return "Molesto";
                case "Furious":  return "Furioso";
                case "WantsOut": return "Quiere irse";
                default:         return "Neutral";
            }
        }

        private string GetRoleLabel(string role)
        {
            switch (role)
            {
                case "First":   return "PILOTO #1";
                case "Second":  return "PILOTO #2";
                case "Reserve": return "RESERVA";
                default:        return role?.ToUpper() ?? "";
            }
        }

        /// <summary>Obtiene los datos del piloto de esta card</summary>
        public PilotData GetPilotData() => _pilotData;
    }
}
