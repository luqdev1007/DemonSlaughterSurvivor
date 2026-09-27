using Game.Core;

namespace Game.Simulation.Services
{
    public sealed class SimulationRandom
    {
        private const uint SeedFallback = 0x9E3779B9u;
        private const float UnitScale = 1f / 16777216f;

        private uint _state;

        public SimulationRandom(int seed)
        {
            _state = SeedState(seed);
        }

        public float NextUnit()
        {
            return NextUnit(ref _state);
        }

        public static uint SeedState(int seed)
        {
            return seed == 0 ? SeedFallback : unchecked((uint)seed);
        }

        public static uint StreamState(int runSeed, string streamId)
        {
            uint state = unchecked((uint)runSeed) ^ StableHash.Fnv1a32(streamId);

            state ^= state >> 16;
            state = unchecked(state * 0x7FEB352Du);
            state ^= state >> 15;
            state = unchecked(state * 0x846CA68Bu);
            state ^= state >> 16;

            return state == 0 ? SeedFallback : state;
        }

        public static float NextUnit(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;

            return (state >> 8) * UnitScale;
        }
    }
}
