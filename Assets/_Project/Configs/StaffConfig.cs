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

        [Header("Explosion")]
        [SerializeField, Min(0.1f)] private float _explosionRadius = 2.5f;
        [SerializeField, Min(0f)] private float _explosionDamageScale = 0.6f;
        [SerializeField, Min(0f)] private float _explosionPushSpeed = 6f;
        [SerializeField, Min(0f)] private float _explosionPushSeconds = 0.33f;
        [SerializeField, Min(0f)] private float _explosionPushArcHeight = 0.3f;
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField, Min(0.05f)] private float _explosionFxSeconds = 1.5f;

        [Header("Animation")]
        [SerializeField] private StaffCastVariant[] _casts = Array.Empty<StaffCastVariant>();
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

        public float ExplosionRadius => _explosionRadius;

        public float ExplosionDamageScale => _explosionDamageScale;

        public float ExplosionPushSpeed => _explosionPushSpeed;

        public float ExplosionPushSeconds => _explosionPushSeconds;

        public float ExplosionPushArcHeight => _explosionPushArcHeight;

        public GameObject ExplosionPrefab => _explosionPrefab;

        public float ExplosionFxSeconds => _explosionFxSeconds;

        public int CastCount => _casts.Length;

        public StaffCastVariant Cast(int index)
        {
            return _casts[index];
        }

        public string CancelTrigger => _cancelTrigger;

        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
                throw new InvalidOperationException($"{nameof(StaffConfig)} '{name}' has no id.");

            if (_fireballPrefab == null || _explosionPrefab == null)
                throw new InvalidOperationException($"{nameof(StaffConfig)} '{Id}' is missing its fireball or explosion prefab.");

            if (_casts.Length == 0)
                throw new InvalidOperationException($"{nameof(StaffConfig)} '{Id}' has no cast variants.");

            for (int index = 0; index < _casts.Length; index++)
            {
                if (_casts[index] == null || string.IsNullOrEmpty(_casts[index].Trigger) || _casts[index].ReleaseClipSeconds <= 0f)
                    throw new InvalidOperationException($"{nameof(StaffConfig)} '{Id}' cast {index} needs a trigger and a positive release time.");
            }

            if (_castSeconds > _cooldownSeconds)
                throw new InvalidOperationException(
                    $"{nameof(StaffConfig)} '{Id}' casts for {_castSeconds} s, longer than its {_cooldownSeconds} s cooldown; " +
                    "the cast period would silently become the cast time.");
        }
    }

    [Serializable]
    public sealed class StaffCastVariant
    {
        [SerializeField] private string _trigger = "CastFireballR1";
        [SerializeField, Min(0.01f)] private float _releaseClipSeconds = 0.78f;
        [SerializeField] private Vector3 _muzzle = new Vector3(0.4f, 1.1f, 0.45f);

        public string Trigger => _trigger;

        public float ReleaseClipSeconds => _releaseClipSeconds;

        public Vector3 Muzzle => _muzzle;

        public bool LeftHand => _muzzle.x < 0f;
    }
}
