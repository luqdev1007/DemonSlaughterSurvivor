using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ExecutionThirstConfig", menuName = "Game/Content/Execution Thirst Config")]
    public sealed class ExecutionThirstConfig : PerkBehaviourConfig
    {
        [SerializeField] private ExecutionThirstLevel[] _levels = Array.Empty<ExecutionThirstLevel>();

        public override int LevelCount => _levels.Length;

        public ExecutionThirstLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(ExecutionThirstConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' execution thirst '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                ExecutionThirstLevel entry = Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.DamageReduction) || entry.DamageReduction <= 0f || entry.DamageReduction >= 1f)
                    throw new InvalidOperationException(
                        $"{label} level {level} has damage reduction {entry.DamageReduction}; it must be in (0, 1), otherwise it does nothing or makes the hero immortal.");
            }

            for (int index = 0; index < perk.Requires.Count; index++)
            {
                if (perk.Requires[index] != null && perk.Requires[index].Behaviour is ExecutionConfig)
                    return;
            }

            throw new InvalidOperationException(
                $"{label} acts during the execution, but the perk does not require a perk with an {nameof(ExecutionConfig)}, so it could be taken without one.");
        }
    }

    [Serializable]
    public sealed class ExecutionThirstLevel
    {
        [SerializeField, Range(0f, 1f)] private float _damageReduction = 0.3f;

        public float DamageReduction => _damageReduction;
    }
}
