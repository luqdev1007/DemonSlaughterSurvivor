using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class SwingCoverage
    {
        public const int AngleBins = 72;
        public const float AngleStep = 5f;
        public const int DistanceBins = 15;
        public const float MinDistance = 0.6f;
        public const float DistanceStep = 0.2f;

        [SerializeField] private bool[] _cells = Array.Empty<bool>();

        public bool IsBaked => _cells.Length == AngleBins * DistanceBins;

        public float MaxDistance => MinDistance + DistanceStep * (DistanceBins - 1);

        public int CoveredCount
        {
            get
            {
                int count = 0;

                for (int index = 0; index < _cells.Length; index++)
                {
                    if (_cells[index])
                        count++;
                }

                return count;
            }
        }

        public static float CellAngle(int angleBin)
        {
            return -180f + angleBin * AngleStep;
        }

        public static float CellDistance(int distanceBin)
        {
            return MinDistance + distanceBin * DistanceStep;
        }

        public bool Covers(float localAngleDegrees, float distance)
        {
            if (IsBaked == false)
                return false;

            float distanceSlot = (distance - MinDistance) / DistanceStep;

            if (distanceSlot < -0.5f || distanceSlot >= DistanceBins - 0.5f)
                return false;

            int distanceBin = Mathf.Clamp(Mathf.RoundToInt(distanceSlot), 0, DistanceBins - 1);

            int angleBin = Mathf.RoundToInt((localAngleDegrees + 180f) / AngleStep) % AngleBins;

            if (angleBin < 0)
                angleBin += AngleBins;

            return _cells[angleBin * DistanceBins + distanceBin];
        }

        public bool Cell(int angleBin, int distanceBin)
        {
            return IsBaked && _cells[angleBin * DistanceBins + distanceBin];
        }

#if UNITY_EDITOR
        public void Overwrite(bool[] cells)
        {
            if (cells == null || cells.Length != AngleBins * DistanceBins)
                throw new ArgumentException($"Coverage needs exactly {AngleBins * DistanceBins} cells.", nameof(cells));

            _cells = cells;
        }
#endif
    }

    [Serializable]
    public sealed class SwingVariant
    {
        [SerializeField] private string _animatorTrigger;
        [SerializeField] private float _playbackSpeed = 1f;
        [SerializeField] private float _windowStart;
        [SerializeField] private float _windowEnd;
        [SerializeField] private float _halfAngleDegrees = 90f;
        [SerializeField] private SwingBake _bake;
        [SerializeField] private float _maxReach;
        [SerializeField] private SwingCoverage _coverage = new SwingCoverage();

        public string AnimatorTrigger => _animatorTrigger;

        public float PlaybackSpeed => _playbackSpeed;

        public float WindowStart => _windowStart;

        public float WindowEnd => _windowEnd;

        public float HalfAngleDegrees => _halfAngleDegrees;

        public SwingBake Bake => _bake;

        public float MaxReach => _maxReach;

        public SwingCoverage Coverage => _coverage;

#if UNITY_EDITOR
        public void OverwriteBakeResults(float windowStart, float windowEnd, float maxReach, SwingCoverage coverage)
        {
            _windowStart = windowStart;
            _windowEnd = windowEnd;
            _maxReach = maxReach;
            _coverage = coverage;
        }
#endif
    }

    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "Game/Content/Weapon Config")]
    public sealed class WeaponConfig : ContentConfig
    {
        [Header("Stats")]
        [SerializeField] private float _damage = 20f;
        [SerializeField] private float _cooldownSeconds = 1f;

        [Header("Swings")]
        [SerializeField] private SwingVariant[] _variants = Array.Empty<SwingVariant>();

        [Header("Trigger, written by the bake tool")]
        [SerializeField] private SwingCoverage _triggerCoverage = new SwingCoverage();

        [Header("Bake inputs")]
        [SerializeField] private GameObject _bakeRig;
        [SerializeField] private string _handBoneName;
        [SerializeField] private float _bakeSampleRate = 240f;
        [SerializeField] private float _windowReachThreshold = 1.4f;
        [SerializeField] private float _coverageBodyHeight = 1.77f;
        [SerializeField] private float _coverageBodyRadius = 0.4f;

        public float Damage => _damage;

        public float CooldownSeconds => _cooldownSeconds;

        public int VariantCount => _variants.Length;

        public SwingVariant Variant(int index)
        {
            return _variants[index];
        }

        public SwingCoverage TriggerCoverage => _triggerCoverage;

        public GameObject BakeRig => _bakeRig;

        public string HandBoneName => _handBoneName;

        public float BakeSampleRate => _bakeSampleRate;

        public float WindowReachThreshold => _windowReachThreshold;

        public float CoverageBodyHeight => _coverageBodyHeight;

        public float CoverageBodyRadius => _coverageBodyRadius;

        public float MaxReach
        {
            get
            {
                float max = 0f;

                for (int index = 0; index < _variants.Length; index++)
                {
                    if (_variants[index].MaxReach > max)
                        max = _variants[index].MaxReach;
                }

                return max;
            }
        }

#if UNITY_EDITOR
        public void OverwriteTriggerCoverage(SwingCoverage coverage)
        {
            _triggerCoverage = coverage;
        }
#endif
    }
}
