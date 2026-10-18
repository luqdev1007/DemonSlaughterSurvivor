using Game.Configs;
using Game.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Services
{
    public static class ItemRoller
    {
        public const float LuckScale = 100f;

        public static ItemOfferData Roll(ItemLootConfig loot, ref uint state, List<int> scratch)
        {
            return Roll(loot, ref state, scratch, 0f, null);
        }

        public static ItemOfferData Roll(ItemLootConfig loot, ref uint state, List<int> scratch, float luck, ItemFloors floors)
        {
            ItemConfig item = PickItem(loot, ref state, floors);
            Rarity rarity = PickRarity(loot, ref state, luck);
            int rank = PickRank(loot, ref state);

            if (floors != null)
                Raise(item.Slot, floors, ref rank, ref rarity);

            ItemOfferData roll = new ItemOfferData
            {
                ItemId = item.Id,
                Rank = rank,
                Rarity = (int)rarity,
                Main = new StatLine(item.Stat, item.Op, item.Value(rank))
            };

            IReadOnlyList<MinorBonus> pool = loot.MinorPool;
            int count = Mathf.Min(loot.MinorBonuses(rarity), pool.Count);

            scratch.Clear();

            for (int index = 0; index < pool.Count; index++)
                scratch.Add(index);

            for (int index = 0; index < count; index++)
            {
                int pick = index + Mathf.Min(scratch.Count - index - 1, (int)(SimulationRandom.NextUnit(ref state) * (scratch.Count - index)));
                int swap = scratch[index];
                scratch[index] = scratch[pick];
                scratch[pick] = swap;

                MinorBonus bonus = pool[scratch[index]];
                float value = Mathf.Lerp(bonus.Min, bonus.Max, SimulationRandom.NextUnit(ref state));

                roll.Minors.Add(new StatLine(bonus.Stat, bonus.Op, value));
            }

            return roll;
        }

        public static void Raise(ItemSlot slot, ItemFloors floors, ref int rank, ref Rarity rarity)
        {
            if (rank < floors.MinRank)
                rank = Mathf.Min(floors.MinRank, ItemConfig.RankCount);

            if (floors.HasEquipped(slot) == false)
                return;

            int equippedRank = floors.Rank(slot);
            Rarity equippedRarity = (Rarity)floors.Rarity(slot);

            if (rank < equippedRank)
                rank = equippedRank;

            if (rarity < equippedRarity)
                rarity = equippedRarity;

            if (rank > equippedRank || rarity > equippedRarity)
                return;

            if (rank < ItemConfig.RankCount)
            {
                rank++;

                return;
            }

            if ((int)rarity < ItemLootConfig.RarityCount - 1)
                rarity++;
        }

        private static float ItemWeight(WeightedItem item, ItemFloors floors)
        {
            if (floors != null && floors.IsMaxed(item.Item.Slot))
                return 0f;

            return item.Weight;
        }

        private static ItemConfig PickItem(ItemLootConfig loot, ref uint state, ItemFloors floors)
        {
            IReadOnlyList<WeightedItem> items = loot.Items;
            float total = 0f;

            for (int index = 0; index < items.Count; index++)
                total += ItemWeight(items[index], floors);

            if (total <= 0f)
            {
                floors = null;

                for (int index = 0; index < items.Count; index++)
                    total += items[index].Weight;
            }

            float pick = SimulationRandom.NextUnit(ref state) * total;

            for (int index = 0; index < items.Count; index++)
            {
                pick -= ItemWeight(items[index], floors);

                if (pick < 0f)
                    return items[index].Item;
            }

            return items[items.Count - 1].Item;
        }

        private static float RarityWeight(ItemLootConfig loot, int rarity, float luck)
        {
            return loot.RarityWeight((Rarity)rarity) * (1f + Mathf.Max(0f, luck) / LuckScale * rarity);
        }

        private static Rarity PickRarity(ItemLootConfig loot, ref uint state, float luck)
        {
            float total = 0f;

            for (int index = 0; index < ItemLootConfig.RarityCount; index++)
                total += RarityWeight(loot, index, luck);

            float pick = SimulationRandom.NextUnit(ref state) * total;

            for (int index = 0; index < ItemLootConfig.RarityCount; index++)
            {
                pick -= RarityWeight(loot, index, luck);

                if (pick < 0f)
                    return (Rarity)index;
            }

            return Rarity.Common;
        }

        private static int PickRank(ItemLootConfig loot, ref uint state)
        {
            float total = 0f;

            for (int rank = 1; rank <= ItemConfig.RankCount; rank++)
                total += loot.RankWeight(rank);

            float pick = SimulationRandom.NextUnit(ref state) * total;

            for (int rank = 1; rank <= ItemConfig.RankCount; rank++)
            {
                pick -= loot.RankWeight(rank);

                if (pick < 0f)
                    return rank;
            }

            return 1;
        }
    }
}
