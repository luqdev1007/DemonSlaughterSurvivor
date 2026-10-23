using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class RunLoadout
    {
        public static readonly RunLoadout Empty = new RunLoadout(
            Array.Empty<StatModifierSpec>(), Array.Empty<TuningSpec>(), Array.Empty<GrantedPerkSpec>());

        public RunLoadout(IReadOnlyList<StatModifierSpec> modifiers, IReadOnlyList<TuningSpec> tuning, IReadOnlyList<GrantedPerkSpec> grantedPerks)
        {
            Modifiers = Copy(modifiers);
            Tuning = Copy(tuning);
            GrantedPerks = Copy(grantedPerks);
        }

        public IReadOnlyList<StatModifierSpec> Modifiers { get; }
        public IReadOnlyList<TuningSpec> Tuning { get; }
        public IReadOnlyList<GrantedPerkSpec> GrantedPerks { get; }

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            T[] copy = new T[source.Count];

            for (int index = 0; index < copy.Length; index++)
                copy[index] = source[index];

            return copy;
        }
    }
}
