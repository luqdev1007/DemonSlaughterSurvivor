using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "DeathRayConfig", menuName = "Game/Content/Death Ray Config")]
    public sealed class DeathRayConfig : PerkBehaviourConfig
    {
        [SerializeField] private DeathRayLevel[] _levels = Array.Empty<DeathRayLevel>();

        [Header("Cast")]
        [SerializeField, Min(0.05f)] private float _chargeSeconds = 0.6f;
        [FormerlySerializedAs("_beamSeconds"), SerializeField, Min(0.1f)] private float _maxBeamSeconds = 3f;
        [SerializeField, Min(0.02f)] private float _tickSeconds = 0.1f;
        [SerializeField] private string _castTrigger = "CastRay";
        [SerializeField] private string _endTrigger = "CastCancel";
        [SerializeField, Min(0.01f)] private float _chargeClipSeconds = 1.6f;

        [Header("Beam")]
        [SerializeField, Min(1f)] private float _length = 16f;
        [SerializeField, Min(0.05f)] private float _halfWidth = 0.45f;
        [SerializeField, Min(0f)] private float _turnSpeed = 150f;
        [SerializeField, Min(0f)] private float _muzzleForward = 1f;
        [SerializeField, Min(0f)] private float _muzzleHeight = 1.2f;

        [Header("View")]
        [SerializeField] private GameObject _beamPrefab;

        public override int LevelCount => _levels.Length;

        public float ChargeSeconds => _chargeSeconds;

        public float MaxBeamSeconds => _maxBeamSeconds;

        public float TickSeconds => _tickSeconds;

        public string CastTrigger => _castTrigger;

        public string EndTrigger => _endTrigger;

        public float ChargeClipSeconds => _chargeClipSeconds;

        public float Length => _length;

        public float HalfWidth => _halfWidth;

        public float TurnSpeed => _turnSpeed;

        public float MuzzleForward => _muzzleForward;

        public float MuzzleHeight => _muzzleHeight;

        public GameObject BeamPrefab => _beamPrefab;

        public DeathRayLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(DeathRayConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' death ray '{name}'";

            if (_beamPrefab == null)
                throw new InvalidOperationException($"{label} has no beam prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                DeathRayLevel entry = Level(level);

                if (entry == null || entry.CooldownSeconds <= 0f || entry.InitialDamage < 0f || entry.TickDamage < 0f)
                    throw new InvalidOperationException($"{label} level {level} must have a positive cooldown and non-negative damage.");
            }
        }
    }

    [Serializable]
    public sealed class DeathRayLevel
    {
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 12f;
        [SerializeField, Min(0f)] private float _initialDamage = 25f;
        [SerializeField, Min(0f)] private float _tickDamage = 6f;

        public float CooldownSeconds => _cooldownSeconds;

        public float InitialDamage => _initialDamage;

        public float TickDamage => _tickDamage;
    }
}
