namespace Game.Core
{
    public interface IRunStatsSink
    {
        void RecordDamageTaken(float amount);

        void RecordGemCollected();

        void RecordEnemyKilled();

        void RecordItemFound();

        void RecordItemTaken(int rarity);

        void RecordDamageDealt(float amount, float effective);

        void RecordEliteKilled();

        void RecordBreakableDestroyed();

        void RecordBossKilled(string bossId);
    }
}
