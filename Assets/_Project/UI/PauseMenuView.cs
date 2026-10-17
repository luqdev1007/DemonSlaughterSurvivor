using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private UiFade _fade;
        [SerializeField] private Button _resume;
        [SerializeField] private Button _settings;
        [SerializeField] private SettingsPopupView _settingsPopup;

        public bool IsWired => _fade != null && _fade.IsWired && _resume != null && _settings != null
            && _settingsPopup != null && _settingsPopup.IsWired;

        public SettingsPopupView SettingsPopup => _settingsPopup;

        public void Bind(Action onResume, Action onSettings)
        {
            _resume.onClick.AddListener(() => onResume());
            _settings.onClick.AddListener(() => onSettings());
        }

        public void Unbind()
        {
            if (_resume != null)
                _resume.onClick.RemoveAllListeners();

            if (_settings != null)
                _settings.onClick.RemoveAllListeners();
        }

        public void SetOpen(bool open)
        {
            if (open)
            {
                _fade.Show();

                return;
            }

            _fade.Hide();
        }

        public void HideImmediate()
        {
            _fade.HideImmediate();
        }
    }
}
