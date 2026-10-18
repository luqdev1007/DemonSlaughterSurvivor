using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "PunishingLeapConfig", menuName = "Game/Content/Punishing Leap Config")]
    public sealed class PunishingLeapConfig : PerkBehaviourConfig
    {
        [SerializeField] private PunishingLeapLevel[] _levels = Array.Empty<PunishingLeapLevel>();
        [SerializeField, Range(0f, 1f)] private float _healthThreshold = 0.5f;

        [Header("View")]
        [SerializeField] private GameObject _impactFxPrefab;
        [SerializeField] private float _impactFxSeconds = 1f;

        public override int LevelCount => _levels.Length;

        public float HealthThreshold => _healthThreshold;

        public GameObject ImpactFxPrefab => _impactFxPrefab;

        public float ImpactFxSeconds => _impactFxSeconds;

        public PunishingLeapLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(PunishingLeapConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' punishing leap '{name}'";

            if (float.IsNaN(_healthThreshold) || _healthThreshold <= 0f || _healthThreshold >= 1f)
                throw new InvalidOperationException($"{label} has health threshold {_healthThreshold}; it must be between 0 and 1, otherwise it finishes nobody or everybody.");

            for (int level = 1; level <= LevelCount; level++)
            {
                PunishingLeapLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.CooldownSeconds) || entry.CooldownSeconds < 0f)
                    throw new InvalidOperationException($"{label} level {level} has cooldown {entry.CooldownSeconds} s; it must not be negative.");
            }

            if (RequiresLeap(perk) == false)
                throw new InvalidOperationException(
                    $"{label} reacts to the heroic leap landing, but the perk does not require a perk with a {nameof(HeroicLeapConfig)}, so it could be taken without a leap.");
        }

        private static bool RequiresLeap(PerkConfig perk)
        {
            for (int index = 0; index < perk.Requires.Count; index++)
            {
                if (perk.Requires[index] != null && perk.Requires[index].Behaviour is HeroicLeapConfig)
                    return true;
            }

            return false;
        }
    }

    [Serializable]
    public sealed class PunishingLeapLevel
    {
        [SerializeField, Min(0f)] private float _cooldownSeconds = 8f;

        public float CooldownSeconds => _cooldownSeconds;
    }
}
