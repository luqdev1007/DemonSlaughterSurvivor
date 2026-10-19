using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "LightningArrowConfig", menuName = "Game/Content/Lightning Arrow Config")]
    public sealed class LightningArrowConfig : PerkBehaviourConfig
    {
        [SerializeField] private LightningArrowLevel[] _levels = Array.Empty<LightningArrowLevel>();
        [SerializeField, Min(0.5f)] private float _chainRange = 5f;
        [SerializeField, Range(0.1f, 1f)] private float _jumpFalloff = 0.85f;
        [SerializeField, Min(0f)] private float _jumpSeconds = 0.06f;

        [Header("View")]
        [SerializeField] private GameObject _arrowPrefab;
        [SerializeField] private GameObject _boltPrefab;
        [SerializeField, Min(0.05f)] private float _boltSeconds = 0.45f;
        [SerializeField, Min(0f)] private float _boltHeight = 1f;

        public override int LevelCount => _levels.Length;

        public float ChainRange => _chainRange;

        public float JumpFalloff => _jumpFalloff;

        public float JumpSeconds => _jumpSeconds;

        public GameObject ArrowPrefab => _arrowPrefab;

        public GameObject BoltPrefab => _boltPrefab;

        public float BoltSeconds => _boltSeconds;

        public float BoltHeight => _boltHeight;

        public LightningArrowLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(LightningArrowConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' lightning arrow '{name}'";

            if (_arrowPrefab == null || _boltPrefab == null)
                throw new InvalidOperationException($"{label} needs both the charged arrow and the bolt prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                LightningArrowLevel entry = Level(level);

                if (entry == null || entry.CooldownSeconds <= 0f || entry.Jumps < 1 || entry.DamageScale <= 0f)
                    throw new InvalidOperationException(
                        $"{label} level {level} must have a positive cooldown, at least one jump and a positive damage share.");
            }
        }
    }

    [Serializable]
    public sealed class LightningArrowLevel
    {
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 7f;
        [SerializeField, Min(1)] private int _jumps = 3;
        [SerializeField, Min(0f)] private float _damageScale = 0.7f;

        public float CooldownSeconds => _cooldownSeconds;

        public int Jumps => _jumps;

        public float DamageScale => _damageScale;
    }
}
