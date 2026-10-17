using DG.Tweening;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class AbilitySlotView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _cooldown;
        [SerializeField] private Image _frame;
        [SerializeField] private TMP_Text _timer;
        [SerializeField] private Color _readyFrame = new Color(0.95f, 0.80f, 0.45f);
        [SerializeField] private Color _coolingFrame = new Color(0.45f, 0.42f, 0.40f);
        [SerializeField] private float _readyPunch = 0.18f;
        [SerializeField] private float _readyPunchSeconds = 0.25f;

        private float _shownRemaining = -1f;
        private Tween _punch;

        public bool IsWired => _icon != null && _cooldown != null && _frame != null && _timer != null;

        public void SetIcon(Sprite icon)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        public void SetCooldown(float remaining, float fill)
        {
            bool cooling = remaining > 0f;

            _cooldown.fillAmount = cooling ? fill : 0f;
            _frame.color = cooling ? _coolingFrame : _readyFrame;

            float rounded = cooling ? Mathf.Ceil(remaining * 10f) / 10f : 0f;

            if (Mathf.Approximately(rounded, _shownRemaining))
                return;

            if (cooling == false && _shownRemaining > 0f)
                PunchReady();

            _shownRemaining = rounded;
            _timer.SetText(cooling ? rounded.ToString("0.0", CultureInfo.InvariantCulture) + "s" : string.Empty);
        }

        private void PunchReady()
        {
            _punch?.Kill();
            transform.localScale = Vector3.one;
            _punch = DOTween.To(() => 0f, value => transform.localScale = Vector3.one * (1f + _readyPunch * Mathf.Sin(value * Mathf.PI)), 1f, _readyPunchSeconds)
                .SetUpdate(true);
        }

        private void OnDestroy()
        {
            _punch?.Kill();
        }
    }
}
