using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "MeteorConfig", menuName = "Game/Content/Meteor Config")]
    public sealed class MeteorConfig : PerkBehaviourConfig
    {
        [SerializeField] private MeteorLevel[] _levels = Array.Empty<MeteorLevel>();

        [Header("Target")]
        [SerializeField, Min(1f)] private float _range = 14f;
        [SerializeField, Min(0.5f)] private float _clusterRadius = 3f;

        [Header("Cast")]
        [SerializeField, Min(0.05f)] private float _castSeconds = 0.6f;
        [SerializeField] private string _castTrigger = "CastNova";
        [SerializeField, Min(0.01f)] private float _castClipImpactSeconds = 1.78f;

        [Header("Fall")]
        [SerializeField, Min(0.05f)] private float _fallSeconds = 0.7f;
        [SerializeField, Min(1f)] private float _fallHeight = 14f;
        [SerializeField, Min(0f)] private float _fallDrift = 4f;

        [Header("Impact")]
        [SerializeField, Min(0.5f)] private float _radius = 3.5f;
        [SerializeField, Min(0f)] private float _pushSpeed = 6f;
        [SerializeField, Min(0f)] private float _pushSeconds = 0.35f;
        [SerializeField, Min(0f)] private float _pushArcHeight = 0.6f;

        [Header("View")]
        [SerializeField] private GameObject _meteorPrefab;
        [SerializeField] private GameObject _impactPrefab;
        [SerializeField, Min(0.05f)] private float _impactSeconds = 2f;

        public override int LevelCount => _levels.Length;

        public override float MaxPushSpeed => _pushSpeed;

        public float Range => _range;

        public float ClusterRadius => _clusterRadius;

        public float CastSeconds => _castSeconds;

        public string CastTrigger => _castTrigger;

        public float CastClipImpactSeconds => _castClipImpactSeconds;

        public float FallSeconds => _fallSeconds;

        public float FallHeight => _fallHeight;

        public float FallDrift => _fallDrift;

        public float Radius => _radius;

        public float PushSpeed => _pushSpeed;

        public float PushSeconds => _pushSeconds;

        public float PushArcHeight => _pushArcHeight;

        public GameObject MeteorPrefab => _meteorPrefab;

        public GameObject ImpactPrefab => _impactPrefab;

        public float ImpactSeconds => _impactSeconds;

        public MeteorLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(MeteorConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' meteor '{name}'";

            if (_meteorPrefab == null || _impactPrefab == null)
                throw new InvalidOperationException($"{label} is missing its meteor or impact prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                MeteorLevel entry = Level(level);

                if (entry == null || entry.CooldownSeconds <= 0f || entry.Damage <= 0f)
                    throw new InvalidOperationException($"{label} level {level} must have a positive cooldown and damage.");
            }
        }
    }

    [Serializable]
    public sealed class MeteorLevel
    {
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 10f;
        [SerializeField, Min(0f)] private float _damage = 50f;

        public float CooldownSeconds => _cooldownSeconds;

        public float Damage => _damage;
    }
}
