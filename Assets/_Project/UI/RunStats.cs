using Game.Core;
using System.Collections.Generic;

namespace Game.UI
{
    public sealed class RunStats : IRunStatsSink
    {
        private const int RarityCount = 4;

        private readonly int[] _itemsByRarity = new int[RarityCount];
        private readonly List<string> _bossesKilled = new List<string>();

        public float DamageTaken { get; private set; }

        public int GemsCollected { get; private set; }

        public int EnemiesKilled { get; private set; }

        public int ItemsFound { get; private set; }

        public float DamageDealt { get; private set; }

        public float EffectiveDamage { get; private set; }

        public int ElitesKilled { get; private set; }

        public int BreakablesDestroyed { get; private set; }

        public IReadOnlyList<int> ItemsByRarity => _itemsByRarity;

        public IReadOnlyList<string> BossesKilled => _bossesKilled;

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

        public void RecordItemTaken(int rarity)
        {
            if (rarity < 0 || rarity >= RarityCount)
                return;

            _itemsByRarity[rarity]++;
        }

        public void RecordDamageDealt(float amount, float effective)
        {
            DamageDealt += amount;
            EffectiveDamage += effective;
        }

        public void RecordEliteKilled()
        {
            ElitesKilled++;
        }

        public void RecordBreakableDestroyed()
        {
            BreakablesDestroyed++;
        }

        public void RecordBossKilled(string bossId)
        {
            if (string.IsNullOrEmpty(bossId) || _bossesKilled.Contains(bossId))
                return;

            _bossesKilled.Add(bossId);
        }
    }
}
