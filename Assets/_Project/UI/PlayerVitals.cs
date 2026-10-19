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
        private readonly ReactiveProperty<bool> _hasDuality = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<float> _demonic = new ReactiveProperty<float>(0.5f);
        private readonly ReactiveProperty<float> _formThreshold = new ReactiveProperty<float>(1f);
        private readonly ReactiveProperty<bool> _hasConcentration = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<float> _concentration = new ReactiveProperty<float>();

        public ReadOnlyReactiveProperty<float> Health => _health;

        public ReadOnlyReactiveProperty<float> MaxHealth => _maxHealth;

        public ReadOnlyReactiveProperty<float> UltimateCharge => _ultimateCharge;

        public ReadOnlyReactiveProperty<bool> UltimateActive => _ultimateActive;

        public ReadOnlyReactiveProperty<bool> HasUltimate => _hasUltimate;

        public ReadOnlyReactiveProperty<bool> HasDuality => _hasDuality;

        public ReadOnlyReactiveProperty<float> Demonic => _demonic;

        public ReadOnlyReactiveProperty<float> FormThreshold => _formThreshold;

        public ReadOnlyReactiveProperty<bool> HasConcentration => _hasConcentration;

        public ReadOnlyReactiveProperty<float> Concentration => _concentration;

        public void Publish(float health, float maxHealth, bool hasUltimate, float ultimateCharge, bool ultimateActive)
        {
            _hasUltimate.Value = hasUltimate;
            _maxHealth.Value = maxHealth;
            _health.Value = health;
            _ultimateCharge.Value = ultimateCharge;
            _ultimateActive.Value = ultimateActive;
        }

        public void PublishDuality(bool hasDuality, float demonic, float formThreshold)
        {
            _formThreshold.Value = formThreshold;
            _hasDuality.Value = hasDuality;
            _demonic.Value = demonic;
        }

        public void PublishConcentration(bool hasConcentration, float fraction)
        {
            _hasConcentration.Value = hasConcentration;
            _concentration.Value = fraction;
        }

        public void Dispose()
        {
            _health.Dispose();
            _maxHealth.Dispose();
            _ultimateCharge.Dispose();
            _ultimateActive.Dispose();
            _hasUltimate.Dispose();
            _hasDuality.Dispose();
            _demonic.Dispose();
            _formThreshold.Dispose();
            _hasConcentration.Dispose();
            _concentration.Dispose();
        }
    }
}
