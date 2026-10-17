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
        [SerializeField] private float _heavyStrikeWeight = 1f;

        [Header("Spawn")]
        [SerializeField] private float _minSpawnDistanceToHero = 12f;

        [Header("Blind zone while casting")]
        [SerializeField, Range(0f, 180f)] private float _blindSideFromDegrees = 60f;
        [SerializeField, Range(0f, 180f)] private float _blindBackFromDegrees = 120f;
        [SerializeField, Range(0f, 1f)] private float _blindSideDamage = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _blindBackDamage = 0f;

        [Header("Death")]
        [SerializeField] private float _deathSeconds = 4.5f;

        public string DisplayName => _displayName;

        public float GlobalAbilityCooldownSeconds => _globalAbilityCooldownSeconds;

        public float ChargeWeight => _chargeWeight;

        public float SpinWeight => _spinWeight;

        public float HeavyStrikeWeight => _heavyStrikeWeight;

        public float MinSpawnDistanceToHero => _minSpawnDistanceToHero;

        public float DeathSeconds => _deathSeconds;

        public float BlindSideFromDegrees => _blindSideFromDegrees;

        public float BlindBackFromDegrees => _blindBackFromDegrees;

        public float BlindSideDamage => _blindSideDamage;

        public float BlindBackDamage => _blindBackDamage;

        public float BlindZoneDamageScale(float angleFromFacing)
        {
            if (angleFromFacing >= _blindBackFromDegrees)
                return _blindBackDamage;

            if (angleFromFacing >= _blindSideFromDegrees)
                return _blindSideDamage;

            return 1f;
        }

        public void Validate(EnemyConfig owner)
        {
            string label = $"{nameof(EnemyConfig)} '{(owner == null ? "<none>" : owner.Id)}' boss '{Id}'";

            if (string.IsNullOrWhiteSpace(_displayName))
                throw new InvalidOperationException($"{label} has no display name; the boss health bar would show an empty title.");

            RequireNonNegative(label, nameof(_globalAbilityCooldownSeconds), _globalAbilityCooldownSeconds);
            RequireNonNegative(label, nameof(_chargeWeight), _chargeWeight);
            RequireNonNegative(label, nameof(_spinWeight), _spinWeight);
            RequireNonNegative(label, nameof(_heavyStrikeWeight), _heavyStrikeWeight);
            RequireNonNegative(label, nameof(_minSpawnDistanceToHero), _minSpawnDistanceToHero);

            if (_blindBackFromDegrees < _blindSideFromDegrees)
                throw new InvalidOperationException(
                    $"{label} starts the blind back at {_blindBackFromDegrees} deg, before the blind side at {_blindSideFromDegrees} deg; the side zone would vanish.");

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
