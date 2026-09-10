namespace Game.Simulation.Services
{
    public sealed class SimulationRandom
    {
        private const uint SeedFallback = 0x9E3779B9u;
        private const float UnitScale = 1f / 16777216f;

        private uint _state;

        public SimulationRandom(int seed)
        {
            _state = seed == 0 ? SeedFallback : unchecked((uint)seed);
        }

        public float NextUnit()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;

            return (_state >> 8) * UnitScale;
        }
    }
}
