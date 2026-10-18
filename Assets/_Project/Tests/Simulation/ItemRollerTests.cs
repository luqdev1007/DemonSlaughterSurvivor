using Game.Configs;
using Game.Core;
using Game.Simulation.Services;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;

namespace Game.Simulation.Tests
{
    public sealed class ItemRollerTests
    {
        private const string LootPath = "Assets/_Project/Configs/Items/ItemLoot_Gameplay.asset";
        private const int Rolls = 2000;

        [Test]
        public void TheSameSeedRollsTheSameItems()
        {
            ItemLootConfig loot = Load();
            List<int> scratch = new List<int>();

            uint first = SimulationRandom.SeedState(987654321);
            uint second = SimulationRandom.SeedState(987654321);

            for (int index = 0; index < 50; index++)
            {
                ItemOfferData a = ItemRoller.Roll(loot, ref first, scratch);
                ItemOfferData b = ItemRoller.Roll(loot, ref second, scratch);

                Assert.AreEqual(a.ItemId, b.ItemId);
                Assert.AreEqual(a.Rank, b.Rank);
                Assert.AreEqual(a.Rarity, b.Rarity);
                Assert.AreEqual(a.Minors.Count, b.Minors.Count);
            }
        }

        [Test]
        public void RarityDecidesTheMinorBonusesAndRankStaysInRange()
        {
            ItemLootConfig loot = Load();
            List<int> scratch = new List<int>();
            uint state = SimulationRandom.SeedState(42);

            for (int index = 0; index < Rolls; index++)
            {
                ItemOfferData roll = ItemRoller.Roll(loot, ref state, scratch);

                Assert.That(roll.Rank, Is.InRange(1, ItemConfig.RankCount));
                Assert.AreEqual(loot.MinorBonuses((Rarity)roll.Rarity), roll.Minors.Count, $"rarity {roll.Rarity}");

                HashSet<StatId> stats = new HashSet<StatId>();

                foreach (StatLine minor in roll.Minors)
                    Assert.IsTrue(stats.Add(minor.Stat), "one item never rolls the same minor bonus twice");
            }
        }

        [Test]
        public void AnOfferNeverDowngradesTheEquippedItemOfItsSlot()
        {
            ItemLootConfig loot = Load();
            List<int> scratch = new List<int>();
            uint state = SimulationRandom.SeedState(7);

            ItemFloors floors = new ItemFloors();
            floors.SetEquipped(ItemSlot.Weapon, 3, (int)Rarity.Rare);
            floors.SetEquipped(ItemSlot.Armor, 5, (int)Rarity.Common);
            floors.SetEquipped(ItemSlot.Ring, 1, (int)Rarity.Epic);
            floors.SetEquipped(ItemSlot.Boots, 5, (int)Rarity.Legendary);

            for (int index = 0; index < Rolls; index++)
            {
                ItemOfferData roll = ItemRoller.Roll(loot, ref state, scratch, 0f, floors);
                ItemSlot slot = Item(loot, roll.ItemId).Slot;

                Assert.AreNotEqual(ItemSlot.Boots, slot, "a maxed slot is never offered while others are open");

                if (floors.HasEquipped(slot) == false)
                    continue;

                Assert.GreaterOrEqual(roll.Rank, floors.Rank(slot), $"{roll.ItemId} rank");
                Assert.GreaterOrEqual(roll.Rarity, floors.Rarity(slot), $"{roll.ItemId} rarity");
                Assert.IsTrue(roll.Rank > floors.Rank(slot) || roll.Rarity > floors.Rarity(slot), $"{roll.ItemId} must be strictly better");
                Assert.AreEqual(loot.MinorBonuses((Rarity)roll.Rarity), roll.Minors.Count);
            }
        }

        [Test]
        public void TheRankFloorRisesWithLootWindows()
        {
            ItemLootConfig loot = Load();
            List<int> scratch = new List<int>();
            uint state = SimulationRandom.SeedState(11);

            ItemFloors floors = new ItemFloors();
            floors.MinRank = loot.RankFloor(6);

            Assert.AreEqual(3, floors.MinRank);

            for (int index = 0; index < Rolls; index++)
                Assert.GreaterOrEqual(ItemRoller.Roll(loot, ref state, scratch, 0f, floors).Rank, 3);
        }

        [Test]
        public void LuckRaisesTheShareOfRareItems()
        {
            ItemLootConfig loot = Load();
            List<int> scratch = new List<int>();

            Assert.Greater(Legendaries(loot, scratch, 100f), Legendaries(loot, scratch, 0f) * 2);
        }

        private static int Legendaries(ItemLootConfig loot, List<int> scratch, float luck)
        {
            uint state = SimulationRandom.SeedState(5);
            int count = 0;

            for (int index = 0; index < 8000; index++)
            {
                if (ItemRoller.Roll(loot, ref state, scratch, luck, null).Rarity == (int)Rarity.Legendary)
                    count++;
            }

            return count;
        }

        private static ItemConfig Item(ItemLootConfig loot, string id)
        {
            foreach (WeightedItem item in loot.Items)
            {
                if (item.Item.Id == id)
                    return item.Item;
            }

            Assert.Fail($"{id} is not in the loot table.");

            return null;
        }

        private static ItemLootConfig Load()
        {
            ItemLootConfig loot = AssetDatabase.LoadAssetAtPath<ItemLootConfig>(LootPath);

            Assert.IsNotNull(loot, $"{LootPath} is missing.");

            return loot;
        }
    }
}
