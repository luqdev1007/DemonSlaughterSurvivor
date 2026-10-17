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
        [SerializeField] private Button _close;

        public bool IsWired => _fade != null && _fade.IsWired && _volume != null && _mute != null && _close != null;

        public void Bind(Action<float> onVolume, Action<bool> onMute, Action onClose)
        {
            _volume.onValueChanged.AddListener(value => onVolume(value));
            _mute.onValueChanged.AddListener(value => onMute(value));
            _close.onClick.AddListener(() => onClose());
        }

        public void Unbind()
        {
            if (_volume != null)
                _volume.onValueChanged.RemoveAllListeners();

            if (_mute != null)
                _mute.onValueChanged.RemoveAllListeners();

            if (_close != null)
                _close.onClick.RemoveAllListeners();
        }

        public void Show(float volume, bool muted)
        {
            _volume.SetValueWithoutNotify(volume);
            _mute.SetIsOnWithoutNotify(muted);

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
