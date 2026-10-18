using Game.Core;
using R3;
using System;

namespace Game.UI
{
    public sealed class PlayerVitals : IPlayerVitalsSink, IDisposable
    {
        private readonly ReactiveProperty<float> _health = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _maxHealth = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _ultimateCharge = new ReactiveProperty<float>();
        private readonly ReactiveProperty<bool> _ultimateActive = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> _hasUltimate = new ReactiveProperty<bool>(true);

        public ReadOnlyReactiveProperty<float> Health => _health;

        public ReadOnlyReactiveProperty<float> MaxHealth => _maxHealth;

        public ReadOnlyReactiveProperty<float> UltimateCharge => _ultimateCharge;

        public ReadOnlyReactiveProperty<bool> UltimateActive => _ultimateActive;

        public ReadOnlyReactiveProperty<bool> HasUltimate => _hasUltimate;

        public void Publish(float health, float maxHealth, bool hasUltimate, float ultimateCharge, bool ultimateActive)
        {
            _hasUltimate.Value = hasUltimate;
            _maxHealth.Value = maxHealth;
            _health.Value = health;
            _ultimateCharge.Value = ultimateCharge;
            _ultimateActive.Value = ultimateActive;
        }

        public void Dispose()
        {
            _health.Dispose();
            _maxHealth.Dispose();
            _ultimateCharge.Dispose();
            _ultimateActive.Dispose();
            _hasUltimate.Dispose();
        }
    }
}
