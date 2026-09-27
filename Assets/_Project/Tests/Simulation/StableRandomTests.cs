using Game.Core;
using Game.Simulation.Services;
using NUnit.Framework;
using System;

namespace Game.Simulation.Tests
{
    public sealed class StableRandomTests
    {
        [TestCase("", 2166136261u)]
        [TestCase("a", 3826002220u)]
        [TestCase("weapon.berserk_sword", 3838912619u)]
        [TestCase("character.berserk", 2398331752u)]
        [TestCase("меч", 3525272274u)]
        [TestCase("a\U0001F5E1b", 832730385u)]
        public void Fnv1aOfUtf8BytesIsFrozen(string value, uint expected)
        {
            Assert.AreEqual(expected, StableHash.Fnv1a32(value));
        }

        [Test]
        public void HashRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => StableHash.Fnv1a32(null));
        }

        [Test]
        public void InstanceAndStaticStepProduceTheSameSequence()
        {
            SimulationRandom instance = new SimulationRandom(987654321);
            uint state = SimulationRandom.SeedState(987654321);

            for (int index = 0; index < 1000; index++)
                Assert.AreEqual(BitConverter.SingleToInt32Bits(instance.NextUnit()), BitConverter.SingleToInt32Bits(SimulationRandom.NextUnit(ref state)));
        }

        [Test]
        public void StreamsOfDifferentIdsDiverge()
        {
            uint first = SimulationRandom.StreamState(987654321, "weapon.berserk_sword");
            uint second = SimulationRandom.StreamState(987654321, "weapon.other");
            uint spawn = SimulationRandom.SeedState(987654321);

            Assert.AreNotEqual(first, second);
            Assert.AreNotEqual(first, spawn);

            int equal = 0;

            for (int index = 0; index < 100; index++)
            {
                if (SimulationRandom.NextUnit(ref first) == SimulationRandom.NextUnit(ref second))
                    equal++;
            }

            Assert.Less(equal, 5);
        }

        [Test]
        public void StreamStateIsStableForTheSameInput()
        {
            Assert.AreEqual(SimulationRandom.StreamState(987654321, "weapon.berserk_sword"), SimulationRandom.StreamState(987654321, "weapon.berserk_sword"));
            Assert.AreNotEqual(0u, SimulationRandom.StreamState(0, ""));
        }
    }
}
