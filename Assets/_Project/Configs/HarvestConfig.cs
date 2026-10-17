using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "HarvestConfig", menuName = "Game/Content/Harvest Config")]
    public sealed class HarvestConfig : PerkBehaviourConfig
    {
        [SerializeField] private HarvestLevel[] _levels = Array.Empty<HarvestLevel>();
        [SerializeField, Min(2)] private int _minTargets = 2;

        public override int LevelCount => _levels.Length;

        public int MinTargets => _minTargets;

        public HarvestLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(HarvestConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public float HealShare(HarvestLevel level, int targets)
        {
            if (targets < _minTargets)
                return 0f;

            return level.HealPerTarget * targets * (1f + level.BonusPerExtraTarget * (targets - 1));
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' harvest '{name}'";

            if (_minTargets < 2)
                throw new InvalidOperationException($"{label} has {nameof(_minTargets)} {_minTargets}; one target is ordinary life steal, not a harvest.");

            for (int level = 1; level <= LevelCount; level++)
            {
                HarvestLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.HealPerTarget) || entry.HealPerTarget <= 0f || entry.HealPerTarget >= 1f)
                    throw new InvalidOperationException($"{label} level {level} heals {entry.HealPerTarget} of max health per target; it must be in (0, 1).");

                if (float.IsNaN(entry.BonusPerExtraTarget) || entry.BonusPerExtraTarget < 0f)
                    throw new InvalidOperationException($"{label} level {level} has bonus per extra target {entry.BonusPerExtraTarget}; it must be zero or positive.");
            }
        }
    }

    [Serializable]
    public sealed class HarvestLevel
    {
        [SerializeField] private float _healPerTarget = 0.004f;
        [SerializeField] private float _bonusPerExtraTarget = 0.25f;

        public float HealPerTarget => _healPerTarget;

        public float BonusPerExtraTarget => _bonusPerExtraTarget;
    }
}
