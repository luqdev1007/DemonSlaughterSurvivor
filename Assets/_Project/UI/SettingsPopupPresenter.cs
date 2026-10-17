using Game.Core;
using System;

namespace Game.UI
{
    public sealed class SettingsPopupPresenter : IDisposable
    {
        private readonly SettingsPopupView _view;
        private readonly ISoundSettings _settings;

        public SettingsPopupPresenter(SettingsPopupView view, ISoundSettings settings)
        {
            _view = view;
            _settings = settings;

            _view.Bind(_settings.SetMasterVolume, _settings.SetMuted, Close);
            _view.HideImmediate();
        }

        public void Open()
        {
            _view.Show(_settings.MasterVolume, _settings.IsMuted);
        }

        public void Close()
        {
            _view.Hide();
        }

        public void Dispose()
        {
            if (_view != null)
                _view.Unbind();
        }
    }
}
