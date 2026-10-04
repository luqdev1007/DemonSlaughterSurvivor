using Game.Configs;
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

namespace Game.Simulation.Tests
{
    public sealed class DamageRulesTests
    {
        private const float TickSeconds = 1f / 60f;

        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();

        private EcsWorld _world;
        private EcsSystems _systems;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;

            foreach (ScriptableObject asset in _assets)
                UnityEngine.Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void EveryEventOnAnEnemyIsApplied()
        {
            Build();

            int enemy = SpawnEnemy(100f);
            int first = AddEvent(enemy, 20f, Vector3.zero);
            int second = AddEvent(enemy, 30f, Vector3.zero);

            Run();

            Assert.AreEqual(50f, Health(enemy));
            Assert.AreEqual(20f, Applied(first));
            Assert.AreEqual(30f, Applied(second));
        }

        [Test]
        public void KillingBlowComesFromTheEventThatTakesHealthToZero()
        {
            Build();

            int enemy = SpawnEnemy(25f);
            AddEvent(enemy, 20f, new Vector3(1f, 0f, 0f));
            AddEvent(enemy, 10f, new Vector3(2f, 0f, 0f));

            Run();

            Assert.AreEqual(-5f, Health(enemy));
            Assert.AreEqual(new Vector3(2f, 0f, 0f), _world.GetPool<KillingBlow>().Get(enemy).SourcePosition);
        }

        [Test]
        public void EventsAfterTheKillingOneAreSkipped()
        {
            Build();

            int enemy = SpawnEnemy(10f);
            int killing = AddEvent(enemy, 50f, new Vector3(1f, 0f, 0f));
            int overkill = AddEvent(enemy, 30f, new Vector3(2f, 0f, 0f));

            Run();

            Assert.AreEqual(-40f, Health(enemy));
            Assert.IsTrue(_world.GetPool<DamageApplied>().Has(killing));
            Assert.IsFalse(_world.GetPool<DamageApplied>().Has(overkill), "an event after the killing one must not count as applied");
            Assert.AreEqual(new Vector3(1f, 0f, 0f), _world.GetPool<KillingBlow>().Get(enemy).SourcePosition);
        }

        [Test]
        public void HeroStillTakesOnlyTheStrongestEventOfTheTick()
        {
            Build();

            int hero = _world.NewEntity();
            _world.GetPool<Player>().Add(hero);
            _world.GetPool<Health>().Add(hero).Current = 100f;

            int weak = AddEvent(hero, 10f, Vector3.zero);
            int strong = AddEvent(hero, 30f, Vector3.zero);
            int middle = AddEvent(hero, 20f, Vector3.zero);

            Run();

            Assert.AreEqual(70f, Health(hero));
            Assert.IsFalse(_world.GetPool<DamageApplied>().Has(weak));
            Assert.AreEqual(30f, Applied(strong));
            Assert.IsFalse(_world.GetPool<DamageApplied>().Has(middle));
        }

        [Test]
        public void SeveralHitsOnOneEnemyMarkItDeadOnce()
        {
            Build();

            int enemy = SpawnEnemy(10f);
            AddEvent(enemy, 20f, Vector3.zero);
            AddEvent(enemy, 20f, Vector3.zero);

            Run();

            Assert.IsTrue(_world.GetPool<Dead>().Has(enemy));
            Assert.AreEqual(1, _world.Filter<DiedEvent>().End().GetEntitiesCount());
        }

        private void Build()
        {
            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            FeedbackConfig feedback = ScriptableObject.CreateInstance<FeedbackConfig>();
            _assets.Add(feedback);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            SetField(level, "_feedback", feedback);
            _assets.Add(level);

            SimulationClock clock = new SimulationClock();
            clock.Advance(TickSeconds);

            _world = new EcsWorld();
            _systems = new EcsSystems(_world);

            _systems.Add(new ApplyDamageSystem());
            _systems.Add(new MarkDeadSystem());
            _systems.Inject(level, clock);
            _systems.Init();
        }

        private void Run()
        {
            _systems.Run();
        }

        private int SpawnEnemy(float health)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Health>().Add(enemy).Current = health;

            return enemy;
        }

        private int AddEvent(int target, float amount, Vector3 sourcePosition)
        {
            int entity = _world.NewEntity();

            ref DamageEvent damageEvent = ref _world.GetPool<DamageEvent>().Add(entity);
            damageEvent.Target = _world.PackEntity(target);
            damageEvent.SourcePosition = sourcePosition;
            damageEvent.Amount = amount;
            damageEvent.Kind = DamageKind.Weapon;

            return entity;
        }

        private float Health(int entity)
        {
            return _world.GetPool<Health>().Get(entity).Current;
        }

        private float Applied(int eventEntity)
        {
            Assert.IsTrue(_world.GetPool<DamageApplied>().Has(eventEntity), "the event was not applied");

            return _world.GetPool<DamageApplied>().Get(eventEntity).Amount;
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
