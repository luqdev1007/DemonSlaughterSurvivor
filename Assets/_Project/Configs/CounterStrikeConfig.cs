using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "CounterStrikeConfig", menuName = "Game/Content/Counter Strike Config")]
    public sealed class CounterStrikeConfig : PerkBehaviourConfig
    {
        [SerializeField] private CounterStrikeLevel[] _levels = Array.Empty<CounterStrikeLevel>();
        [SerializeField] private bool _locksMovement;

        [Header("Swing")]
        [SerializeField] private SwingVariant _swing = new SwingVariant();

        [Header("Bake inputs: rig, hand bone, sample rate and body of this weapon")]
        [SerializeField] private WeaponConfig _bakeSource;

        public override int LevelCount => _levels.Length;

        public bool LocksMovement => _locksMovement;

        public SwingVariant Swing => _swing;

        public WeaponConfig BakeSource => _bakeSource;

        public CounterStrikeLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(CounterStrikeConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' counter strike '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                CounterStrikeLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.Chance) || entry.Chance <= 0f || entry.Chance > 1f)
                    throw new InvalidOperationException($"{label} level {level} has chance {entry.Chance}; it must be in (0, 1].");

                if (float.IsNaN(entry.DamageShare) || float.IsInfinity(entry.DamageShare) || entry.DamageShare <= 0f)
                    throw new InvalidOperationException($"{label} level {level} has damage share {entry.DamageShare}; it must be positive.");
            }

            if (_swing == null || _swing.Bake == null)
                throw new InvalidOperationException($"{label} has no {nameof(SwingBake)} for its swing.");

            if (_swing.PlaybackSpeed <= 0f || _swing.WindowEnd <= _swing.WindowStart)
                throw new InvalidOperationException(
                    $"{label} swing has playback speed {_swing.PlaybackSpeed} and window {_swing.WindowStart}-{_swing.WindowEnd}; bake it with Game/Bake Weapon Swings.");
        }
    }

    [Serializable]
    public sealed class CounterStrikeLevel
    {
        [SerializeField, Range(0f, 1f)] private float _chance;
        [SerializeField] private float _damageShare = 1f;

        public float Chance => _chance;

        public float DamageShare => _damageShare;
    }
}
