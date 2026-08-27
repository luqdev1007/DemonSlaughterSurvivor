namespace Game.Simulation.Services
{
    /// <summary>
    /// Run time. Systems read it instead of UnityEngine.Time; only the run entry point advances it.
    /// </summary>
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
