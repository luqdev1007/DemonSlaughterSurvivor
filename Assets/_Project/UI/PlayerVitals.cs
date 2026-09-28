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

        public ReadOnlyReactiveProperty<float> Health => _health;

        public ReadOnlyReactiveProperty<float> MaxHealth => _maxHealth;

        public ReadOnlyReactiveProperty<float> UltimateCharge => _ultimateCharge;

        public ReadOnlyReactiveProperty<bool> UltimateActive => _ultimateActive;

        public void Publish(float health, float maxHealth, float ultimateCharge, bool ultimateActive)
        {
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
        }
    }
}
