using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class MeteorTests
    {
        private const float Tick = 1f / 60f;
        private const float CastSeconds = 0.6f;
        private const float FallSeconds = 0.7f;
        private const float CooldownSeconds = 10f;
        private const float Damage = 50f;

        private readonly List<Object> _assets = new List<Object>();
        private readonly SimulationClock _clock = new SimulationClock();

        private EcsWorld _world;
        private EcsSystems _systems;
        private RecordingViewFactory _views;
        private RecordingView _heroView;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            GameObject fx = new GameObject("Fx");
            _assets.Add(fx);

            MeteorConfig meteor = Asset<MeteorConfig>();
            MeteorLevel level = new MeteorLevel();
            Set(level, "_cooldownSeconds", CooldownSeconds);
            Set(level, "_damage", Damage);
            Set(meteor, "_levels", new[] { level });
            Set(meteor, "_range", 14f);
            Set(meteor, "_clusterRadius", 3f);
            Set(meteor, "_castSeconds", CastSeconds);
            Set(meteor, "_fallSeconds", FallSeconds);
            Set(meteor, "_meteorPrefab", fx);
            Set(meteor, "_impactPrefab", fx);

            PerkConfig perk = Asset<PerkConfig>();
            Set(perk, "_id", "perk.meteor");
            Set(perk, "_behaviour", meteor);
            Set(perk, "_levels", new[] { new PerkLevel() });

            _world = new EcsWorld();
            _views = new RecordingViewFactory();

            _systems = new EcsSystems(_world);
            _systems.Add(new MeteorSystem());
            _systems.Inject(_clock, (IViewFactory)_views, (IContentRegistry)new ContentRegistry(new IContentEntry[] { perk }));
            _systems.Init();

            _heroView = new RecordingView();
            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);
            _world.GetPool<View>().Add(_hero).Value = _heroView;

            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(_world.NewEntity());
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = perk.Id;
            taken.Level = 1;
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
        public void TheMeteorFallsOnTheDensestCrowdAfterATwoHandedCast()
        {
            Enemy(new Vector3(-6f, 0f, 0f));
            Enemy(new Vector3(6f, 0f, 6f));
            Enemy(new Vector3(6.5f, 0f, 6f));
            Enemy(new Vector3(6f, 0f, 7f));

            Step(1);

            CollectionAssert.AreEqual(new[] { "CastNova" }, _heroView.Triggers, "the two-handed cast plays first");
            Assert.AreEqual(0, Strikes().Count);

            Step(Mathf.RoundToInt((CastSeconds + FallSeconds) / Tick) + 1);

            List<AreaStrike> strikes = Strikes();
            Assert.AreEqual(1, strikes.Count);
            Assert.Less(Vector3.Distance(strikes[0].Origin, new Vector3(6f, 0f, 6f)), 1f, "on the crowd of three, not the lone enemy");
            Assert.AreEqual(Damage, strikes[0].Damage);
            Assert.IsTrue(strikes[0].IsMagic);
        }

        [Test]
        public void NoEnemyInRangeMeansNoMeteorAndTheCooldownGatesTheNext()
        {
            Enemy(new Vector3(0f, 0f, 20f));

            Step(30);

            Assert.AreEqual(0, _heroView.Triggers.Count, "out of range");

            Enemy(new Vector3(0f, 0f, 5f));

            Step(1);

            Assert.AreEqual(1, _heroView.Triggers.Count);

            Step(Mathf.RoundToInt(CooldownSeconds / Tick) - 1);

            Assert.AreEqual(1, _heroView.Triggers.Count, "still on cooldown");

            Step(2);

            Assert.AreEqual(2, _heroView.Triggers.Count);
        }

        private void Enemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
        }

        private List<AreaStrike> Strikes()
        {
            List<AreaStrike> strikes = new List<AreaStrike>();

            foreach (int entity in _world.Filter<AreaStrike>().End())
                strikes.Add(_world.GetPool<AreaStrike>().Get(entity));

            return strikes;
        }

        private void Step(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private T Asset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);

            return asset;
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = null;

            for (System.Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");
            field.SetValue(target, value);
        }
    }
}
