using Game.Configs;
using System;

namespace Game.Simulation.Services
{
    public sealed class ItemFloors
    {
        private static readonly int SlotCount = Enum.GetValues(typeof(ItemSlot)).Length;

        private readonly bool[] _equipped = new bool[SlotCount];
        private readonly int[] _ranks = new int[SlotCount];
        private readonly int[] _rarities = new int[SlotCount];

        public int MinRank { get; set; } = 1;

        public void Clear()
        {
            Array.Clear(_equipped, 0, _equipped.Length);
            Array.Clear(_ranks, 0, _ranks.Length);
            Array.Clear(_rarities, 0, _rarities.Length);
            MinRank = 1;
        }

        public void SetEquipped(ItemSlot slot, int rank, int rarity)
        {
            _equipped[(int)slot] = true;
            _ranks[(int)slot] = rank;
            _rarities[(int)slot] = rarity;
        }

        public bool HasEquipped(ItemSlot slot)
        {
            return _equipped[(int)slot];
        }

        public int Rank(ItemSlot slot)
        {
            return _ranks[(int)slot];
        }

        public int Rarity(ItemSlot slot)
        {
            return _rarities[(int)slot];
        }

        public bool IsMaxed(ItemSlot slot)
        {
            return _equipped[(int)slot] && _ranks[(int)slot] >= ItemConfig.RankCount && _rarities[(int)slot] >= ItemLootConfig.RarityCount - 1;
        }
    }
}
