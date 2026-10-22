using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ChakramConfig", menuName = "Game/Content/Chakram Config")]
    public sealed class ChakramConfig : PerkBehaviourConfig
    {
        [SerializeField] private ChakramLevel[] _levels = Array.Empty<ChakramLevel>();

        [Header("Flight")]
        [SerializeField, Min(0.1f)] private float _lifeSeconds = 2.4f;
        [SerializeField, Min(0f)] private float _startRadius = 0.8f;
        [SerializeField, Min(0.1f)] private float _endRadius = 9f;
        [SerializeField, Min(0.1f)] private float _turns = 1.5f;
        [SerializeField, Min(0.05f)] private float _hitRadius = 0.6f;
        [SerializeField, Min(0f)] private float _triggerRange = 11f;

        [Header("Slow")]
        [SerializeField, Range(0.01f, 0.95f)] private float _slow = 0.35f;
        [SerializeField, Min(0.05f)] private float _slowSeconds = 2f;

        [Header("Bleeding")]
        [SerializeField, Min(0.05f)] private float _bleedTickSeconds = 0.5f;

        [Header("View")]
        [SerializeField] private GameObject _chakramPrefab;
        [SerializeField] private GameObject _bleedFxPrefab;

        public override int LevelCount => _levels.Length;

        public float LifeSeconds => _lifeSeconds;

        public float StartRadius => _startRadius;

        public float EndRadius => _endRadius;

        public float Turns => _turns;

        public float HitRadius => _hitRadius;

        public float TriggerRange => _triggerRange;

        public float Slow => _slow;

        public float SlowSeconds => _slowSeconds;

        public float BleedTickSeconds => _bleedTickSeconds;

        public GameObject ChakramPrefab => _chakramPrefab;

        public GameObject BleedFxPrefab => _bleedFxPrefab;

        public ChakramLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(ChakramConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' chakram '{name}'";

            if (_chakramPrefab == null)
                throw new InvalidOperationException($"{label} is missing a chakram view prefab.");

            if (_endRadius <= _startRadius)
                throw new InvalidOperationException($"{label} spirals from {_startRadius} m to {_endRadius} m; the end must be farther than the start.");

            for (int level = 1; level <= LevelCount; level++)
            {
                ChakramLevel entry = Level(level);

                if (entry == null || entry.CooldownSeconds <= 0f || entry.Count <= 0 || entry.DamageScale <= 0f
                    || entry.BleedDamageScale < 0f || entry.BleedSeconds < 0f)
                    throw new InvalidOperationException($"{label} level {level} must have a positive cooldown, count and damage, and no negative bleeding.");
            }
        }
    }

    [Serializable]
    public sealed class ChakramLevel
    {
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 6f;
        [SerializeField, Min(1)] private int _count = 1;
        [SerializeField, Min(0.01f)] private float _damageScale = 1f;
        [SerializeField, Min(0f)] private float _bleedDamageScale = 0.3f;
        [SerializeField, Min(0f)] private float _bleedSeconds = 3f;

        public float CooldownSeconds => _cooldownSeconds;

        public int Count => _count;

        public float DamageScale => _damageScale;

        public float BleedDamageScale => _bleedDamageScale;

        public float BleedSeconds => _bleedSeconds;
    }
}
