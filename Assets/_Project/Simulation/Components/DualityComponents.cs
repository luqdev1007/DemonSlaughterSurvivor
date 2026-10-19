using Game.Configs;

namespace Game.Simulation.Components
{
    public struct Duality
    {
        public DualityConfig Config;
        public float Demonic;
        public float LastHealth;
        public int CalmTicks;
        public bool WasLeaping;
    }
}
