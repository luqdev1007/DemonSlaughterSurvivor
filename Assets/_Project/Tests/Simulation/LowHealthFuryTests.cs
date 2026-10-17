using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class LowHealthFuryTests
    {
        private const string CharacterId = "character.fury.test";

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            LowHealthFuryConfig fury = Own(ScriptableObject.CreateInstance<LowHealthFuryConfig>());

            CharacterConfig character = Own(ScriptableObject.CreateInstance<CharacterConfig>());
            SetField(character, "_id", CharacterId);
            SetField(character, "_lowHealthFury", fury);

            _world = new EcsWorld();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Health>().Add(_hero).Current = 100f;
            _world.GetPool<MaxHealth>().Add(_hero).Value = 100f;

            _systems = new EcsSystems(_world);
            _systems.Add(new ApplyLowHealthFurySystem());
            _systems.Inject(
                (IContentRegistry)new ContentRegistry(new IContentEntry[] { character }),
                new RunContext("level.test", CharacterId, RunMode.Story, 1, Array.Empty<StatModifierSpec>()),
                new StatModifiers(_world));
            _systems.Init();
        }

        [TearDown]
        public void TearDown()
        {
            _systems.Destroy();
            _world.Destroy();

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void AtFullHealthThereIsNoBonus()
        {
            _systems.Run();

            Assert.AreEqual(0, Modifiers().Count);
        }

        [Test]
        public void HalfHealthGivesHalfOfEveryBonusAsMore()
        {
            SetHealth(50f);
            _systems.Run();

            Dictionary<StatId, float> modifiers = Modifiers();

            Assert.AreEqual(5, modifiers.Count);
            Assert.AreEqual(0.3f, modifiers[StatId.WeaponDamage], 1e-5f);
            Assert.AreEqual(0.2f, modifiers[StatId.AttackSpeed], 1e-5f);
            Assert.AreEqual(0.125f, modifiers[StatId.MoveSpeed], 1e-5f);
            Assert.AreEqual(0.5f, modifiers[StatId.LifeOnKill], 1e-5f);
            Assert.AreEqual(0.5f, modifiers[StatId.LifeOnHit], 1e-5f);
        }

        [Test]
        public void TheBonusMovesInStepsAndFollowsHealing()
        {
            SetHealth(85f);
            _systems.Run();

            Assert.AreEqual(0.06f, Modifiers()[StatId.WeaponDamage], 1e-5f, "15% missing is one 10% step");

            SetHealth(81f);
            _systems.Run();

            Assert.AreEqual(5, Modifiers().Count, "inside the same step nothing is re-added");
            Assert.AreEqual(0.06f, Modifiers()[StatId.WeaponDamage], 1e-5f);

            SetHealth(20f);
            _systems.Run();

            Assert.AreEqual(0.48f, Modifiers()[StatId.WeaponDamage], 1e-5f, "80% missing");

            SetHealth(100f);
            _systems.Run();

            Assert.AreEqual(0, Modifiers().Count, "healing back to full removes the bonus");
        }

        private void SetHealth(float value)
        {
            _world.GetPool<Health>().Get(_hero).Current = value;
        }

        private Dictionary<StatId, float> Modifiers()
        {
            Dictionary<StatId, float> result = new Dictionary<StatId, float>();

            foreach (int entity in _world.Filter<StatModifier>().End())
            {
                StatModifier modifier = _world.GetPool<StatModifier>().Get(entity);

                Assert.AreEqual(StatOp.More, modifier.Op);
                Assert.AreEqual(ApplyLowHealthFurySystem.ModifierSource, modifier.SourceId);
                Assert.IsFalse(result.ContainsKey(modifier.Stat), $"{modifier.Stat} carries two fury modifiers");

                result.Add(modifier.Stat, modifier.Value);
            }

            return result;
        }

        private T Own<T>(T asset) where T : Object
        {
            _assets.Add(asset);

            return asset;
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
