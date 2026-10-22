using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "KillShotConfig", menuName = "Game/Content/Kill Shot Config")]
    public sealed class KillShotConfig : PerkBehaviourConfig
    {
        [SerializeField] private KillShotLevel[] _levels = Array.Empty<KillShotLevel>();

        [Header("Aim")]
        [SerializeField, Min(0f)] private float _aimSeconds = 0.2f;
        [SerializeField, Range(1f, 45f)] private float _aimHalfAngleDegrees = 8f;
        [SerializeField, Min(1f)] private float _range = 30f;

        [Header("Flight")]
        [SerializeField, Min(1f)] private float _speed = 36f;
        [SerializeField, Min(0.05f)] private float _hitRadius = 0.5f;
        [SerializeField, Min(0f)] private float _muzzleDistance = 0.8f;
        [SerializeField, Min(1f)] private float _bossCritScale = 2f;

        [Header("View")]
        [SerializeField] private GameObject _cometPrefab;
        [SerializeField] private GameObject _impactPrefab;
        [SerializeField, Min(0.05f)] private float _impactSeconds = 1f;

        public override int LevelCount => _levels.Length;

        public float AimSeconds => _aimSeconds;

        public float AimHalfAngleDegrees => _aimHalfAngleDegrees;

        public float Range => _range;

        public float Speed => _speed;

        public float HitRadius => _hitRadius;

        public float MuzzleDistance => _muzzleDistance;

        public float BossCritScale => _bossCritScale;

        public GameObject CometPrefab => _cometPrefab;

        public GameObject ImpactPrefab => _impactPrefab;

        public float ImpactSeconds => _impactSeconds;

        public KillShotLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(KillShotConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' kill shot '{name}'";

            if (_cometPrefab == null)
                throw new InvalidOperationException($"{label} is missing a comet view prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                KillShotLevel entry = Level(level);

                if (entry == null || entry.CooldownSeconds <= 0f || entry.DamageScale <= 0f || entry.PierceDamageScale < 0f
                    || entry.ExecuteThreshold < 0f || entry.ExecuteThreshold >= 1f)
                    throw new InvalidOperationException(
                        $"{label} level {level} must have a positive cooldown and damage, no negative pierce damage and an execute threshold in [0, 1).");
            }
        }
    }

    [Serializable]
    public sealed class KillShotLevel
    {
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 12f;
        [SerializeField, Min(0.01f)] private float _damageScale = 4f;
        [SerializeField, Min(0f)] private float _pierceDamageScale = 1.5f;
        [SerializeField, Range(0f, 0.99f)] private float _executeThreshold = 0.3f;

        public float CooldownSeconds => _cooldownSeconds;

        public float DamageScale => _damageScale;

        public float PierceDamageScale => _pierceDamageScale;

        public float ExecuteThreshold => _executeThreshold;
    }
}
