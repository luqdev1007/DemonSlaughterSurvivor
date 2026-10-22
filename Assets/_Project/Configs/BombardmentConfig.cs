using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "BombardmentConfig", menuName = "Game/Content/Bombardment Config")]
    public sealed class BombardmentConfig : PerkBehaviourConfig
    {
        [SerializeField] private BombardmentLevel[] _levels = Array.Empty<BombardmentLevel>();

        public override int LevelCount => _levels.Length;

        public BombardmentLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(BombardmentConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' bombardment '{name}'";
            bool rain = false;
            bool explosive = false;

            for (int index = 0; index < perk.Requires.Count; index++)
            {
                PerkConfig parent = perk.Requires[index];

                if (parent == null)
                    continue;

                rain |= parent.Behaviour is ArrowRainConfig;
                explosive |= parent.Behaviour is ExplosiveArrowConfig;
            }

            if (rain == false || explosive == false)
                throw new InvalidOperationException(
                    $"{label} detonates arrow rain strikes with the explosive arrow blast, so it must require both an " +
                    $"{nameof(ArrowRainConfig)} perk and an {nameof(ExplosiveArrowConfig)} perk.");

            for (int level = 1; level <= LevelCount; level++)
            {
                BombardmentLevel entry = Level(level);

                if (entry == null || entry.Chance <= 0f || entry.Chance > 1f)
                    throw new InvalidOperationException($"{label} level {level} must have a chance in (0, 1].");
            }
        }
    }

    [Serializable]
    public sealed class BombardmentLevel
    {
        [SerializeField, Range(0f, 1f)] private float _chance = 0.25f;

        public float Chance => _chance;
    }
}
