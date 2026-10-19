using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "FireBurstConfig", menuName = "Game/Content/Fire Burst Config")]
    public sealed class FireBurstConfig : PerkBehaviourConfig
    {
        [SerializeField] private FireBurstLevel[] _levels = Array.Empty<FireBurstLevel>();
        [SerializeField, Min(0f)] private float _damageScale = 0.5f;
        [SerializeField, Min(0.1f)] private float _speed = 12f;
        [SerializeField, Min(0.1f)] private float _range = 7f;
        [SerializeField, Range(0.1f, 1f)] private float _blastScale = 0.6f;
        [SerializeField, Min(0f)] private float _spawnOffset = 0.5f;
        [SerializeField] private GameObject _fireballPrefab;

        public override int LevelCount => _levels.Length;

        public float DamageScale => _damageScale;

        public float Speed => _speed;

        public float Range => _range;

        public float BlastScale => _blastScale;

        public float SpawnOffset => _spawnOffset;

        public GameObject FireballPrefab => _fireballPrefab;

        public FireBurstLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(FireBurstConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' fire burst '{name}'";

            if (_fireballPrefab == null)
                throw new InvalidOperationException($"{label} has no fireball prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                FireBurstLevel entry = Level(level);

                if (entry == null || entry.Chance <= 0f || entry.Count < 1)
                    throw new InvalidOperationException($"{label} level {level} must have a positive chance and at least one fireball.");
            }
        }
    }

    [Serializable]
    public sealed class FireBurstLevel
    {
        [SerializeField, Range(0f, 1f)] private float _chance = 0.25f;
        [SerializeField, Min(1)] private int _count = 3;

        public float Chance => _chance;

        public int Count => _count;
    }
}
