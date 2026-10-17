using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ExecutionConfig", menuName = "Game/Content/Execution Config")]
    public sealed class ExecutionConfig : PerkBehaviourConfig
    {
        [SerializeField] private ExecutionLevel[] _levels = Array.Empty<ExecutionLevel>();
        [SerializeField] private bool _locksMovement = true;

        [Header("Target")]
        [SerializeField] private float _reach = 3f;
        [SerializeField] private float _sectorHalfAngleDegrees = 45f;
        [SerializeField] private float _turnSpeedDegrees = 540f;

        [Header("Damage")]
        [SerializeField, Range(0f, 1f)] private float _lowHealthThreshold = 0.3f;
        [SerializeField] private float _lowHealthMultiplier = 2f;

        [Header("Elite targets")]
        [FormerlySerializedAs("_ordinaryCooldownMultiplier")]
        [SerializeField] private float _eliteCooldownMultiplier = 0.5f;

        [Header("Animation")]
        [SerializeField] private string _animatorTrigger = "AttackFlip";
        [SerializeField] private float _clipSeconds = 1.45f;
        [SerializeField] private float _playbackSpeed = 1f;
        [SerializeField] private float[] _hitClipSeconds = { 0.45f, 0.6f, 1f };

        public override int LevelCount => _levels.Length;

        public bool LocksMovement => _locksMovement;

        public float Reach => _reach;

        public float SectorHalfAngleDegrees => _sectorHalfAngleDegrees;

        public float TurnSpeedDegrees => _turnSpeedDegrees;

        public float LowHealthThreshold => _lowHealthThreshold;

        public float LowHealthMultiplier => _lowHealthMultiplier;

        public float EliteCooldownMultiplier => _eliteCooldownMultiplier;

        public string AnimatorTrigger => _animatorTrigger;

        public float ClipSeconds => _clipSeconds;

        public float PlaybackSpeed => _playbackSpeed;

        public int HitCount => _hitClipSeconds.Length;

        public float HitClipSeconds(int index)
        {
            return _hitClipSeconds[index];
        }

        public ExecutionLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(ExecutionConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public float HitDamage(ExecutionLevel level, float currentHealth, float maxHealth)
        {
            float damage = level.HealthShare * Mathf.Max(0f, currentHealth) + level.FlatDamage;

            if (maxHealth > 0f && currentHealth < maxHealth * _lowHealthThreshold)
                damage *= _lowHealthMultiplier;

            return damage;
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' execution '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                ExecutionLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                RequirePositive(label, $"level {level} cooldown", entry.CooldownSeconds);
                RequireNonNegative(label, $"level {level} health share", entry.HealthShare);
                RequireNonNegative(label, $"level {level} flat damage", entry.FlatDamage);
            }

            RequirePositive(label, nameof(_reach), _reach);
            RequirePositive(label, nameof(_sectorHalfAngleDegrees), _sectorHalfAngleDegrees);
            RequireNonNegative(label, nameof(_turnSpeedDegrees), _turnSpeedDegrees);
            RequirePositive(label, nameof(_lowHealthMultiplier), _lowHealthMultiplier);
            RequirePositive(label, nameof(_eliteCooldownMultiplier), _eliteCooldownMultiplier);

            RequirePositive(label, nameof(_clipSeconds), _clipSeconds);
            RequirePositive(label, nameof(_playbackSpeed), _playbackSpeed);

            if (string.IsNullOrWhiteSpace(_animatorTrigger))
                throw new InvalidOperationException($"{label} has no animator trigger; the hero would stand still while dealing damage.");

            if (_hitClipSeconds == null || _hitClipSeconds.Length == 0)
                throw new InvalidOperationException($"{label} has no hit moments; the attack would deal no damage.");

            for (int index = 0; index < _hitClipSeconds.Length; index++)
            {
                float time = _hitClipSeconds[index];

                if (float.IsNaN(time) || time < 0f || time >= _clipSeconds)
                    throw new InvalidOperationException($"{label} hit {index} is at {time} s, outside the clip of {_clipSeconds} s.");

                if (index > 0 && time <= _hitClipSeconds[index - 1])
                    throw new InvalidOperationException($"{label} hit {index} at {time} s is not after hit {index - 1}; the hits must be in clip order.");
            }
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

    [Serializable]
    public sealed class ExecutionLevel
    {
        [SerializeField] private float _cooldownSeconds = 20f;
        [SerializeField, Range(0f, 1f)] private float _healthShare = 0.03f;
        [SerializeField] private float _flatDamage = 20f;

        public float CooldownSeconds => _cooldownSeconds;

        public float HealthShare => _healthShare;

        public float FlatDamage => _flatDamage;
    }
}
