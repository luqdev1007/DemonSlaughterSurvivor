namespace Game.Simulation.Services
{
    public sealed class UpgradeChoiceGate
    {
        public bool IsAwaiting { get; private set; }

        public void Await()
        {
            IsAwaiting = true;
        }

        public void Release()
        {
            IsAwaiting = false;
        }
    }
}
