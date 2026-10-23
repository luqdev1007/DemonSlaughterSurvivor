using System.Collections.Generic;

namespace Game.Core
{
    public readonly struct MagicShardLine
    {
        public MagicShardLine(int rarity, int count, int weight)
        {
            Rarity = rarity;
            Count = count;
            Weight = weight;
        }

        public int Rarity { get; }
        public int Count { get; }
        public int Weight { get; }
        public int Total => Count * Weight;
    }

    public sealed class RunRewards
    {
        public RunRewards(
            float effectiveDamage,
            float damageCoefficient,
            int memoryFromDamage,
            string bossId,
            int bossBonus,
            IReadOnlyList<MagicShardLine> magicLines,
            string relicId,
            bool relicRepeat)
        {
            EffectiveDamage = effectiveDamage;
            DamageCoefficient = damageCoefficient;
            MemoryFromDamage = memoryFromDamage;
            BossId = bossId;
            BossBonus = bossBonus;
            MagicLines = magicLines;
            RelicId = relicId;
            RelicRepeat = relicRepeat;

            int magic = 0;

            for (int index = 0; index < magicLines.Count; index++)
                magic += magicLines[index].Total;

            MagicShards = magic;
        }

        public float EffectiveDamage { get; }
        public float DamageCoefficient { get; }
        public int MemoryFromDamage { get; }
        public string BossId { get; }
        public int BossBonus { get; }
        public int MemoryShards => MemoryFromDamage + BossBonus;
        public IReadOnlyList<MagicShardLine> MagicLines { get; }
        public int MagicShards { get; }
        public string RelicId { get; }
        public bool RelicRepeat { get; }
        public bool RelicGranted => string.IsNullOrEmpty(RelicId) == false && RelicRepeat == false;
    }
}
