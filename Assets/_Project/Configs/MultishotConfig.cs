using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "MultishotConfig", menuName = "Game/Content/Multishot Config")]
    public sealed class MultishotConfig : PerkBehaviourConfig
    {
        [SerializeField] private MultishotLevel[] _levels = Array.Empty<MultishotLevel>();
        [SerializeField, Range(1f, 45f)] private float _spreadDegrees = 8f;

        public override int LevelCount => _levels.Length;

        public float SpreadDegrees => _spreadDegrees;

        public MultishotLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(MultishotConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            for (int level = 1; level <= LevelCount; level++)
            {
                MultishotLevel entry = Level(level);

                if (entry == null || entry.ExtraArrows < 1)
                    throw new InvalidOperationException(
                        $"{nameof(PerkConfig)} '{perk.Id}' multishot '{name}' level {level} adds no arrows; every level must add at least one.");
            }
        }
    }

    [Serializable]
    public sealed class MultishotLevel
    {
        [SerializeField, Min(1)] private int _extraArrows = 1;

        public int ExtraArrows => _extraArrows;
    }
}
