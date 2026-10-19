using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "IceRingConfig", menuName = "Game/Content/Ice Ring Config")]
    public sealed class IceRingConfig : PerkBehaviourConfig
    {
        [SerializeField] private IceRingLevel[] _levels = Array.Empty<IceRingLevel>();
        [SerializeField, Min(0.5f)] private float _radius = 4f;

        [Header("View")]
        [SerializeField] private GameObject _ringFxPrefab;
        [SerializeField, Min(0.05f)] private float _ringFxSeconds = 1.5f;
        [SerializeField] private GameObject _frozenFxPrefab;

        public override int LevelCount => _levels.Length;

        public float Radius => _radius;

        public GameObject RingFxPrefab => _ringFxPrefab;

        public float RingFxSeconds => _ringFxSeconds;

        public GameObject FrozenFxPrefab => _frozenFxPrefab;

        public IceRingLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(IceRingConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' ice ring '{name}'";

            if (_ringFxPrefab == null || _frozenFxPrefab == null)
                throw new InvalidOperationException($"{label} is missing its ring or frozen prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                IceRingLevel entry = Level(level);

                if (entry == null || entry.CooldownSeconds <= 0f || entry.FreezeSeconds <= 0f || entry.Damage < 0f)
                    throw new InvalidOperationException(
                        $"{label} level {level} must have a positive cooldown and freeze and a non-negative damage.");
            }
        }
    }

    [Serializable]
    public sealed class IceRingLevel
    {
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 8f;
        [SerializeField, Min(0.1f)] private float _freezeSeconds = 2f;
        [SerializeField, Min(0f)] private float _damage = 15f;

        public float CooldownSeconds => _cooldownSeconds;

        public float FreezeSeconds => _freezeSeconds;

        public float Damage => _damage;
    }
}
