using Game.Configs;

namespace Game.Simulation.Components
{
    public struct Rune { public RuneConfig Config; }

    public struct ActiveRune
    {
        public RuneConfig Config;
        public int RemainingTicks;
        public int TotalTicks;
        public int PulseTicks;
    }
}
