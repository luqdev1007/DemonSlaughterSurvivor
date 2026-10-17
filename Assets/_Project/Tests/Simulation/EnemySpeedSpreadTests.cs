using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class EnemySpeedSpreadTests
    {
        private EcsWorld _world;
        private EnemyFactory _factory;
        private EnemyConfig _enemy;

        [SetUp]
        public void SetUp()
        {
            _world = new EcsWorld();
            _factory = new EnemyFactory(_world, new NullViewFactory(), new StatModifiers(_world));

            _enemy = ScriptableObject.CreateInstance<EnemyConfig>();
            SetField(_enemy, "_id", "enemy.spread.test");
            SetField(_enemy, "_moveSpeed", 2.5f);
        }

        [TearDown]
        public void TearDown()
        {
            _world.Destroy();
            Object.DestroyImmediate(_enemy);
        }

        [TestCase(0f, -0.1f)]
        [TestCase(0.5f, 0f)]
        [TestCase(1f, 0.1f)]
        public void TheRollMapsOntoTheSpreadAsAMoreModifier(float roll, float expected)
        {
            SetField(_enemy, "_moveSpeedSpread", 0.1f);

            int enemy = _factory.Create(_enemy, Vector3.zero, roll);

            StatModifier modifier = SingleModifier(enemy);

            Assert.AreEqual(StatId.MoveSpeed, modifier.Stat);
            Assert.AreEqual(StatOp.More, modifier.Op);
            Assert.AreEqual(EnemyFactory.SpawnModifierSource, modifier.SourceId);
            Assert.AreEqual(expected, modifier.Value, 1e-6f);
            Assert.AreEqual(2.5f, _world.GetPool<MoveSpeed>().Get(enemy).Base, "the base stays the config value");
        }

        [Test]
        public void WithoutASpreadNoModifierIsCreated()
        {
            _factory.Create(_enemy, Vector3.zero, 1f);

            Assert.AreEqual(0, _world.Filter<StatModifier>().End().GetEntitiesCount());
        }

        [Test]
        public void ThePeakSpeedCoversTheFastestRoll()
        {
            SetField(_enemy, "_moveSpeedSpread", 0.1f);

            Assert.AreEqual(2.75f, _enemy.PeakMoveSpeed, 1e-5f);
        }

        private StatModifier SingleModifier(int target)
        {
            EcsFilter filter = _world.Filter<StatModifier>().End();

            Assert.AreEqual(1, filter.GetEntitiesCount());

            foreach (int entity in filter)
            {
                StatModifier modifier = _world.GetPool<StatModifier>().Get(entity);

                Assert.IsTrue(modifier.Target.Unpack(_world, out int owner) && owner == target);

                return modifier;
            }

            return default;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }

        private sealed class NullViewFactory : IViewFactory
        {
            public IView Create(GameObject prefab, Vector3 position)
            {
                return null;
            }

            public void Release(IView view)
            {
            }

            public void Retire(IView view, float seconds, DeathBlow blow)
            {
            }
        }
    }
}
