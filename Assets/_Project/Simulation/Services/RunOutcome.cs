using Game.Core;

namespace Game.Simulation.Services
{
    public sealed class RunOutcome
    {
        public bool IsFinished { get; private set; }

        public RunEndReason Reason { get; private set; }

        public void Finish(RunEndReason reason)
        {
            if (IsFinished)
                return;

            IsFinished = true;
            Reason = reason;
        }
    }
}
