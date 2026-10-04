using Game.Core;
using System;
using System.Collections.Generic;

namespace Game.Configs
{
    public static class PerkConfigValidator
    {
        public static void Validate(PerkConfig perk)
        {
            if (perk == null)
                throw new InvalidOperationException($"A {nameof(PerkConfig)} slot is empty.");

            if (string.IsNullOrEmpty(perk.Id))
                throw new InvalidOperationException($"{nameof(PerkConfig)} '{perk.name}' has no id.");

            if (perk.MaxLevel == 0)
                throw new InvalidOperationException($"{nameof(PerkConfig)} '{perk.Id}' has no levels, so taking it would change nothing.");

            for (int level = 1; level <= perk.MaxLevel; level++)
            {
                ValidateLevel(perk, level);
            }

            if (perk.Behaviour != null)
                ValidateBehaviour(perk, perk.Behaviour);
        }

        private static void ValidateBehaviour(PerkConfig perk, PerkBehaviourConfig behaviour)
        {
            if (behaviour.LevelCount != perk.MaxLevel)
                throw new InvalidOperationException(
                    $"{nameof(PerkConfig)} '{perk.Id}' behaviour '{behaviour.name}' has {behaviour.LevelCount} levels, " +
                    $"but the perk has {perk.MaxLevel}; every perk level needs its behaviour numbers.");

            behaviour.Validate(perk);
        }

        private static void ValidateLevel(PerkConfig perk, int level)
        {
            PerkLevel entry = perk.Level(level);
            string label = $"{nameof(PerkConfig)} '{perk.Id}' level {level}";

            if (entry == null)
                throw new InvalidOperationException($"{label} is empty.");

            if ((entry.Modifiers == null || entry.Modifiers.Count == 0) && perk.HasBehaviour == false)
                throw new InvalidOperationException($"{label} has no modifiers and the perk has no behaviour, so taking it would change nothing.");

            if (entry.Modifiers == null)
                return;

            IReadOnlyList<StatModifierEntry> modifiers = entry.Modifiers;

            for (int index = 0; index < modifiers.Count; index++)
            {
                StatModifierEntry modifier = modifiers[index];

                if (modifier == null)
                    throw new InvalidOperationException($"{label} has an empty modifier slot {index}.");

                if (Enum.IsDefined(typeof(StatId), modifier.Stat) == false)
                    throw new InvalidOperationException($"{label} modifier {index} targets an unknown stat {(int)modifier.Stat}.");

                if (Enum.IsDefined(typeof(StatOp), modifier.Op) == false)
                    throw new InvalidOperationException($"{label} modifier {index} has an unknown operation {(int)modifier.Op}.");

                if (float.IsNaN(modifier.Value) || float.IsInfinity(modifier.Value))
                    throw new InvalidOperationException($"{label} modifier {index} has a non-finite value.");

                if (modifier.Op != StatOp.Flat && modifier.Value <= -1f)
                    throw new InvalidOperationException(
                        $"{label} modifier {index} is {modifier.Op} {modifier.Value} on {modifier.Stat}. " +
                        "A value of -1 or below zeroes or reverses the stat on its own.");

                for (int other = 0; other < index; other++)
                {
                    if (modifiers[other] != null && modifiers[other].Stat == modifier.Stat)
                        throw new InvalidOperationException(
                            $"{label} lists {modifier.Stat} twice. Both share the perk id as their source, " +
                            "and a stat modifier is keyed by (target, stat, source), so the second would silently replace the first.");
                }
            }
        }
    }
}
