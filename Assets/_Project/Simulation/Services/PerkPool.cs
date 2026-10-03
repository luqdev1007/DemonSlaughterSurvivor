using Game.Configs;
using System;
using System.Collections.Generic;

namespace Game.Simulation.Services
{
    public sealed class PerkPool
    {
        public const int MaxOffers = 3;

        private readonly PerkConfig[] _perks;
        private readonly int[] _candidates;

        public PerkPool(IReadOnlyList<PerkConfig> perks)
        {
            if (perks == null)
                throw new ArgumentNullException(nameof(perks));

            _perks = new PerkConfig[perks.Count];

            for (int index = 0; index < perks.Count; index++)
            {
                PerkConfigValidator.Validate(perks[index]);

                _perks[index] = perks[index];
            }

            Array.Sort(_perks, CompareById);

            _candidates = new int[_perks.Length];
        }

        public int Count => _perks.Length;

        public PerkConfig this[int index] => _perks[index];

        public int[] Candidates => _candidates;

        public int Draw(int candidateCount, ref uint state)
        {
            if (candidateCount < 0 || candidateCount > _candidates.Length)
                throw new ArgumentOutOfRangeException(nameof(candidateCount));

            int picks = Math.Min(MaxOffers, candidateCount);

            for (int slot = 0; slot < picks; slot++)
            {
                int remaining = candidateCount - slot;
                int chosen = slot + Math.Min(remaining - 1, (int)(SimulationRandom.NextUnit(ref state) * remaining));

                int swap = _candidates[slot];
                _candidates[slot] = _candidates[chosen];
                _candidates[chosen] = swap;
            }

            return picks;
        }

        private static int CompareById(PerkConfig left, PerkConfig right)
        {
            return string.CompareOrdinal(left.Id, right.Id);
        }
    }
}
