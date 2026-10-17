using Game.Core;
using R3;
using System;
using UnityEngine;

namespace Game.UI
{
    public sealed class BossVitals : IBossVitalsSink, IDisposable
    {
        private readonly ReactiveProperty<bool> _visible = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<string> _displayName = new ReactiveProperty<string>();
        private readonly ReactiveProperty<float> _health = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _maxHealth = new ReactiveProperty<float>();

        public ReadOnlyReactiveProperty<bool> Visible => _visible;

        public ReadOnlyReactiveProperty<string> DisplayName => _displayName;

        public ReadOnlyReactiveProperty<float> Health => _health;

        public ReadOnlyReactiveProperty<float> MaxHealth => _maxHealth;

        public Vector3 Position { get; private set; }

        public float DistanceToHero { get; private set; }

        public void Publish(bool visible, string displayName, float health, float maxHealth, Vector3 position, float distanceToHero)
        {
            Position = position;
            DistanceToHero = distanceToHero;

            if (visible)
            {
                _displayName.Value = displayName;
                _maxHealth.Value = maxHealth;
                _health.Value = health;
            }

            _visible.Value = visible;
        }

        public void Dispose()
        {
            _visible.Dispose();
            _displayName.Dispose();
            _health.Dispose();
            _maxHealth.Dispose();
        }
    }
}
