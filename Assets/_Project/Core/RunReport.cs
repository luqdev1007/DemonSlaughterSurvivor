using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class RunReport
    {
        public RunReport(
            string levelId,
            string characterId,
            bool victory,
            float seconds,
            float effectiveDamage,
            float totalDamage,
            float damageTaken,
            float bestDps,
            int enemiesKilled,
            int elitesKilled,
            int itemsFound,
            IReadOnlyList<int> itemsByRarity,
            int gemsCollected,
            int breakablesDestroyed,
            IReadOnlyList<string> bossesKilled,
            RunMode mode = RunMode.Story)
        {
            LevelId = levelId;
            CharacterId = characterId;
            Victory = victory;
            Seconds = seconds;
            EffectiveDamage = effectiveDamage;
            TotalDamage = totalDamage;
            DamageTaken = damageTaken;
            BestDps = bestDps;
            EnemiesKilled = enemiesKilled;
            ElitesKilled = elitesKilled;
            ItemsFound = itemsFound;
            ItemsByRarity = Copy(itemsByRarity);
            GemsCollected = gemsCollected;
            BreakablesDestroyed = breakablesDestroyed;
            BossesKilled = Copy(bossesKilled);
            Mode = mode;
        }

        public string LevelId { get; }
        public string CharacterId { get; }
        public bool Victory { get; }
        public float Seconds { get; }
        public float EffectiveDamage { get; }
        public float TotalDamage { get; }
        public float DamageTaken { get; }
        public float BestDps { get; }
        public int EnemiesKilled { get; }
        public int ElitesKilled { get; }
        public int ItemsFound { get; }
        public IReadOnlyList<int> ItemsByRarity { get; }
        public int GemsCollected { get; }
        public int BreakablesDestroyed { get; }
        public IReadOnlyList<string> BossesKilled { get; }
        public RunMode Mode { get; }

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null)
                return Array.Empty<T>();

            T[] copy = new T[source.Count];

            for (int index = 0; index < copy.Length; index++)
                copy[index] = source[index];

            return copy;
        }
    }
}
