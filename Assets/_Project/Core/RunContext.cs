using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class RunContext
    {
        public RunContext(string levelId, string characterId, RunMode mode, int seed, IReadOnlyList<StatModifierSpec> startModifiers)
        {
            if (startModifiers == null)
                throw new ArgumentNullException(nameof(startModifiers));

            LevelId = levelId;
            CharacterId = characterId;
            Mode = mode;
            Seed = seed;

            StatModifierSpec[] copy = new StatModifierSpec[startModifiers.Count];

            for (int index = 0; index < copy.Length; index++)
                copy[index] = startModifiers[index];

            StartModifiers = copy;
        }

        public string LevelId { get; }
        public string CharacterId { get; }
        public RunMode Mode { get; }
        public int Seed { get; }
        public IReadOnlyList<StatModifierSpec> StartModifiers { get; }
    }
}
