using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ExecutionWavesConfig", menuName = "Game/Content/Execution Waves Config")]
    public sealed class ExecutionWavesConfig : PerkBehaviourConfig
    {
        [SerializeField] private ExecutionWavesLevel[] _levels = Array.Empty<ExecutionWavesLevel>();

        [Header("Waves")]
        [SerializeField, Min(1)] private int _waveCount = 3;
        [SerializeField, Range(1f, 360f)] private float _coneDegrees = 120f;
        [SerializeField, Min(0f)] private float _startRadius = 0.8f;
        [SerializeField, Min(0.1f)] private float _maxRadius = 9f;
        [SerializeField, Min(0.1f)] private float _speed = 14f;
        [SerializeField, Min(0.05f)] private float _thickness = 1f;

        [Header("Push")]
        [SerializeField, Min(0f)] private float _pushSpeed = 10f;
        [SerializeField, Min(0f)] private float _pushSeconds = 0.5f;
        [SerializeField, Min(0f)] private float _pushArcHeight = 0.5f;

        [Header("View")]
        [SerializeField] private GameObject _wavePrefab;

        public override int LevelCount => _levels.Length;

        public int WaveCount => _waveCount;

        public float ConeDegrees => _coneDegrees;

        public float WaveHalfAngleDegrees => _coneDegrees / _waveCount * 0.5f;

        public float StartRadius => _startRadius;

        public float MaxRadius => _maxRadius;

        public float Speed => _speed;

        public float Thickness => _thickness;

        public float PushSpeed => _pushSpeed;

        public override float MaxPushSpeed => _pushSpeed;

        public float PushSeconds => _pushSeconds;

        public float PushArcHeight => _pushArcHeight;

        public GameObject WavePrefab => _wavePrefab;

        public ExecutionWavesLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(ExecutionWavesConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' execution waves '{name}'";

            if (_wavePrefab == null)
                throw new InvalidOperationException($"{label} is missing a wave view prefab.");

            if (_maxRadius <= _startRadius)
                throw new InvalidOperationException($"{label} starts at {_startRadius} m and ends at {_maxRadius} m; the waves would not travel.");

            for (int level = 1; level <= LevelCount; level++)
            {
                ExecutionWavesLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.DamageShare) || float.IsInfinity(entry.DamageShare) || entry.DamageShare <= 0f)
                    throw new InvalidOperationException($"{label} level {level} has damage share {entry.DamageShare}; it must be positive.");
            }

            if (Requires<ExecutionConfig>(perk) == false)
                throw new InvalidOperationException(
                    $"{label} reacts to the execution, but the perk does not require a perk with an {nameof(ExecutionConfig)}, so it could be taken without one.");
        }

        private static bool Requires<T>(PerkConfig perk) where T : PerkBehaviourConfig
        {
            for (int index = 0; index < perk.Requires.Count; index++)
            {
                if (perk.Requires[index] != null && perk.Requires[index].Behaviour is T)
                    return true;
            }

            return false;
        }
    }

    [Serializable]
    public sealed class ExecutionWavesLevel
    {
        [SerializeField, Min(0.01f)] private float _damageShare = 1.5f;

        public float DamageShare => _damageShare;
    }
}
