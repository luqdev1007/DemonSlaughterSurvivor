using Game.Core;
using R3;
using System;

namespace Game.UI
{
    public sealed class PlayerVitals : IPlayerVitalsSink, IDisposable
    {
        private readonly ReactiveProperty<float> _health = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _maxHealth = new ReactiveProperty<float>();

        public ReadOnlyReactiveProperty<float> Health => _health;

        public ReadOnlyReactiveProperty<float> MaxHealth => _maxHealth;

        public void Publish(float health, float maxHealth)
        {
            _maxHealth.Value = maxHealth;
            _health.Value = health;
        }

        public void Dispose()
        {
            _health.Dispose();
            _maxHealth.Dispose();
        }
    }
}
