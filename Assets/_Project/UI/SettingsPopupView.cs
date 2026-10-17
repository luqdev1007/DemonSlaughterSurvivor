using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class SettingsPopupView : MonoBehaviour
    {
        [SerializeField] private UiFade _fade;
        [SerializeField] private Slider _volume;
        [SerializeField] private Toggle _mute;
        [SerializeField] private Toggle _damageNumbers;
        [SerializeField] private Slider _damageNumberSize;
        [SerializeField] private Button _close;

        public bool IsWired => _fade != null && _fade.IsWired && _volume != null && _mute != null && _close != null
            && _damageNumbers != null && _damageNumberSize != null;

        public void Bind(Action<float> onVolume, Action<bool> onMute, Action<bool> onDamageNumbers, Action<float> onDamageNumberSize, Action onClose)
        {
            _volume.onValueChanged.AddListener(value => onVolume(value));
            _mute.onValueChanged.AddListener(value => onMute(value));
            _damageNumbers.onValueChanged.AddListener(value => onDamageNumbers(value));
            _damageNumberSize.onValueChanged.AddListener(value => onDamageNumberSize(value));
            _close.onClick.AddListener(() => onClose());
        }

        public void Unbind()
        {
            if (_volume != null)
                _volume.onValueChanged.RemoveAllListeners();

            if (_mute != null)
                _mute.onValueChanged.RemoveAllListeners();

            if (_damageNumbers != null)
                _damageNumbers.onValueChanged.RemoveAllListeners();

            if (_damageNumberSize != null)
                _damageNumberSize.onValueChanged.RemoveAllListeners();

            if (_close != null)
                _close.onClick.RemoveAllListeners();
        }

        public void Show(float volume, bool muted, bool damageNumbers, float damageNumberSize, float minSize, float maxSize)
        {
            _volume.SetValueWithoutNotify(volume);
            _mute.SetIsOnWithoutNotify(muted);
            _damageNumbers.SetIsOnWithoutNotify(damageNumbers);
            _damageNumberSize.minValue = minSize;
            _damageNumberSize.maxValue = maxSize;
            _damageNumberSize.SetValueWithoutNotify(damageNumberSize);

            _fade.Show();
        }

        public void Hide()
        {
            _fade.Hide();
        }

        public void HideImmediate()
        {
            _fade.HideImmediate();
        }
    }
}
