using Game.Configs;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class PerkBehaviourValidationTests
    {
        private readonly List<Object> _assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void BehaviourPerkMayHaveNoModifiers()
        {
            PerkConfig perk = CreatePerk(levels: 2, CreateCounter(new[] { 0.15f, 0.35f }, new[] { 1f, 2f }));

            Assert.DoesNotThrow(() => PerkConfigValidator.Validate(perk));
        }

        [Test]
        public void PerkWithoutBehaviourAndModifiersIsRejected()
        {
            PerkConfig perk = CreatePerk(levels: 2, counter: null);

            Assert.Throws<InvalidOperationException>(() => PerkConfigValidator.Validate(perk));
        }

        [Test]
        public void CounterStrikeNeedsALevelPerPerkLevel()
        {
            PerkConfig perk = CreatePerk(levels: 3, CreateCounter(new[] { 0.15f, 0.35f }, new[] { 1f, 2f }));

            Assert.Throws<InvalidOperationException>(() => PerkConfigValidator.Validate(perk));
        }

        [TestCase(0f, 1f)]
        [TestCase(1.5f, 1f)]
        [TestCase(0.2f, 0f)]
        public void CounterStrikeRejectsAZeroChanceOrShare(float chance, float share)
        {
            PerkConfig perk = CreatePerk(levels: 1, CreateCounter(new[] { chance }, new[] { share }));

            Assert.Throws<InvalidOperationException>(() => PerkConfigValidator.Validate(perk));
        }

        private PerkConfig CreatePerk(int levels, CounterStrikeConfig counter)
        {
            PerkConfig perk = ScriptableObject.CreateInstance<PerkConfig>();
            _assets.Add(perk);

            PerkLevel[] entries = new PerkLevel[levels];

            for (int index = 0; index < levels; index++)
            {
                entries[index] = new PerkLevel();
                SetField(entries[index], "_modifiers", Array.Empty<StatModifierEntry>());
            }

            SetField(perk, "_id", "perk.test-behaviour");
            SetField(perk, "_levels", entries);
            SetField(perk, "_counterStrike", counter);

            return perk;
        }

        private CounterStrikeConfig CreateCounter(float[] chances, float[] shares)
        {
            SwingBake bake = ScriptableObject.CreateInstance<SwingBake>();
            _assets.Add(bake);

            CounterStrikeConfig counter = ScriptableObject.CreateInstance<CounterStrikeConfig>();
            _assets.Add(counter);

            CounterStrikeLevel[] levels = new CounterStrikeLevel[chances.Length];

            for (int index = 0; index < levels.Length; index++)
            {
                levels[index] = new CounterStrikeLevel();
                SetField(levels[index], "_chance", chances[index]);
                SetField(levels[index], "_damageShare", shares[index]);
            }

            SwingVariant swing = new SwingVariant();
            SetField(swing, "_bake", bake);
            SetField(swing, "_windowStart", 0.2f);
            SetField(swing, "_windowEnd", 0.8f);

            SetField(counter, "_levels", levels);
            SetField(counter, "_swing", swing);

            return counter;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }
    }
}
