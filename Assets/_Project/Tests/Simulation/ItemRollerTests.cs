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

        private static ItemLootConfig Load()
        {
            ItemLootConfig loot = AssetDatabase.LoadAssetAtPath<ItemLootConfig>(LootPath);

            Assert.IsNotNull(loot, $"{LootPath} is missing.");

            return loot;
        }
    }
}
