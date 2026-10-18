using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "PowerShotConfig", menuName = "Game/Content/Power Shot Config")]
    public sealed class PowerShotConfig : PerkBehaviourConfig
    {
        [SerializeField] private PowerShotLevel[] _levels = Array.Empty<PowerShotLevel>();
        [SerializeField, Min(1f)] private float _drawMultiplier = 1.6f;
        [SerializeField, Min(1f)] private float _widthMultiplier = 2f;
        [SerializeField] private GameObject _arrowPrefab;

        public override int LevelCount => _levels.Length;

        public float DrawMultiplier => _drawMultiplier;

        public float WidthMultiplier => _widthMultiplier;

        public GameObject ArrowPrefab => _arrowPrefab;

        public PowerShotLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(PowerShotConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' power shot '{name}'";

            if (_arrowPrefab == null)
                throw new InvalidOperationException($"{label} has no empowered arrow prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                PowerShotLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (entry.CooldownSeconds <= 0f || entry.DamageMultiplier < 1f || entry.CritChanceBonus < 0f)
                    throw new InvalidOperationException(
                        $"{label} level {level} has cooldown {entry.CooldownSeconds}, damage x{entry.DamageMultiplier}, crit +{entry.CritChanceBonus}; " +
                        "the cooldown must be positive, the damage at least x1 and the crit bonus not negative.");
            }
        }
    }

    [Serializable]
    public sealed class PowerShotLevel
    {
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 6f;
        [SerializeField, Min(1f)] private float _damageMultiplier = 1.5f;
        [SerializeField, Range(0f, 1f)] private float _critChanceBonus = 0.25f;

        public float CooldownSeconds => _cooldownSeconds;

        public float DamageMultiplier => _damageMultiplier;

        public float CritChanceBonus => _critChanceBonus;
    }
}
