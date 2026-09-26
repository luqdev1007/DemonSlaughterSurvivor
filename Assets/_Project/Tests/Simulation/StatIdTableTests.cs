using Game.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Game.Simulation.Tests
{
    public sealed class StatIdTableTests
    {
        [Test]
        public void StatIdNumbersAreFrozen()
        {
            AssertFrozen<StatId>(new Dictionary<string, int>
            {
                { "MoveSpeed", 1 },
                { "MaxHealth", 2 },
                { "ContactDamage", 3 },
                { "DashCooldown", 4 },
            });
        }

        [Test]
        public void StatOpNumbersAreFrozen()
        {
            AssertFrozen<StatOp>(new Dictionary<string, int>
            {
                { "Flat", 1 },
                { "Increased", 2 },
                { "More", 3 },
            });
        }

        private static void AssertFrozen<TEnum>(Dictionary<string, int> expected) where TEnum : Enum
        {
            string[] names = Enum.GetNames(typeof(TEnum));

            Assert.AreEqual(
                expected.Count,
                names.Length,
                $"{typeof(TEnum).Name} gained or lost a member. Add new members with a new number at the end of this table; " +
                "never reuse or renumber, configs and saves store the number.");

            for (int index = 0; index < names.Length; index++)
            {
                string name = names[index];

                Assert.IsTrue(expected.ContainsKey(name), $"{typeof(TEnum).Name}.{name} is not in the frozen table.");

                int actual = Convert.ToInt32(Enum.Parse(typeof(TEnum), name));

                Assert.AreEqual(
                    expected[name],
                    actual,
                    $"{typeof(TEnum).Name}.{name} was renumbered. Configs and saves store the number, " +
                    "so a renumbered member silently turns every stored value into another stat.");
            }
        }
    }
}
