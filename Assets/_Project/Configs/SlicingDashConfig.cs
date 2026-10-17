using System;
using UnityEngine;

namespace Game.Configs
{
    public enum SwordSide
    {
        Right = 0,
        Left = 1,
    }

    [CreateAssetMenu(fileName = "SlicingDashConfig", menuName = "Game/Content/Slicing Dash Config")]
    public sealed class SlicingDashConfig : PerkBehaviourConfig
    {
        [SerializeField] private SlicingDashLevel[] _levels = Array.Empty<SlicingDashLevel>();

        [Header("Corridor")]
        [SerializeField] private SwordSide _swordSide = SwordSide.Right;
        [SerializeField] private float _corridorWidth = 1.6f;
        [SerializeField] private float _corridorPushSpeed = 5f;
        [SerializeField] private float _corridorPushSeconds = 0.35f;
        [SerializeField] private float _corridorPushArcHeight = 0.2f;

        [Header("Final slash")]
        [SerializeField] private string _finalTrigger = "DashUpperSlash";
        [SerializeField] private float _finalPlaybackSpeed = 1.2f;
        [SerializeField] private float _finalImpactClipSeconds = 0.5f;
        [SerializeField] private float _finalReach = 2.6f;
        [SerializeField] private float _finalHalfAngleDegrees = 70f;
        [SerializeField] private float _finalPushSpeed = 5f;
        [SerializeField] private float _finalPushSeconds = 0.4f;
        [SerializeField] private float _finalPushArcHeight = 0.35f;

        [Header("Charge")]
        [SerializeField] private float _reachAtFullCharge = 1.3f;

        public override int LevelCount => _levels.Length;

        public override float MaxPushSpeed => Mathf.Max(_corridorPushSpeed, _finalPushSpeed);

        public SwordSide SwordSide => _swordSide;

        public float CorridorWidth => _corridorWidth;

        public float CorridorPushSpeed => _corridorPushSpeed;

        public float CorridorPushSeconds => _corridorPushSeconds;

        public float CorridorPushArcHeight => _corridorPushArcHeight;

        public string FinalTrigger => _finalTrigger;

        public float FinalPlaybackSpeed => _finalPlaybackSpeed;

        public float FinalImpactClipSeconds => _finalImpactClipSeconds;

        public float FinalReach => _finalReach;

        public float FinalHalfAngleDegrees => _finalHalfAngleDegrees;

        public float FinalPushSpeed => _finalPushSpeed;

        public float FinalPushSeconds => _finalPushSeconds;

        public float FinalPushArcHeight => _finalPushArcHeight;

        public float ReachAtFullCharge => _reachAtFullCharge;

        public SlicingDashLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(SlicingDashConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' slicing dash '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                SlicingDashLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.Chance) || entry.Chance <= 0f || entry.Chance > 1f)
                    throw new InvalidOperationException($"{label} level {level} has chance {entry.Chance}; it must be in (0, 1].");

                if (float.IsNaN(entry.CorridorDamageShare) || entry.CorridorDamageShare < 0f
                    || float.IsNaN(entry.FinalDamageShare) || entry.FinalDamageShare < 0f)
                    throw new InvalidOperationException(
                        $"{label} level {level} has damage shares {entry.CorridorDamageShare} / {entry.FinalDamageShare}; they must not be negative.");
            }

            if (_corridorWidth <= 0f || _finalReach <= 0f)
                throw new InvalidOperationException($"{label} has corridor width {_corridorWidth} m and final reach {_finalReach} m; both must be positive.");

            if (_finalHalfAngleDegrees <= 0f || _finalHalfAngleDegrees > 180f)
                throw new InvalidOperationException($"{label} has a final half angle of {_finalHalfAngleDegrees}°; it must be in (0, 180].");

            if (_finalPlaybackSpeed <= 0f || _finalImpactClipSeconds < 0f)
                throw new InvalidOperationException(
                    $"{label} has playback {_finalPlaybackSpeed} and impact {_finalImpactClipSeconds} s; the speed must be positive and the impact not negative.");

            if (string.IsNullOrEmpty(_finalTrigger))
                throw new InvalidOperationException($"{label} has no final slash animator trigger.");

            if (_corridorPushSpeed < 0f || _corridorPushSeconds < 0f || _finalPushSpeed < 0f || _finalPushSeconds < 0f
                || _corridorPushArcHeight < 0f || _finalPushArcHeight < 0f)
                throw new InvalidOperationException($"{label} has a negative push number; use 0 for no push.");

            if (_reachAtFullCharge <= 0f)
                throw new InvalidOperationException($"{label} has reach at full charge {_reachAtFullCharge}; it must be positive.");
        }
    }

    [Serializable]
    public sealed class SlicingDashLevel
    {
        [SerializeField] private float _chance = 0.3f;
        [SerializeField] private float _corridorDamageShare = 0.5f;
        [SerializeField] private float _finalDamageShare = 1.5f;

        public float Chance => _chance;

        public float CorridorDamageShare => _corridorDamageShare;

        public float FinalDamageShare => _finalDamageShare;
    }
}
