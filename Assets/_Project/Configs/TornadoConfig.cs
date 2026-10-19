using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "TornadoConfig", menuName = "Game/Content/Tornado Config")]
    public sealed class TornadoConfig : PerkBehaviourConfig
    {
        [SerializeField] private TornadoLevel[] _levels = Array.Empty<TornadoLevel>();
        [SerializeField, Min(0.1f)] private float _lifeSeconds = 4f;
        [SerializeField, Min(0.1f)] private float _speed = 3.5f;
        [SerializeField, Min(0.1f)] private float _radius = 1.5f;
        [SerializeField, Min(0.05f)] private float _pulseSeconds = 0.5f;
        [SerializeField, Min(0.05f)] private float _turnSeconds = 0.5f;
        [SerializeField, Range(0f, 180f)] private float _minTurnDegrees = 35f;
        [SerializeField, Range(0f, 180f)] private float _maxTurnDegrees = 80f;
        [SerializeField, Range(0.01f, 0.95f)] private float _slow = 0.4f;
        [SerializeField, Min(0.05f)] private float _slowSeconds = 1f;

        [Header("View")]
        [SerializeField] private GameObject _tornadoPrefab;

        public override int LevelCount => _levels.Length;

        public float LifeSeconds => _lifeSeconds;

        public float Speed => _speed;

        public float Radius => _radius;

        public float PulseSeconds => _pulseSeconds;

        public float TurnSeconds => _turnSeconds;

        public float MinTurnDegrees => _minTurnDegrees;

        public float MaxTurnDegrees => _maxTurnDegrees;

        public float Slow => _slow;

        public float SlowSeconds => _slowSeconds;

        public GameObject TornadoPrefab => _tornadoPrefab;

        public TornadoLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(TornadoConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' tornado '{name}'";

            if (_tornadoPrefab == null)
                throw new InvalidOperationException($"{label} is missing a view prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                TornadoLevel entry = Level(level);

                if (entry == null || entry.CooldownSeconds <= 0 || entry.Count <= 0 || entry.Damage <= 0)
                    throw new InvalidOperationException($"{label} level {level} must have positive values.");
            }
        }
    }

    [Serializable]
    public sealed class TornadoLevel
    {
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 7f;
        [SerializeField, Min(1)] private int _count = 1;
        [SerializeField, Min(0.01f)] private float _damage = 6f;

        public float CooldownSeconds => _cooldownSeconds;

        public int Count => _count;

        public float Damage => _damage;
    }
}
