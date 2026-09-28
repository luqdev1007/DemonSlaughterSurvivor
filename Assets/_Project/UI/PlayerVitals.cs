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

        public ReadOnlyReactiveProperty<float> Health => _health;

        public ReadOnlyReactiveProperty<float> MaxHealth => _maxHealth;

        public ReadOnlyReactiveProperty<float> UltimateCharge => _ultimateCharge;

        public void Publish(float health, float maxHealth, float ultimateCharge)
        {
            _maxHealth.Value = maxHealth;
            _health.Value = health;
            _ultimateCharge.Value = ultimateCharge;
        }

        public void Dispose()
        {
            _health.Dispose();
            _maxHealth.Dispose();
            _ultimateCharge.Dispose();
        }
    }
}
