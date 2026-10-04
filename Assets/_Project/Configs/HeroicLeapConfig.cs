using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "HeroicLeapConfig", menuName = "Game/Content/Heroic Leap Config")]
    public sealed class HeroicLeapConfig : PerkBehaviourConfig
    {
        [SerializeField] private HeroicLeapLevel[] _levels = Array.Empty<HeroicLeapLevel>();
        [SerializeField] private bool _locksMovement = true;
        [SerializeField] private int _totalTicks = 16;

        [Header("Trigger")]
        [SerializeField] private float _surroundRadius = 1.5f;
        [SerializeField] private int _surroundCount = 4;

        [Header("Landing strike")]
        [SerializeField] private float _sectorHalfAngleDegrees = 60f;
        [SerializeField] private float _sectorReach = 2.5f;

        [Header("Push")]
        [SerializeField] private float _pushRadius = 2.5f;
        [SerializeField] private float _pushSpeed = 5f;
        [SerializeField] private float _pushSeconds = 0.6f;

        [Header("Bake inputs")]
        [SerializeField] private string _animatorTrigger = "AttackJump";
        [SerializeField] private WeaponConfig _bakeSource;
        [SerializeField] private string[] _toeBoneNames = { "mixamorig:LeftToe_End", "mixamorig:RightToe_End" };
        [SerializeField] private float _contactHeight = 0.05f;

        [Header("Written by the bake tool")]
        [SerializeField] private AnimationClip _clip;
        [SerializeField] private uint _clipHash;
        [SerializeField] private float _stateSpeed;
        [SerializeField] private float _landingClipTime;
        [SerializeField] private int _landingTick;

        public override int LevelCount => _levels.Length;

        public bool LocksMovement => _locksMovement;

        public int TotalTicks => _totalTicks;

        public float SurroundRadius => _surroundRadius;

        public int SurroundCount => _surroundCount;

        public float SectorHalfAngleDegrees => _sectorHalfAngleDegrees;

        public float SectorReach => _sectorReach;

        public float PushRadius => _pushRadius;

        public float PushSpeed => _pushSpeed;

        public float PushSeconds => _pushSeconds;

        public string AnimatorTrigger => _animatorTrigger;

        public WeaponConfig BakeSource => _bakeSource;

        public string[] ToeBoneNames => _toeBoneNames;

        public float ContactHeight => _contactHeight;

        public AnimationClip Clip => _clip;

        public uint ClipHash => _clipHash;

        public float StateSpeed => _stateSpeed;

        public float LandingClipTime => _landingClipTime;

        public int LandingTick => _landingTick;

        public HeroicLeapLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(HeroicLeapConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' heroic leap '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                HeroicLeapLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.CooldownSeconds) || entry.CooldownSeconds <= 0f)
                    throw new InvalidOperationException($"{label} level {level} has cooldown {entry.CooldownSeconds} s; it must be positive.");

                if (float.IsNaN(entry.DamageShare) || float.IsInfinity(entry.DamageShare) || entry.DamageShare <= 0f)
                    throw new InvalidOperationException($"{label} level {level} has damage share {entry.DamageShare}; it must be positive.");
            }

            if (_surroundRadius <= 0f || _surroundCount < 1)
                throw new InvalidOperationException($"{label} triggers at {_surroundCount} enemies within {_surroundRadius} m; both must be positive.");

            if (_sectorHalfAngleDegrees <= 0f || _sectorHalfAngleDegrees > 180f || _sectorReach <= 0f)
                throw new InvalidOperationException($"{label} strikes a sector of half angle {_sectorHalfAngleDegrees} deg to {_sectorReach} m; the angle must be in (0, 180] and the reach positive.");

            if (_pushRadius < 0f || _pushSpeed < 0f || _pushSeconds < 0f)
                throw new InvalidOperationException($"{label} pushes within {_pushRadius} m at {_pushSpeed} m/s for {_pushSeconds} s; none may be negative.");

            if (_clip == null || _stateSpeed <= 0f || _landingTick < 1)
                throw new InvalidOperationException($"{label} has no baked landing; run Game/Bake Weapon Swings.");

            if (_totalTicks < _landingTick)
                throw new InvalidOperationException(
                    $"{label} ends after {_totalTicks} ticks, before its landing on tick {_landingTick}; the strike would never happen.");
        }

#if UNITY_EDITOR
        public void OverwriteBakeResults(AnimationClip clip, uint clipHash, float stateSpeed, float landingClipTime, int landingTick)
        {
            _clip = clip;
            _clipHash = clipHash;
            _stateSpeed = stateSpeed;
            _landingClipTime = landingClipTime;
            _landingTick = landingTick;
        }
#endif
    }

    [Serializable]
    public sealed class HeroicLeapLevel
    {
        [SerializeField] private float _cooldownSeconds = 8f;
        [SerializeField] private float _damageShare = 1f;

        public float CooldownSeconds => _cooldownSeconds;

        public float DamageShare => _damageShare;
    }
}
