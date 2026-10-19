using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "StaffConfig", menuName = "Game/Content/Staff Config")]
    public sealed class StaffConfig : ContentConfig
    {
        [Header("Cast")]
        [SerializeField, Min(0f)] private float _damage = 20f;
        [SerializeField, Min(0.01f)] private float _cooldownSeconds = 1.2f;
        [SerializeField, Min(0.01f)] private float _castSeconds = 0.45f;
        [SerializeField, Min(0.1f)] private float _range = 15f;
        [SerializeField, Range(0f, 90f)] private float _aimToleranceDegrees = 15f;

        [Header("Fireball")]
        [SerializeField] private GameObject _fireballPrefab;
        [SerializeField, Min(0.1f)] private float _fireballSpeed = 14f;
        [SerializeField, Min(0.1f)] private float _fireballRange = 16f;
        [SerializeField, Min(0.01f)] private float _fireballRadius = 0.3f;
        [SerializeField, Min(0f)] private float _muzzleDistance = 0.8f;

        [Header("Explosion")]
        [SerializeField, Min(0.1f)] private float _explosionRadius = 2.5f;
        [SerializeField, Min(0f)] private float _explosionDamageScale = 0.6f;
        [SerializeField, Min(0f)] private float _explosionPushSpeed = 6f;
        [SerializeField, Min(0f)] private float _explosionPushSeconds = 0.33f;
        [SerializeField, Min(0f)] private float _explosionPushArcHeight = 0.3f;
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField, Min(0.05f)] private float _explosionFxSeconds = 1.5f;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float _releaseClipSeconds = 0.82f;
        [SerializeField] private string _castTrigger = "CastFireball";
        [SerializeField] private string _cancelTrigger = "CastCancel";

        public float Damage => _damage;

        public float CooldownSeconds => _cooldownSeconds;

        public float CastSeconds => _castSeconds;

        public float Range => _range;

        public float AimToleranceDegrees => _aimToleranceDegrees;

        public GameObject FireballPrefab => _fireballPrefab;

        public float FireballSpeed => _fireballSpeed;

        public float FireballRange => _fireballRange;

        public float FireballRadius => _fireballRadius;

        public float MuzzleDistance => _muzzleDistance;

        public float ExplosionRadius => _explosionRadius;

        public float ExplosionDamageScale => _explosionDamageScale;

        public float ExplosionPushSpeed => _explosionPushSpeed;

        public float ExplosionPushSeconds => _explosionPushSeconds;

        public float ExplosionPushArcHeight => _explosionPushArcHeight;

        public GameObject ExplosionPrefab => _explosionPrefab;

        public float ExplosionFxSeconds => _explosionFxSeconds;

        public float ReleaseClipSeconds => _releaseClipSeconds;

        public string CastTrigger => _castTrigger;

        public string CancelTrigger => _cancelTrigger;

        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
                throw new InvalidOperationException($"{nameof(StaffConfig)} '{name}' has no id.");

            if (_fireballPrefab == null || _explosionPrefab == null)
                throw new InvalidOperationException($"{nameof(StaffConfig)} '{Id}' is missing its fireball or explosion prefab.");

            if (_castSeconds > _cooldownSeconds)
                throw new InvalidOperationException(
                    $"{nameof(StaffConfig)} '{Id}' casts for {_castSeconds} s, longer than its {_cooldownSeconds} s cooldown; " +
                    "the cast period would silently become the cast time.");
        }
    }
}
