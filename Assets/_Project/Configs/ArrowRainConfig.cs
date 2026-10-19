using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ArrowRainConfig", menuName = "Game/Content/Arrow Rain Config")]
    public sealed class ArrowRainConfig : PerkBehaviourConfig
    {
        [SerializeField] private ArrowRainLevel[] _levels = Array.Empty<ArrowRainLevel>();
        [SerializeField, Min(1f)] private float _range = 14f;
        [SerializeField, Min(0.1f)] private float _radius = 1.4f;
        [SerializeField, Min(0f)] private float _staggerSeconds = 0.12f;
        [SerializeField, Min(0f)] private float _repeatDelaySeconds = 0.5f;
        [SerializeField, Min(0.02f)] private float _fallSeconds = 0.4f;

        [Header("View")]
        [SerializeField] private GameObject _fxPrefab;
        [SerializeField, Min(0f)] private float _lingerSeconds = 0.6f;

        public override int LevelCount => _levels.Length;

        public float Range => _range;

        public float Radius => _radius;

        public float StaggerSeconds => _staggerSeconds;

        public float RepeatDelaySeconds => _repeatDelaySeconds;

        public float FallSeconds => _fallSeconds;

        public GameObject FxPrefab => _fxPrefab;

        public float LingerSeconds => _lingerSeconds;

        public ArrowRainLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(ArrowRainConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' arrow rain '{name}'";

            if (_fxPrefab == null)
                throw new InvalidOperationException($"{label} has no falling arrows prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                ArrowRainLevel entry = Level(level);

                if (entry == null || entry.CooldownSeconds <= 0f || entry.Strikes < 1 || entry.DamageScale <= 0f)
                    throw new InvalidOperationException(
                        $"{label} level {level} must have a positive cooldown, at least one strike and a positive damage share.");
            }
        }
    }

    [Serializable]
    public sealed class ArrowRainLevel
    {
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 8f;
        [SerializeField, Min(1)] private int _strikes = 5;
        [SerializeField, Min(0f)] private float _damageScale = 0.8f;

        public float CooldownSeconds => _cooldownSeconds;

        public int Strikes => _strikes;

        public float DamageScale => _damageScale;
    }
}
