using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "HeavyStrikeAbilityConfig", menuName = "Game/Content/Heavy Strike Ability Config")]
    public sealed class HeavyStrikeAbilityConfig : ContentConfig
    {
        [Header("Trigger")]
        [SerializeField] private float _triggerRange = 5f;
        [SerializeField] private float _cooldownSeconds = 8f;

        [Header("Phases")]
        [SerializeField] private float _windupSeconds = 1.8f;
        [SerializeField] private float _lockSeconds = 0.5f;
        [SerializeField] private float _recoverSeconds = 2f;
        [SerializeField] private float _windupTurnSpeedDegrees = 90f;

        [Header("Impact")]
        [SerializeField] private float _strikeDistance = 3f;
        [SerializeField] private float _radius = 2f;
        [SerializeField] private float _damage = 45f;

        [Header("View")]
        [SerializeField] private float _impactClipSeconds = 2.35f;
        [SerializeField] private float _clipSeconds = 4.8f;
        [SerializeField, Range(0f, 1f)] private float _markerStartScale = 0.25f;

        public float TriggerRange => _triggerRange;

        public float CooldownSeconds => _cooldownSeconds;

        public float WindupSeconds => _windupSeconds;

        public float LockSeconds => _lockSeconds;

        public float RecoverSeconds => _recoverSeconds;

        public float WindupTurnSpeedDegrees => _windupTurnSpeedDegrees;

        public float StrikeDistance => _strikeDistance;

        public float Radius => _radius;

        public float Damage => _damage;

        public float ImpactClipSeconds => _impactClipSeconds;

        public float ClipSeconds => _clipSeconds;

        public float MarkerStartScale => _markerStartScale;

        public void Validate(EnemyConfig owner)
        {
            string label = $"{nameof(EnemyConfig)} '{(owner == null ? "<none>" : owner.Id)}' heavy strike '{Id}'";

            RequirePositive(label, nameof(_triggerRange), _triggerRange);
            RequireNonNegative(label, nameof(_cooldownSeconds), _cooldownSeconds);
            RequirePositive(label, nameof(_windupSeconds), _windupSeconds);
            RequirePositive(label, nameof(_lockSeconds), _lockSeconds);
            RequirePositive(label, nameof(_recoverSeconds), _recoverSeconds);
            RequireNonNegative(label, nameof(_windupTurnSpeedDegrees), _windupTurnSpeedDegrees);
            RequireNonNegative(label, nameof(_strikeDistance), _strikeDistance);
            RequirePositive(label, nameof(_radius), _radius);
            RequireNonNegative(label, nameof(_damage), _damage);
            RequirePositive(label, nameof(_impactClipSeconds), _impactClipSeconds);

            if (float.IsNaN(_clipSeconds) || _clipSeconds <= _impactClipSeconds)
                throw new InvalidOperationException(
                    $"{label} has {nameof(_clipSeconds)} {_clipSeconds}, not longer than {nameof(_impactClipSeconds)} {_impactClipSeconds}; " +
                    "the recover phase plays the rest of the clip after the impact.");
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
