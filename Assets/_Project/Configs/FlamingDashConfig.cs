using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "FlamingDashConfig", menuName = "Game/Content/Flaming Dash Config")]
    public sealed class FlamingDashConfig : PerkBehaviourConfig
    {
        [SerializeField] private FlamingDashLevel[] _levels = Array.Empty<FlamingDashLevel>();

        [Header("Trail")]
        [SerializeField] private float _segmentSpacing = 0.6f;
        [SerializeField] private float _segmentRadius = 0.6f;
        [SerializeField] private float _pulseSeconds = 0.5f;
        [SerializeField] private int _maxLiveSegments = 200;
        [SerializeField] private GameObject _segmentPrefab;

        public override int LevelCount => _levels.Length;

        public float SegmentSpacing => _segmentSpacing;

        public float SegmentRadius => _segmentRadius;

        public float PulseSeconds => _pulseSeconds;

        public int MaxLiveSegments => _maxLiveSegments;

        public GameObject SegmentPrefab => _segmentPrefab;

        public FlamingDashLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(FlamingDashConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' flaming dash '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                FlamingDashLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.DamageShare) || float.IsInfinity(entry.DamageShare) || entry.DamageShare <= 0f)
                    throw new InvalidOperationException($"{label} level {level} has damage share {entry.DamageShare}; it must be positive.");

                if (float.IsNaN(entry.LifetimeSeconds) || entry.LifetimeSeconds <= 0f)
                    throw new InvalidOperationException($"{label} level {level} lives {entry.LifetimeSeconds} s; it must be positive.");
            }

            if (_segmentSpacing <= 0f || _segmentRadius <= 0f || _pulseSeconds <= 0f)
                throw new InvalidOperationException(
                    $"{label} has spacing {_segmentSpacing} m, radius {_segmentRadius} m and pulse {_pulseSeconds} s; all must be positive.");

            if (_maxLiveSegments < 1)
                throw new InvalidOperationException($"{label} allows {_maxLiveSegments} live segments; the cap must be at least 1.");

            if (_segmentPrefab == null)
                throw new InvalidOperationException($"{label} has no segment prefab.");
        }
    }

    [Serializable]
    public sealed class FlamingDashLevel
    {
        [SerializeField] private float _damageShare = 0.25f;
        [SerializeField] private float _lifetimeSeconds = 3f;

        public float DamageShare => _damageShare;

        public float LifetimeSeconds => _lifetimeSeconds;
    }
}
