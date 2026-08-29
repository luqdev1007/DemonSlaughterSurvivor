namespace Game.Simulation.Services
{
    public sealed class SimulationClock
    {
        public float Delta { get; private set; }
        public float Elapsed { get; private set; }

        public void Advance(float delta)
        {
            Delta = delta;
            Elapsed += delta;
        }
    }
}
