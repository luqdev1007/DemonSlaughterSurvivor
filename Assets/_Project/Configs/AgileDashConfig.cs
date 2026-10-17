using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "AgileDashConfig", menuName = "Game/Content/Agile Dash Config")]
    public sealed class AgileDashConfig : PerkBehaviourConfig
    {
        [SerializeField] private AgileDashLevel[] _levels = Array.Empty<AgileDashLevel>();

        [Header("Trigger")]
        [SerializeField] private float _triggerRadius = 3f;
        [SerializeField] private int _minEnemies = 1;

        [Header("Dodge window")]
        [SerializeField] private float _windowSeconds = 0.75f;

        [Header("Strike")]
        [SerializeField] private string _trigger = "DashFinalAttack";
        [SerializeField] private float _playbackSpeed = 1.4f;
        [SerializeField] private float _impactClipSeconds = 1.5f;
        [SerializeField] private float _clipSeconds = 2.133f;
        [SerializeField] private bool _locksMovement = true;
        [SerializeField] private float _reach = 3.2f;
        [SerializeField] private float _halfAngleDegrees = 180f;
        [SerializeField] private float _pushSpeed = 5f;
        [SerializeField] private float _pushSeconds = 0.5f;
        [SerializeField] private float _pushArcHeight = 0.5f;

        [Header("Charge")]
        [SerializeField] private float _reachAtFullCharge = 1.3f;

        public override int LevelCount => _levels.Length;

        public override float MaxPushSpeed => _pushSpeed;

        public float TriggerRadius => _triggerRadius;

        public int MinEnemies => _minEnemies;

        public float WindowSeconds => _windowSeconds;

        public string Trigger => _trigger;

        public float PlaybackSpeed => _playbackSpeed;

        public float ImpactClipSeconds => _impactClipSeconds;

        public float ClipSeconds => _clipSeconds;

        public bool LocksMovement => _locksMovement;

        public float Reach => _reach;

        public float HalfAngleDegrees => _halfAngleDegrees;

        public float PushSpeed => _pushSpeed;

        public float PushSeconds => _pushSeconds;

        public float PushArcHeight => _pushArcHeight;

        public float ReachAtFullCharge => _reachAtFullCharge;

        public AgileDashLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(AgileDashConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' agile dash '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                AgileDashLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.DamageShare) || entry.DamageShare <= 0f)
                    throw new InvalidOperationException($"{label} level {level} has damage share {entry.DamageShare}; it must be positive.");

                if (float.IsNaN(entry.CooldownSeconds) || entry.CooldownSeconds < 0f)
                    throw new InvalidOperationException($"{label} level {level} has cooldown {entry.CooldownSeconds} s; it must not be negative.");
            }

            if (_triggerRadius <= 0f || _minEnemies < 1)
                throw new InvalidOperationException(
                    $"{label} triggers on {_minEnemies} enemies within {_triggerRadius} m; the radius must be positive and the count at least 1.");

            if (_windowSeconds < 0f)
                throw new InvalidOperationException($"{label} has a dodge window of {_windowSeconds} s; it must not be negative.");

            if (_reach <= 0f || _halfAngleDegrees <= 0f || _halfAngleDegrees > 180f)
                throw new InvalidOperationException($"{label} strikes {_reach} m at ±{_halfAngleDegrees}°; the reach must be positive and the angle in (0, 180].");

            if (_playbackSpeed <= 0f || _clipSeconds <= 0f || _impactClipSeconds < 0f || _impactClipSeconds > _clipSeconds)
                throw new InvalidOperationException(
                    $"{label} has playback {_playbackSpeed}, impact {_impactClipSeconds} s of a {_clipSeconds} s clip; " +
                    "the speed and clip must be positive and the impact inside the clip.");

            if (string.IsNullOrEmpty(_trigger))
                throw new InvalidOperationException($"{label} has no animator trigger.");

            if (_pushSpeed < 0f || _pushSeconds < 0f || _pushArcHeight < 0f)
                throw new InvalidOperationException($"{label} has a negative push number; use 0 for no push.");

            if (_reachAtFullCharge <= 0f)
                throw new InvalidOperationException($"{label} has reach at full charge {_reachAtFullCharge}; it must be positive.");
        }
    }

    [Serializable]
    public sealed class AgileDashLevel
    {
        [SerializeField] private float _damageShare = 2f;
        [SerializeField] private float _cooldownSeconds = 6f;

        public float DamageShare => _damageShare;

        public float CooldownSeconds => _cooldownSeconds;
    }
}
