using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "BlinkRepulseConfig", menuName = "Game/Content/Blink Repulse Config")]
    public sealed class BlinkRepulseConfig : PerkBehaviourConfig
    {
        [SerializeField] private BlinkRepulseLevel[] _levels = Array.Empty<BlinkRepulseLevel>();
        [SerializeField, Min(0.05f)] private float _pushSeconds = 0.3f;
        [SerializeField, Min(0f)] private float _pushArcHeight = 0.5f;

        [Header("View")]
        [SerializeField] private GameObject _fxPrefab;
        [SerializeField, Min(0.05f)] private float _fxSeconds = 1f;

        public override int LevelCount => _levels.Length;

        public override float MaxPushSpeed
        {
            get
            {
                float max = 0f;

                for (int index = 0; index < _levels.Length; index++)
                {
                    if (_levels[index] != null)
                        max = Mathf.Max(max, _levels[index].PushSpeed);
                }

                return max;
            }
        }

        public float PushSeconds => _pushSeconds;

        public float PushArcHeight => _pushArcHeight;

        public GameObject FxPrefab => _fxPrefab;

        public float FxSeconds => _fxSeconds;

        public BlinkRepulseLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(BlinkRepulseConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' blink repulse '{name}'";

            if (_fxPrefab == null)
                throw new InvalidOperationException($"{label} has no burst prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                BlinkRepulseLevel entry = Level(level);

                if (entry == null || entry.Radius <= 0f || entry.PushSpeed <= 0f)
                    throw new InvalidOperationException($"{label} level {level} must have a positive radius and push speed.");
            }
        }
    }

    [Serializable]
    public sealed class BlinkRepulseLevel
    {
        [SerializeField, Min(0.5f)] private float _radius = 3f;
        [SerializeField, Min(0.1f)] private float _pushSpeed = 8f;

        public float Radius => _radius;

        public float PushSpeed => _pushSpeed;
    }
}
