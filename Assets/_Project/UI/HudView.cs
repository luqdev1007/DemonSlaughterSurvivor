using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private Image _healthFill;
        [SerializeField] private TMP_Text _healthText;

        public bool IsWired => _healthFill != null && _healthText != null;

        public void ShowHealth(float health, float maxHealth)
        {
            float fraction = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;

            _healthFill.fillAmount = fraction;
            _healthText.SetText("{0} / {1}", Mathf.Ceil(health), Mathf.Ceil(maxHealth));
        }
    }
}
