using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "LightningShieldConfig", menuName = "Game/Content/Lightning Shield Config")]
    public sealed class LightningShieldConfig : PerkBehaviourConfig
    {
        [SerializeField] private LightningShieldLevel[] _levels = Array.Empty<LightningShieldLevel>();
        [SerializeField, Min(0.5f)] private float _radius = 2.6f;
        [SerializeField, Min(0f)] private float _boltHeight = 1.1f;
        [SerializeField, Min(0.02f)] private float _boltSeconds = 0.15f;
        [SerializeField, Min(0.05f)] private float _impactSeconds = 0.6f;

        [Header("View")]
        [SerializeField] private GameObject _auraFxPrefab;
        [SerializeField] private GameObject _boltPrefab;
        [SerializeField] private GameObject _impactFxPrefab;

        public override int LevelCount => _levels.Length;

        public float Radius => _radius;

        public float BoltHeight => _boltHeight;

        public float BoltSeconds => _boltSeconds;

        public float ImpactSeconds => _impactSeconds;

        public GameObject AuraFxPrefab => _auraFxPrefab;

        public GameObject BoltPrefab => _boltPrefab;

        public GameObject ImpactFxPrefab => _impactFxPrefab;

        public LightningShieldLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(LightningShieldConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' lightning shield '{name}'";

            if (_auraFxPrefab == null || _boltPrefab == null || _impactFxPrefab == null)
                throw new InvalidOperationException($"{label} is missing a view prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                LightningShieldLevel entry = Level(level);

                if (entry == null || entry.IntervalSeconds <= 0 || entry.MaxTargets <= 0 || entry.Damage <= 0)
                    throw new InvalidOperationException($"{label} level {level} must have positive values.");
            }
        }
    }

    [Serializable]
    public sealed class LightningShieldLevel
    {
        [SerializeField, Min(0.05f)] private float _intervalSeconds = 0.6f;
        [SerializeField, Min(1)] private int _maxTargets = 2;
        [SerializeField, Min(0.01f)] private float _damage = 8f;

        public float IntervalSeconds => _intervalSeconds;

        public int MaxTargets => _maxTargets;

        public float Damage => _damage;
    }
}
