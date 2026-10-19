using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ArcaneWaveConfig", menuName = "Game/Content/Arcane Wave Config")]
    public sealed class ArcaneWaveConfig : PerkBehaviourConfig
    {
        [SerializeField] private ArcaneWaveLevel[] _levels = Array.Empty<ArcaneWaveLevel>();
        [SerializeField, Min(0.5f)] private float _speed = 12f;
        [SerializeField, Min(0f)] private float _pushSpeed = 4f;
        [SerializeField, Min(0f)] private float _pushSeconds = 0.25f;
        [SerializeField, Min(0.5f)] private float _fxDesignRadius = 5f;

        [Header("View")]
        [SerializeField] private GameObject _waveFxPrefab;

        public override int LevelCount => _levels.Length;

        public override float MaxPushSpeed => _pushSpeed;

        public float Speed => _speed;

        public float PushSpeed => _pushSpeed;

        public float PushSeconds => _pushSeconds;

        public float FxDesignRadius => _fxDesignRadius;

        public GameObject WaveFxPrefab => _waveFxPrefab;

        public ArcaneWaveLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(ArcaneWaveConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' arcane wave '{name}'";

            if (_waveFxPrefab == null)
                throw new InvalidOperationException($"{label} is missing a view prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                ArcaneWaveLevel entry = Level(level);

                if (entry == null || entry.CooldownSeconds <= 0 || entry.Damage <= 0 || entry.Radius <= 0 || entry.AffectedBonus <= 0)
                    throw new InvalidOperationException($"{label} level {level} must have positive values.");
            }
        }
    }

    [Serializable]
    public sealed class ArcaneWaveLevel
    {
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 6f;
        [SerializeField, Min(0.01f)] private float _damage = 20f;
        [SerializeField, Min(0.5f)] private float _radius = 5f;
        [SerializeField, Min(0.01f)] private float _affectedBonus = 0.5f;

        public float CooldownSeconds => _cooldownSeconds;

        public float Damage => _damage;

        public float Radius => _radius;

        public float AffectedBonus => _affectedBonus;
    }
}
