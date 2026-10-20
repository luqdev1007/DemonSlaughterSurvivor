using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "SweepingStrikesConfig", menuName = "Game/Content/Sweeping Strikes Config")]
    public sealed class SweepingStrikesConfig : PerkBehaviourConfig
    {
        [SerializeField] private SweepingStrikesLevel[] _levels = Array.Empty<SweepingStrikesLevel>();

        public override int LevelCount => _levels.Length;

        public SweepingStrikesLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(SweepingStrikesConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' sweeping strikes '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                SweepingStrikesLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.ExtraRadius) || float.IsInfinity(entry.ExtraRadius) || entry.ExtraRadius <= 0f)
                    throw new InvalidOperationException($"{label} level {level} adds {entry.ExtraRadius} m; it must be positive.");

                if (float.IsNaN(entry.DamageShare) || entry.DamageShare <= 0f || entry.DamageShare > 1f)
                    throw new InvalidOperationException($"{label} level {level} has damage share {entry.DamageShare}; it must be in (0, 1].");
            }
        }
    }

    [Serializable]
    public sealed class SweepingStrikesLevel
    {
        [SerializeField] private float _extraRadius = 1f;
        [SerializeField, Range(0f, 1f)] private float _damageShare = 0.3f;

        public float ExtraRadius => _extraRadius;

        public float DamageShare => _damageShare;
    }
}
