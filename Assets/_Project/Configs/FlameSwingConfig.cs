using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "FlameSwingConfig", menuName = "Game/Content/Flame Swing Config")]
    public sealed class FlameSwingConfig : PerkBehaviourConfig
    {
        [SerializeField] private FlameSwingLevel[] _levels = Array.Empty<FlameSwingLevel>();
        [SerializeField] private bool _locksMovement = true;

        [Header("Trigger")]
        [SerializeField] private float _triggerRange = 6f;
        [SerializeField] private float _triggerHalfAngleDegrees = 60f;
        [SerializeField] private int _minEnemies = 2;

        [Header("Animation")]
        [SerializeField] private string _animatorTrigger = "FlameSwing";
        [SerializeField] private float _clipSeconds = 1.25f;
        [SerializeField] private float _releaseClipSeconds = 0.67f;
        [SerializeField] private float _playbackSpeed = 1.56f;

        [Header("Wave")]
        [SerializeField] private float _startRadius = 1.2f;
        [SerializeField] private float _speed = 9f;
        [SerializeField] private float _halfAngleDegrees = 50f;
        [SerializeField] private float _thickness = 0.8f;
        [SerializeField] private int _pillarCount = 15;
        [SerializeField] private GameObject _pillarPrefab;

        public override int LevelCount => _levels.Length;

        public bool LocksMovement => _locksMovement;

        public float TriggerRange => _triggerRange;

        public float TriggerHalfAngleDegrees => _triggerHalfAngleDegrees;

        public int MinEnemies => _minEnemies;

        public string AnimatorTrigger => _animatorTrigger;

        public float ClipSeconds => _clipSeconds;

        public float ReleaseClipSeconds => _releaseClipSeconds;

        public float PlaybackSpeed => _playbackSpeed;

        public float StartRadius => _startRadius;

        public float Speed => _speed;

        public float HalfAngleDegrees => _halfAngleDegrees;

        public float Thickness => _thickness;

        public int PillarCount => _pillarCount;

        public GameObject PillarPrefab => _pillarPrefab;

        public FlameSwingLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(FlameSwingConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' flame swing '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                FlameSwingLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                RequirePositive(label, $"level {level} cooldown", entry.CooldownSeconds);
                RequirePositive(label, $"level {level} damage share", entry.DamageShare);

                if (float.IsNaN(entry.Distance) || entry.Distance <= _startRadius)
                    throw new InvalidOperationException(
                        $"{label} level {level} travels to {entry.Distance} m, not beyond its start radius {_startRadius} m.");
            }

            RequirePositive(label, nameof(_triggerRange), _triggerRange);
            RequirePositive(label, nameof(_triggerHalfAngleDegrees), _triggerHalfAngleDegrees);
            RequirePositive(label, nameof(_clipSeconds), _clipSeconds);
            RequirePositive(label, nameof(_playbackSpeed), _playbackSpeed);
            RequirePositive(label, nameof(_speed), _speed);
            RequirePositive(label, nameof(_halfAngleDegrees), _halfAngleDegrees);
            RequirePositive(label, nameof(_thickness), _thickness);

            if (_minEnemies < 1)
                throw new InvalidOperationException($"{label} has {nameof(_minEnemies)} {_minEnemies}; at least one enemy must be ahead.");

            if (float.IsNaN(_releaseClipSeconds) || _releaseClipSeconds < 0f || _releaseClipSeconds >= _clipSeconds)
                throw new InvalidOperationException($"{label} releases the wave at {_releaseClipSeconds} s, outside the attack of {_clipSeconds} s.");

            if (string.IsNullOrWhiteSpace(_animatorTrigger))
                throw new InvalidOperationException($"{label} has no animator trigger.");

            if (_pillarCount < 1)
                throw new InvalidOperationException($"{label} has {_pillarCount} flame pillars; the wave needs at least one.");

            if (_pillarPrefab == null)
                throw new InvalidOperationException($"{label} has no flame pillar prefab.");
        }

        private static void RequirePositive(string label, string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new InvalidOperationException($"{label} has {field} {value}; it must be positive.");
        }
    }

    [Serializable]
    public sealed class FlameSwingLevel
    {
        [SerializeField] private float _cooldownSeconds = 8f;
        [SerializeField] private float _damageShare = 1.5f;
        [SerializeField] private float _distance = 8f;

        public float CooldownSeconds => _cooldownSeconds;

        public float DamageShare => _damageShare;

        public float Distance => _distance;
    }
}
