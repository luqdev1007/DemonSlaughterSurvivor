namespace Game.Simulation.Services
{
    public sealed class RunOutcome
    {
        public bool IsFinished { get; private set; }

        public void Finish()
        {
            IsFinished = true;
        }
    }
}
