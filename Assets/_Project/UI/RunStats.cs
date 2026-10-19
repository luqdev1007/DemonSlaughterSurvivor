using Game.Core;

namespace Game.UI
{
    public sealed class RunStats : IRunStatsSink
    {
        public float DamageTaken { get; private set; }

        public int GemsCollected { get; private set; }

        public int EnemiesKilled { get; private set; }

        public int ItemsFound { get; private set; }

        public void RecordDamageTaken(float amount)
        {
            DamageTaken += amount;
        }

        public void RecordGemCollected()
        {
            GemsCollected++;
        }

        public void RecordEnemyKilled()
        {
            EnemiesKilled++;
        }

        public void RecordItemFound()
        {
            ItemsFound++;
        }
    }
}
