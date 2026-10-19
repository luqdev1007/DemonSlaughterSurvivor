using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "SearingFireballConfig", menuName = "Game/Content/Searing Fireball Config")]
    public sealed class SearingFireballConfig : PerkBehaviourConfig
    {
        [SerializeField] private SearingFireballLevel[] _levels = Array.Empty<SearingFireballLevel>();
        [SerializeField, Min(0.05f)] private float _tickSeconds = 0.5f;

        [Header("View")]
        [SerializeField] private GameObject _burningFxPrefab;

        public override int LevelCount => _levels.Length;

        public float TickSeconds => _tickSeconds;

        public GameObject BurningFxPrefab => _burningFxPrefab;

        public SearingFireballLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(SearingFireballConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' searing fireball '{name}'";

            if (_burningFxPrefab == null)
                throw new InvalidOperationException($"{label} is missing a view prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                SearingFireballLevel entry = Level(level);

                if (entry == null || entry.DamagePerSecond <= 0 || entry.Seconds <= 0 || entry.CritChanceBonus <= 0)
                    throw new InvalidOperationException($"{label} level {level} must have positive values.");
            }
        }
    }

    [Serializable]
    public sealed class SearingFireballLevel
    {
        [SerializeField, Min(0.01f)] private float _damagePerSecond = 5f;
        [SerializeField, Min(0.1f)] private float _seconds = 3f;
        [SerializeField, Range(0.01f, 1f)] private float _critChanceBonus = 0.1f;

        public float DamagePerSecond => _damagePerSecond;

        public float Seconds => _seconds;

        public float CritChanceBonus => _critChanceBonus;
    }
}
