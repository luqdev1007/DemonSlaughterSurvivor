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

            if (perk.CounterStrike != null)
                ValidateCounterStrike(perk);
        }

        private static void ValidateCounterStrike(PerkConfig perk)
        {
            CounterStrikeConfig counter = perk.CounterStrike;
            string label = $"{nameof(PerkConfig)} '{perk.Id}' counter strike '{counter.name}'";

            if (counter.LevelCount != perk.MaxLevel)
                throw new InvalidOperationException(
                    $"{label} has {counter.LevelCount} levels, but the perk has {perk.MaxLevel}; every perk level needs its chance and damage share.");

            for (int level = 1; level <= counter.LevelCount; level++)
            {
                CounterStrikeLevel entry = counter.Level(level);

                if (entry == null)
                    throw new InvalidOperationException($"{label} level {level} is empty.");

                if (float.IsNaN(entry.Chance) || entry.Chance <= 0f || entry.Chance > 1f)
                    throw new InvalidOperationException($"{label} level {level} has chance {entry.Chance}; it must be in (0, 1].");

                if (float.IsNaN(entry.DamageShare) || float.IsInfinity(entry.DamageShare) || entry.DamageShare <= 0f)
                    throw new InvalidOperationException($"{label} level {level} has damage share {entry.DamageShare}; it must be positive.");
            }

            SwingVariant swing = counter.Swing;

            if (swing == null || swing.Bake == null)
                throw new InvalidOperationException($"{label} has no {nameof(SwingBake)} for its swing.");

            if (swing.PlaybackSpeed <= 0f || swing.WindowEnd <= swing.WindowStart)
                throw new InvalidOperationException(
                    $"{label} swing has playback speed {swing.PlaybackSpeed} and window {swing.WindowStart}-{swing.WindowEnd}; bake it with Game/Bake Weapon Swings.");
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
