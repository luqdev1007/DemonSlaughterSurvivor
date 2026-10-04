using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "CounterStrikeConfig", menuName = "Game/Content/Counter Strike Config")]
    public sealed class CounterStrikeConfig : ScriptableObject
    {
        [SerializeField] private CounterStrikeLevel[] _levels = Array.Empty<CounterStrikeLevel>();
        [SerializeField] private bool _locksMovement;

        [Header("Swing")]
        [SerializeField] private SwingVariant _swing = new SwingVariant();

        [Header("Bake inputs: rig, hand bone, sample rate and body of this weapon")]
        [SerializeField] private WeaponConfig _bakeSource;

        public int LevelCount => _levels.Length;

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
