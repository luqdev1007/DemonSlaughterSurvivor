using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private Image _healthFill;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private Image _ultimateChargeFill;

        public bool IsWired => _healthFill != null && _healthText != null && _ultimateChargeFill != null;

        public void ShowHealth(float health, float maxHealth)
        {
            float fraction = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;

            _healthFill.fillAmount = fraction;
            _healthText.SetText("{0} / {1}", Mathf.Ceil(health), Mathf.Ceil(maxHealth));
        }

        public void ShowUltimateCharge(float fraction)
        {
            _ultimateChargeFill.fillAmount = Mathf.Clamp01(fraction);
        }
    }
}
