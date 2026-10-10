using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "RangedAttackConfig", menuName = "Game/Content/Ranged Attack Config")]
    public sealed class RangedAttackConfig : ContentConfig
    {
        [Header("Trigger")]
        [SerializeField] private float _range = 8f;
        [SerializeField] private float _windupSeconds = 0.4f;
        [SerializeField] private float _cooldownSeconds = 2.5f;

        [Header("Animation")]
        [SerializeField] private string _animatorTrigger = "Shoot";
        [SerializeField] private float _releaseClipSeconds = 0.4f;

        [Header("Bolt")]
        [SerializeField] private float _boltSpeed = 10f;
        [SerializeField] private float _boltRadius = 0.2f;
        [SerializeField] private float _boltRange = 14f;
        [SerializeField] private float _boltDamage = 10f;
        [SerializeField] private GameObject _boltPrefab;

        public float Range => _range;

        public float WindupSeconds => _windupSeconds;

        public float CooldownSeconds => _cooldownSeconds;

        public string AnimatorTrigger => _animatorTrigger;

        public float ReleaseClipSeconds => _releaseClipSeconds;

        public float BoltSpeed => _boltSpeed;

        public float BoltRadius => _boltRadius;

        public float BoltRange => _boltRange;

        public float BoltDamage => _boltDamage;

        public GameObject BoltPrefab => _boltPrefab;

        public void Validate(EnemyConfig owner)
        {
            string label = $"{nameof(EnemyConfig)} '{(owner == null ? "<none>" : owner.Id)}' ranged attack '{Id}'";

            RequirePositive(label, nameof(_range), _range, "the archer would never see the hero and only chase");
            RequirePositive(label, nameof(_windupSeconds), _windupSeconds, "the shot would leave on the tick the windup starts, with no animation to read");
            RequirePositive(label, nameof(_releaseClipSeconds), _releaseClipSeconds, "the animator playback speed is derived from it");
            RequirePositive(label, nameof(_boltSpeed), _boltSpeed, "the bolt would hang where it was shot");
            RequirePositive(label, nameof(_boltRadius), _boltRadius, "the bolt could only hit by touching the hero's centre");
            RequirePositive(label, nameof(_boltRange), _boltRange, "the bolt would vanish on the tick it is shot");
            RequirePositive(label, nameof(_boltDamage), _boltDamage, "ApplyDamageSystem drops events that deal nothing");

            if (float.IsNaN(_cooldownSeconds) || float.IsInfinity(_cooldownSeconds) || _cooldownSeconds < 0f)
                throw new InvalidOperationException($"{label} has {nameof(_cooldownSeconds)} {_cooldownSeconds}; it must be zero or positive.");

            if (string.IsNullOrEmpty(_animatorTrigger))
                throw new InvalidOperationException($"{label} has no animator trigger; the windup would play nothing on screen.");

            if (_boltPrefab == null)
                throw new InvalidOperationException($"{label} has no bolt prefab.");
        }

        private static void RequirePositive(string label, string field, float value, string consequence)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new InvalidOperationException($"{label} has {field} {value}; it must be positive, otherwise {consequence}.");
        }
    }
}
