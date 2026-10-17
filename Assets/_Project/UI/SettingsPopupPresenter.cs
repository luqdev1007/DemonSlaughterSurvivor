using Game.Core;
using System;

namespace Game.UI
{
    public sealed class SettingsPopupPresenter : IDisposable
    {
        private readonly SettingsPopupView _view;
        private readonly ISoundSettings _settings;
        private readonly ICombatTextSettings _combatText;

        public SettingsPopupPresenter(SettingsPopupView view, ISoundSettings settings, ICombatTextSettings combatText)
        {
            _view = view;
            _settings = settings;
            _combatText = combatText;

            _view.Bind(_settings.SetMasterVolume, _settings.SetMuted, _combatText.SetDamageNumbersEnabled, _combatText.SetDamageNumberScale, Close);
            _view.HideImmediate();
        }

        public void Open()
        {
            _view.Show(_settings.MasterVolume, _settings.IsMuted, _combatText.DamageNumbersEnabled, _combatText.DamageNumberScale,
                CombatTextLimits.MinDamageNumberScale, CombatTextLimits.MaxDamageNumberScale);
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
