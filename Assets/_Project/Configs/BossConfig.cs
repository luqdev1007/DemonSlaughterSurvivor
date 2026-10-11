using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "BossConfig", menuName = "Game/Content/Boss Config")]
    public sealed class BossConfig : ContentConfig
    {
        [SerializeField] private string _displayName = "Boss";

        [Header("Abilities")]
        [SerializeField] private float _globalAbilityCooldownSeconds = 3f;
        [SerializeField] private float _chargeWeight = 1f;
        [SerializeField] private float _spinWeight = 1f;

        [Header("Spawn")]
        [SerializeField] private float _minSpawnDistanceToHero = 12f;

        [Header("Death")]
        [SerializeField] private float _deathSeconds = 4.5f;

        public string DisplayName => _displayName;

        public float GlobalAbilityCooldownSeconds => _globalAbilityCooldownSeconds;

        public float ChargeWeight => _chargeWeight;

        public float SpinWeight => _spinWeight;

        public float MinSpawnDistanceToHero => _minSpawnDistanceToHero;

        public float DeathSeconds => _deathSeconds;

        public void Validate(EnemyConfig owner)
        {
            string label = $"{nameof(EnemyConfig)} '{(owner == null ? "<none>" : owner.Id)}' boss '{Id}'";

            if (string.IsNullOrWhiteSpace(_displayName))
                throw new InvalidOperationException($"{label} has no display name; the boss health bar would show an empty title.");

            RequireNonNegative(label, nameof(_globalAbilityCooldownSeconds), _globalAbilityCooldownSeconds);
            RequireNonNegative(label, nameof(_chargeWeight), _chargeWeight);
            RequireNonNegative(label, nameof(_spinWeight), _spinWeight);
            RequireNonNegative(label, nameof(_minSpawnDistanceToHero), _minSpawnDistanceToHero);

            if (float.IsNaN(_deathSeconds) || float.IsInfinity(_deathSeconds) || _deathSeconds <= 0f)
                throw new InvalidOperationException(
                    $"{label} has {nameof(_deathSeconds)} {_deathSeconds}; it must be positive, otherwise the run is won on the tick the boss dies " +
                    "and its death clip never plays.");
        }

        private static void RequireNonNegative(string label, string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new InvalidOperationException($"{label} has {field} {value}; it must be zero or positive.");
        }
    }
}
