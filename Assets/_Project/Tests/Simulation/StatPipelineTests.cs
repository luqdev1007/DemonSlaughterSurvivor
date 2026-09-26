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
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class StatPipelineTests
    {
        private const float FastestEnemySpeed = 2.5f;
        private const float Tolerance = 1e-5f;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private StatModifiers _modifiers;

        [SetUp]
        public void SetUp()
        {
            _world = new EcsWorld();
            _modifiers = new StatModifiers(_world);

            _systems = new EcsSystems(_world);
            _systems.Add(new RecomputeStatsSystem());
            _systems.Add(new SweepOrphanModifiersSystem());
            _systems.Inject(CreateLevel(FastestEnemySpeed));
            _systems.Init();
        }

        [TearDown]
        public void TearDown()
        {
            _systems.Destroy();
            _world.Destroy();

            for (int index = 0; index < _assets.Count; index++)
                Object.DestroyImmediate(_assets[index]);

            _assets.Clear();
        }

        [Test]
        public void RemovingOneSourceKeepsTheOther()
        {
            int hero = CreateHero(5f, 100f);

            _modifiers.Add(hero, StatId.MoveSpeed, StatOp.Increased, 0.2f, "upgrade.speed");
            _modifiers.Add(hero, StatId.MoveSpeed, StatOp.Flat, 1f, "meta.speed");
            _systems.Run();

            Assert.AreEqual(7.2f, Speed(hero), Tolerance);

            Assert.AreEqual(1, _modifiers.RemoveBySource(hero, "upgrade.speed"));
            _systems.Run();

            Assert.AreEqual(6f, Speed(hero), Tolerance);
            Assert.AreEqual(1, ModifierCount());
        }

        [Test]
        public void ResultDoesNotDependOnInsertionOrderBitForBit()
        {
            float[] firstOrder = { 0.13f, 0.76f, 0.69f, 0.24f };
            float[] secondOrder = { 0.76f, 0.69f, 0.13f, 0.24f };

            Assert.AreNotEqual(
                BitConverter.SingleToInt32Bits(5f * (1f + NaiveSum(firstOrder))),
                BitConverter.SingleToInt32Bits(5f * (1f + NaiveSum(secondOrder))),
                "These two orders must give different naive final speeds, otherwise this test proves nothing.");

            int first = CreateHero(5f, 100f);
            int second = CreateHero(5f, 100f);

            for (int index = 0; index < firstOrder.Length; index++)
                _modifiers.Add(first, StatId.MoveSpeed, StatOp.Increased, firstOrder[index], SourceFor(firstOrder[index]));

            for (int index = 0; index < secondOrder.Length; index++)
                _modifiers.Add(second, StatId.MoveSpeed, StatOp.Increased, secondOrder[index], SourceFor(secondOrder[index]));

            _systems.Run();

            Assert.AreEqual(BitConverter.SingleToInt32Bits(Speed(first)), BitConverter.SingleToInt32Bits(Speed(second)));
        }

        [Test]
        public void AddingTheSameSourceTwiceReplacesInsteadOfStacking()
        {
            int hero = CreateHero(10f, 100f);

            _modifiers.Add(hero, StatId.MoveSpeed, StatOp.Increased, 0.2f, "upgrade.speed#1");
            _modifiers.Add(hero, StatId.MoveSpeed, StatOp.Increased, 0.2f, "upgrade.speed#1");
            _systems.Run();

            Assert.AreEqual(12f, Speed(hero), Tolerance);
            Assert.AreEqual(1, ModifierCount());

            _modifiers.Add(hero, StatId.MoveSpeed, StatOp.Increased, 0.2f, "upgrade.speed#2");
            _systems.Run();

            Assert.AreEqual(14f, Speed(hero), Tolerance);
        }

        [Test]
        public void EnemyFasterThanTheTimelineThrowsWithAnAddress()
        {
            int enemy = CreateEnemy(FastestEnemySpeed);

            _modifiers.Add(enemy, StatId.MoveSpeed, StatOp.Increased, 0.1f, "test.haste");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => _systems.Run());

            StringAssert.Contains($"Enemy entity {enemy}", exception.Message);
            StringAssert.Contains("2.75", exception.Message);
            StringAssert.Contains("fastest enemy speed 2.5", exception.Message);
            StringAssert.Contains("'test.haste'", exception.Message);
        }

        [Test]
        public void SlowingAnEnemyPassesTheGuard()
        {
            int enemy = CreateEnemy(FastestEnemySpeed);

            _modifiers.Add(enemy, StatId.MoveSpeed, StatOp.Increased, -0.5f, "test.slow");

            Assert.DoesNotThrow(() => _systems.Run());
            Assert.AreEqual(1.25f, Speed(enemy), Tolerance);
        }

        [Test]
        public void ModifierOfADeadTargetIsSwept()
        {
            int enemy = CreateEnemy(FastestEnemySpeed);

            _modifiers.Add(enemy, StatId.MaxHealth, StatOp.More, 2f, "elite.devil");
            _systems.Run();

            Assert.AreEqual(1, ModifierCount());

            _world.DelEntity(enemy);
            _systems.Run();

            Assert.AreEqual(0, ModifierCount());
        }

        [Test]
        public void StartModifierIsInPlaceAfterTheFirstTick()
        {
            int hero = CreateHero(5f, 100f);

            _modifiers.Add(hero, StatId.MaxHealth, StatOp.Flat, 10f, "meta.general.vitality");
            _systems.Run();

            Assert.AreEqual(110f, _world.GetPool<MaxHealth>().Get(hero).Value, Tolerance);
            Assert.AreEqual(110f, _world.GetPool<Health>().Get(hero).Current, Tolerance);
        }

        [Test]
        public void ModifierAddedLaterInTheTickLandsOnTheNextTick()
        {
            int hero = CreateHero(10f, 100f);
            _systems.Run();

            _modifiers.Add(hero, StatId.MoveSpeed, StatOp.More, 0.3f, "ability.rage.berserk");

            Assert.AreEqual(10f, Speed(hero), Tolerance);

            _systems.Run();

            Assert.AreEqual(13f, Speed(hero), Tolerance);

            _modifiers.Add(hero, StatId.MoveSpeed, StatOp.More, 0.1f, "ability.rage.berserk");
            _systems.Run();

            Assert.AreEqual(11f, Speed(hero), Tolerance);
        }

        [Test]
        public void MaxHealthGrowsByDeltaAndShrinksByClamping()
        {
            int hero = CreateHero(5f, 100f);
            ref Health health = ref _world.GetPool<Health>().Get(hero);
            health.Current = 50f;

            _modifiers.Add(hero, StatId.MaxHealth, StatOp.Flat, 10f, "meta.vitality");
            _systems.Run();

            Assert.AreEqual(60f, _world.GetPool<Health>().Get(hero).Current, Tolerance);
            Assert.AreEqual(110f, _world.GetPool<MaxHealth>().Get(hero).Value, Tolerance);

            _world.GetPool<Health>().Get(hero).Current = 105f;

            _modifiers.RemoveBySource(hero, "meta.vitality");
            _systems.Run();

            Assert.AreEqual(100f, _world.GetPool<Health>().Get(hero).Current, Tolerance);
            Assert.AreEqual(100f, _world.GetPool<MaxHealth>().Get(hero).Value, Tolerance);
        }

        [Test]
        public void DeadTargetIsNotHealedByGrowingMaxHealth()
        {
            int hero = CreateHero(5f, 100f);
            _world.GetPool<Health>().Get(hero).Current = 0f;
            _world.GetPool<Dead>().Add(hero);

            _modifiers.Add(hero, StatId.MaxHealth, StatOp.Flat, 10f, "meta.vitality");
            _systems.Run();

            Assert.AreEqual(0f, _world.GetPool<Health>().Get(hero).Current);
        }

        [Test]
        public void OwnedEntityInheritsOwnerModifiersForTheStatsItCarries()
        {
            int hero = CreateHero(5f, 100f);
            int owned = _world.NewEntity();

            ref MaxHealth ownedMax = ref _world.GetPool<MaxHealth>().Add(owned);
            ownedMax.Base = 20f;
            ownedMax.Value = 20f;

            ref OwnerLink link = ref _world.GetPool<OwnerLink>().Add(owned);
            link.Owner = _world.PackEntity(hero);

            _modifiers.Add(hero, StatId.MaxHealth, StatOp.Flat, 10f, "meta.vitality");
            _modifiers.Add(hero, StatId.MoveSpeed, StatOp.Flat, 1f, "meta.speed");
            _systems.Run();

            Assert.AreEqual(30f, _world.GetPool<MaxHealth>().Get(owned).Value, Tolerance);
            Assert.IsFalse(_world.GetPool<MoveSpeed>().Has(owned));

            _modifiers.Add(hero, StatId.MaxHealth, StatOp.Flat, 20f, "meta.vitality");
            _systems.Run();

            Assert.AreEqual(40f, _world.GetPool<MaxHealth>().Get(owned).Value, Tolerance);
        }

        [Test]
        public void TargetWithoutModifiersKeepsItsBaseBitForBit()
        {
            int hero = CreateHero(5f, 100f);
            int enemy = CreateEnemy(FastestEnemySpeed);

            _modifiers.MarkDirty(hero);
            _modifiers.MarkDirty(enemy);
            _systems.Run();

            Assert.AreEqual(BitConverter.SingleToInt32Bits(5f), BitConverter.SingleToInt32Bits(Speed(hero)));
            Assert.AreEqual(BitConverter.SingleToInt32Bits(FastestEnemySpeed), BitConverter.SingleToInt32Bits(Speed(enemy)));
            Assert.AreEqual(BitConverter.SingleToInt32Bits(10f), BitConverter.SingleToInt32Bits(_world.GetPool<ContactDamage>().Get(enemy).Value));
        }

        [Test]
        public void RepeatedRecomputeDoesNotAllocate()
        {
            int hero = CreateHero(5f, 100f);

            for (int index = 0; index < 8; index++)
                _modifiers.Add(hero, StatId.MoveSpeed, StatOp.Increased, 0.01f * index, "source." + index);

            _systems.Run();

            const int cycles = 20000;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            int collectionsBefore = GC.CollectionCount(0);
            long before = Profiler.GetMonoUsedSizeLong();

            for (int cycle = 0; cycle < cycles; cycle++)
            {
                _modifiers.MarkDirty(hero);
                _systems.Run();
            }

            long growth = Profiler.GetMonoUsedSizeLong() - before;
            int collections = GC.CollectionCount(0) - collectionsBefore;

            Assert.AreEqual(0, collections);
            Assert.Less(growth, cycles * 8L);
        }

        private int CreateHero(float moveSpeed, float maxHealth)
        {
            int entity = _world.NewEntity();

            _world.GetPool<Player>().Add(entity);

            ref MoveSpeed speed = ref _world.GetPool<MoveSpeed>().Add(entity);
            speed.Base = moveSpeed;
            speed.Value = moveSpeed;

            ref MaxHealth max = ref _world.GetPool<MaxHealth>().Add(entity);
            max.Base = maxHealth;
            max.Value = maxHealth;

            ref Health health = ref _world.GetPool<Health>().Add(entity);
            health.Current = maxHealth;

            return entity;
        }

        private int CreateEnemy(float moveSpeed)
        {
            int entity = _world.NewEntity();

            _world.GetPool<Enemy>().Add(entity);

            ref Position position = ref _world.GetPool<Position>().Add(entity);
            position.Value = new Vector3(3f, 0f, 4f);

            ref MoveSpeed speed = ref _world.GetPool<MoveSpeed>().Add(entity);
            speed.Base = moveSpeed;
            speed.Value = moveSpeed;

            ref ContactDamage damage = ref _world.GetPool<ContactDamage>().Add(entity);
            damage.Base = 10f;
            damage.Value = 10f;

            ref MaxHealth max = ref _world.GetPool<MaxHealth>().Add(entity);
            max.Base = 20f;
            max.Value = 20f;

            ref Health health = ref _world.GetPool<Health>().Add(entity);
            health.Current = 20f;

            return entity;
        }

        private float Speed(int entity)
        {
            return _world.GetPool<MoveSpeed>().Get(entity).Value;
        }

        private int ModifierCount()
        {
            return _world.Filter<StatModifier>().End().GetEntitiesCount();
        }

        private static string SourceFor(float value)
        {
            return "source." + value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static float NaiveSum(float[] values)
        {
            float sum = 0f;

            for (int index = 0; index < values.Length; index++)
                sum += values[index];

            return sum;
        }

        private LevelConfig CreateLevel(float fastestEnemySpeed)
        {
            EnemyConfig enemy = ScriptableObject.CreateInstance<EnemyConfig>();
            SetField(enemy, "_moveSpeed", fastestEnemySpeed);
            _assets.Add(enemy);

            WeightedEnemy weighted = new WeightedEnemy();
            SetField(weighted, "_enemy", enemy);

            Wave wave = new Wave();
            SetField(wave, "_enemies", new[] { weighted });

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            SetField(timeline, "_waves", new[] { wave });
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            Assert.AreEqual(fastestEnemySpeed, WaveTimelineValidator.ResolveMaxMoveSpeed(level.Waves));

            return level;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }
    }
}
