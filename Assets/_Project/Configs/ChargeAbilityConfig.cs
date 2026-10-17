using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ChargeAbilityConfig", menuName = "Game/Content/Charge Ability Config")]
    public sealed class ChargeAbilityConfig : ContentConfig
    {
        [Header("Trigger")]
        [SerializeField] private float _minRange = 6f;
        [SerializeField] private float _maxRange = 18f;
        [SerializeField] private float _cooldownSeconds = 8f;

        [Header("Windup")]
        [SerializeField] private float _windupSeconds = 1f;
        [SerializeField] private float _windupTurnSpeedDegrees = 120f;
        [SerializeField] private float _lockSeconds = 0.35f;

        [Header("Rush")]
        [SerializeField] private float _rushSpeed = 20f;
        [SerializeField] private float _rushSeconds = 0.5f;
        [SerializeField] private float _damage = 60f;
        [SerializeField] private float _hitRadiusExtra = 0.3f;
        [SerializeField] private float _recoverSeconds = 1f;

        [Header("Homing")]
        [SerializeField, Range(0f, 1f)] private float _homingStartFraction = 0.5f;
        [SerializeField] private float _homingTurnSpeedDegrees = 120f;
        [SerializeField] private float _homingMaxDegrees = 30f;
        [SerializeField] private float _homingJitterDegrees = 12f;

        [Header("Animation")]
        [SerializeField] private float _rushClipSeconds = 0.4f;

        public float MinRange => _minRange;

        public float HomingStartFraction => _homingStartFraction;

        public float HomingTurnSpeedDegrees => _homingTurnSpeedDegrees;

        public float HomingMaxDegrees => _homingMaxDegrees;

        public float HomingJitterDegrees => _homingJitterDegrees;

        public float MaxRange => _maxRange;

        public float CooldownSeconds => _cooldownSeconds;

        public float WindupSeconds => _windupSeconds;

        public float WindupTurnSpeedDegrees => _windupTurnSpeedDegrees;

        public float LockSeconds => _lockSeconds;

        public float RushSpeed => _rushSpeed;

        public float RushSeconds => _rushSeconds;

        public float Damage => _damage;

        public float HitRadiusExtra => _hitRadiusExtra;

        public float RecoverSeconds => _recoverSeconds;

        public float RushClipSeconds => _rushClipSeconds;

        public float TelegraphLength => _rushSpeed * _rushSeconds;

        public void Validate(EnemyConfig owner)
        {
            string label = $"{nameof(EnemyConfig)} '{(owner == null ? "<none>" : owner.Id)}' charge '{Id}'";

            RequireNonNegative(label, nameof(_minRange), _minRange);

            if (float.IsNaN(_maxRange) || _maxRange <= _minRange)
                throw new InvalidOperationException($"{label} has {nameof(_maxRange)} {_maxRange} not above {nameof(_minRange)} {_minRange}; the charge could never start.");

            RequireNonNegative(label, nameof(_cooldownSeconds), _cooldownSeconds);
            RequirePositive(label, nameof(_windupSeconds), _windupSeconds);
            RequireNonNegative(label, nameof(_windupTurnSpeedDegrees), _windupTurnSpeedDegrees);
            RequirePositive(label, nameof(_lockSeconds), _lockSeconds);
            RequirePositive(label, nameof(_rushSpeed), _rushSpeed);
            RequirePositive(label, nameof(_rushSeconds), _rushSeconds);
            RequirePositive(label, nameof(_damage), _damage);
            RequireNonNegative(label, nameof(_hitRadiusExtra), _hitRadiusExtra);
            RequirePositive(label, nameof(_recoverSeconds), _recoverSeconds);
            RequirePositive(label, nameof(_rushClipSeconds), _rushClipSeconds);
            RequireNonNegative(label, nameof(_homingTurnSpeedDegrees), _homingTurnSpeedDegrees);
            RequireNonNegative(label, nameof(_homingMaxDegrees), _homingMaxDegrees);
            RequireNonNegative(label, nameof(_homingJitterDegrees), _homingJitterDegrees);
        }

        private static void RequirePositive(string label, string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new InvalidOperationException($"{label} has {field} {value}; it must be positive.");
        }

        private static void RequireNonNegative(string label, string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new InvalidOperationException($"{label} has {field} {value}; it must be zero or positive.");
        }
    }
}
