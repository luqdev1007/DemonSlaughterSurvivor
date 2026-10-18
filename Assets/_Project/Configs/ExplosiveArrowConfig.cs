using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ExplosiveArrowConfig", menuName = "Game/Content/Explosive Arrow Config")]
    public sealed class ExplosiveArrowConfig : PerkBehaviourConfig
    {
        [SerializeField] private ExplosiveArrowLevel[] _levels = Array.Empty<ExplosiveArrowLevel>();
        [SerializeField, Min(0.1f)] private float _radius = 2f;
        [SerializeField, Min(0f)] private float _pushSpeed = 5f;
        [SerializeField, Min(0f)] private float _pushSeconds = 0.3f;
        [SerializeField, Min(0f)] private float _pushArcHeight = 0.5f;

        [Header("View")]
        [SerializeField] private GameObject _fxPrefab;
        [SerializeField, Min(0.05f)] private float _fxSeconds = 1.2f;

        public override int LevelCount => _levels.Length;

        public override float MaxPushSpeed => _pushSpeed;

        public float Radius => _radius;

        public float PushSpeed => _pushSpeed;

        public float PushSeconds => _pushSeconds;

        public float PushArcHeight => _pushArcHeight;

        public GameObject FxPrefab => _fxPrefab;

        public float FxSeconds => _fxSeconds;

        public ExplosiveArrowLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(ExplosiveArrowConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' explosive arrow '{name}'";

            if (_fxPrefab == null)
                throw new InvalidOperationException($"{label} has no explosion prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                ExplosiveArrowLevel entry = Level(level);

                if (entry == null || entry.Chance <= 0f || entry.Chance > 1f || entry.DamageScale <= 0f)
                    throw new InvalidOperationException(
                        $"{label} level {level} must have a chance in (0, 1] and a positive damage share.");
            }
        }
    }

    [Serializable]
    public sealed class ExplosiveArrowLevel
    {
        [SerializeField, Range(0f, 1f)] private float _chance = 0.15f;
        [SerializeField, Min(0f)] private float _damageScale = 0.6f;

        public float Chance => _chance;

        public float DamageScale => _damageScale;
    }
}
