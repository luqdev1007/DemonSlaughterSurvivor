using Game.Core;

namespace Game.UI
{
    public sealed class RunStats : IRunStatsSink
    {
        public float DamageTaken { get; private set; }

        public int GemsCollected { get; private set; }

        public void RecordDamageTaken(float amount)
        {
            DamageTaken += amount;
        }

        public void RecordGemCollected()
        {
            GemsCollected++;
        }
    }
}
