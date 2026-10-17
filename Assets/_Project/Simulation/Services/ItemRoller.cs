using Game.Configs;
using Game.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Services
{
    public static class ItemRoller
    {
        public static ItemOfferData Roll(ItemLootConfig loot, ref uint state, List<int> scratch)
        {
            ItemConfig item = PickItem(loot, ref state);
            Rarity rarity = PickRarity(loot, ref state);
            int rank = PickRank(loot, ref state);

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

        private static ItemConfig PickItem(ItemLootConfig loot, ref uint state)
        {
            IReadOnlyList<WeightedItem> items = loot.Items;
            float total = 0f;

            for (int index = 0; index < items.Count; index++)
                total += items[index].Weight;

            float pick = SimulationRandom.NextUnit(ref state) * total;

            for (int index = 0; index < items.Count; index++)
            {
                pick -= items[index].Weight;

                if (pick < 0f)
                    return items[index].Item;
            }

            return items[items.Count - 1].Item;
        }

        private static Rarity PickRarity(ItemLootConfig loot, ref uint state)
        {
            float total = 0f;

            for (int index = 0; index < ItemLootConfig.RarityCount; index++)
                total += loot.RarityWeight((Rarity)index);

            float pick = SimulationRandom.NextUnit(ref state) * total;

            for (int index = 0; index < ItemLootConfig.RarityCount; index++)
            {
                pick -= loot.RarityWeight((Rarity)index);

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
