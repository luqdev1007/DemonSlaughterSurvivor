using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "RapidFireConfig", menuName = "Game/Content/Rapid Fire Config")]
    public sealed class RapidFireConfig : PerkBehaviourConfig
    {
        [SerializeField] private RapidFireLevel[] _levels = Array.Empty<RapidFireLevel>();
        [SerializeField, Range(0f, 1f)] private float _demonicThreshold = 0.6f;

        public override int LevelCount => _levels.Length;

        public float DemonicThreshold => _demonicThreshold;

        public RapidFireLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(RapidFireConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' rapid fire '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                RapidFireLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.IntervalSeconds) || float.IsInfinity(entry.IntervalSeconds) || entry.IntervalSeconds <= 0f)
                    throw new InvalidOperationException($"{label} level {level} has interval {entry.IntervalSeconds} s; it must be positive.");

                if (float.IsNaN(entry.DurationSeconds) || entry.DurationSeconds <= 0f || entry.DurationSeconds > entry.IntervalSeconds)
                    throw new InvalidOperationException(
                        $"{label} level {level} lasts {entry.DurationSeconds} s every {entry.IntervalSeconds} s; it must be positive and not longer than the interval.");

                if (float.IsNaN(entry.AttackSpeed) || entry.AttackSpeed <= 0f)
                    throw new InvalidOperationException($"{label} level {level} has attack speed {entry.AttackSpeed}; it must be positive.");

                if (float.IsNaN(entry.DemonicAttackSpeed) || entry.DemonicAttackSpeed < 0f)
                    throw new InvalidOperationException($"{label} level {level} has demonic bonus {entry.DemonicAttackSpeed}; it must be zero or positive.");
            }
        }
    }

    [Serializable]
    public sealed class RapidFireLevel
    {
        [SerializeField] private float _intervalSeconds = 10f;
        [SerializeField] private float _durationSeconds = 4f;
        [SerializeField] private float _attackSpeed = 0.3f;
        [SerializeField] private float _demonicAttackSpeed = 0.2f;

        public float IntervalSeconds => _intervalSeconds;

        public float DurationSeconds => _durationSeconds;

        public float AttackSpeed => _attackSpeed;

        public float DemonicAttackSpeed => _demonicAttackSpeed;
    }
}
