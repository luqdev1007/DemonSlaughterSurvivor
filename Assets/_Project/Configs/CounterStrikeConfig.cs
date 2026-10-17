using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "CounterStrikeConfig", menuName = "Game/Content/Counter Strike Config")]
    public sealed class CounterStrikeConfig : PerkBehaviourConfig
    {
        [SerializeField] private CounterStrikeLevel[] _levels = Array.Empty<CounterStrikeLevel>();
        [SerializeField] private bool _locksMovement;
        [SerializeField] private float _armedSeconds = 1.5f;

        [Header("Trigger")]
        [SerializeField] private float _triggerRadius = 3f;
        [SerializeField] private int _minEnemies = 3;

        [Header("Swing")]
        [SerializeField] private SwingVariant _swing = new SwingVariant();

        [Header("Bake inputs: rig, hand bone, sample rate and body of this weapon")]
        [SerializeField] private WeaponConfig _bakeSource;

        public override int LevelCount => _levels.Length;

        public bool LocksMovement => _locksMovement;

        public float ArmedSeconds => _armedSeconds;

        public float TriggerRadius => _triggerRadius;

        public int MinEnemies => _minEnemies;

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

                if (float.IsNaN(entry.CooldownSeconds) || float.IsInfinity(entry.CooldownSeconds) || entry.CooldownSeconds <= 0f)
                    throw new InvalidOperationException($"{label} level {level} has cooldown {entry.CooldownSeconds} s; it must be positive, otherwise the whirl fires every tick.");

                if (float.IsNaN(entry.DamageShare) || float.IsInfinity(entry.DamageShare) || entry.DamageShare <= 0f)
                    throw new InvalidOperationException($"{label} level {level} has damage share {entry.DamageShare}; it must be positive.");
            }

            if (float.IsNaN(_triggerRadius) || float.IsInfinity(_triggerRadius) || _triggerRadius <= 0f)
                throw new InvalidOperationException($"{label} has trigger radius {_triggerRadius}; it must be positive.");

            if (_minEnemies < 1)
                throw new InvalidOperationException($"{label} has {nameof(_minEnemies)} {_minEnemies}; at least one enemy must be near.");

            if (float.IsNaN(_armedSeconds) || float.IsInfinity(_armedSeconds) || _armedSeconds <= 0f)
                throw new InvalidOperationException(
                    $"{label} has armed window {_armedSeconds} s; it must be positive, otherwise a rolled counter strike expires before it can replace a swing.");

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
        [SerializeField] private float _cooldownSeconds = 8f;
        [SerializeField] private float _damageShare = 1f;

        public float CooldownSeconds => _cooldownSeconds;

        public float DamageShare => _damageShare;
    }
}
