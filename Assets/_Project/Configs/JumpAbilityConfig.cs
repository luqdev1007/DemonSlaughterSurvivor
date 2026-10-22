using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "JumpAbilityConfig", menuName = "Game/Content/Jump Ability Config")]
    public sealed class JumpAbilityConfig : ContentConfig
    {
        [Header("Trigger")]
        [SerializeField] private float _triggerRange = 6f;
        [SerializeField] private float _cooldownSeconds = 7f;
        [SerializeField] private float _behindRange = 5f;
        [SerializeField, Min(1f)] private float _behindWeightScale = 5f;

        [Header("Flight")]
        [SerializeField] private float _turnSpeedDegrees = 360f;
        [SerializeField, Min(0f)] private float _liftHeight = 0.9f;
        [SerializeField, Min(0f)] private float _liftStartClipSeconds = 0.45f;

        [Header("Impact")]
        [SerializeField] private float _radius = 4.5f;
        [SerializeField] private float _damage = 40f;
        [SerializeField] private GameObject _impactFxPrefab;
        [SerializeField, Min(0.05f)] private float _impactFxSeconds = 1.5f;
        [SerializeField, Min(0.05f)] private float _impactFxScale = 1.8f;

        [Header("View")]
        [SerializeField] private float _impactClipSeconds = 1.15f;
        [SerializeField] private float _clipSeconds = 2.17f;
        [SerializeField] private float _playbackSpeed = 1.25f;

        public float TriggerRange => _triggerRange;

        public float CooldownSeconds => _cooldownSeconds;

        public float BehindRange => _behindRange;

        public float BehindWeightScale => _behindWeightScale;

        public float TurnSpeedDegrees => _turnSpeedDegrees;

        public float LiftHeight => _liftHeight;

        public float LiftStartClipSeconds => _liftStartClipSeconds;

        public float Radius => _radius;

        public float Damage => _damage;

        public GameObject ImpactFxPrefab => _impactFxPrefab;

        public float ImpactFxSeconds => _impactFxSeconds;

        public float ImpactFxScale => _impactFxScale;

        public float ImpactClipSeconds => _impactClipSeconds;

        public float ClipSeconds => _clipSeconds;

        public float PlaybackSpeed => _playbackSpeed;

        public float AirSeconds => _impactClipSeconds / _playbackSpeed;

        public float RecoverSeconds => (_clipSeconds - _impactClipSeconds) / _playbackSpeed;

        public float Lift(float airProgress)
        {
            float start = Mathf.Clamp01(_liftStartClipSeconds / _impactClipSeconds);

            if (airProgress <= start || start >= 1f)
                return 0f;

            return Mathf.Sin(Mathf.PI * Mathf.Clamp01((airProgress - start) / (1f - start))) * _liftHeight;
        }

        public void Validate(EnemyConfig owner)
        {
            string label = $"{nameof(EnemyConfig)} '{(owner == null ? "<none>" : owner.Id)}' jump '{Id}'";

            RequirePositive(label, nameof(_triggerRange), _triggerRange);
            RequirePositive(label, nameof(_radius), _radius);
            RequirePositive(label, nameof(_impactClipSeconds), _impactClipSeconds);
            RequirePositive(label, nameof(_playbackSpeed), _playbackSpeed);

            if (float.IsNaN(_cooldownSeconds) || _cooldownSeconds < 0f || float.IsNaN(_damage) || _damage < 0f)
                throw new InvalidOperationException($"{label} has a negative cooldown or damage.");

            if (float.IsNaN(_clipSeconds) || _clipSeconds <= _impactClipSeconds)
                throw new InvalidOperationException(
                    $"{label} has {nameof(_clipSeconds)} {_clipSeconds}, not longer than {nameof(_impactClipSeconds)} {_impactClipSeconds}; " +
                    "the recover phase plays the rest of the clip after the landing.");
        }

        private static void RequirePositive(string label, string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new InvalidOperationException($"{label} has {field} {value}; it must be positive.");
        }
    }
}
