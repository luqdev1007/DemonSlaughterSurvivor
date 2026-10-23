using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class RunContext
    {
        public RunContext(string levelId, string characterId, RunMode mode, int seed, IReadOnlyList<StatModifierSpec> startModifiers)
            : this(levelId, characterId, mode, seed, new RunLoadout(startModifiers, Array.Empty<TuningSpec>(), Array.Empty<GrantedPerkSpec>()))
        {
        }

        public RunContext(string levelId, string characterId, RunMode mode, int seed, RunLoadout loadout)
        {
            if (loadout == null)
                throw new ArgumentNullException(nameof(loadout));

            LevelId = levelId;
            CharacterId = characterId;
            Mode = mode;
            Seed = seed;
            StartModifiers = loadout.Modifiers;
            Tuning = loadout.Tuning;
            GrantedPerks = loadout.GrantedPerks;
        }

        public string LevelId { get; }
        public string CharacterId { get; }
        public RunMode Mode { get; }
        public int Seed { get; }
        public IReadOnlyList<StatModifierSpec> StartModifiers { get; }
        public IReadOnlyList<TuningSpec> Tuning { get; }
        public IReadOnlyList<GrantedPerkSpec> GrantedPerks { get; }
    }
}
