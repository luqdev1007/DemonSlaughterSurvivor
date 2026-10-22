using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ChakramReturnConfig", menuName = "Game/Content/Chakram Return Config")]
    public sealed class ChakramReturnConfig : PerkBehaviourConfig
    {
        [SerializeField] private ChakramReturnLevel[] _levels = Array.Empty<ChakramReturnLevel>();

        public override int LevelCount => _levels.Length;

        public ChakramReturnLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(ChakramReturnConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' chakram return '{name}'";
            bool chakram = false;

            for (int index = 0; index < perk.Requires.Count; index++)
            {
                PerkConfig parent = perk.Requires[index];

                if (parent != null)
                    chakram |= parent.Behaviour is ChakramConfig;
            }

            if (chakram == false)
                throw new InvalidOperationException($"{label} brings chakrams back, so it must require a {nameof(ChakramConfig)} perk.");

            for (int level = 1; level <= LevelCount; level++)
            {
                ChakramReturnLevel entry = Level(level);

                if (entry == null || entry.DamageScale <= 0f)
                    throw new InvalidOperationException($"{label} level {level} must have a positive damage share for the way back.");
            }
        }
    }

    [Serializable]
    public sealed class ChakramReturnLevel
    {
        [SerializeField, Min(0.01f)] private float _damageScale = 1f;

        public float DamageScale => _damageScale;
    }
}
