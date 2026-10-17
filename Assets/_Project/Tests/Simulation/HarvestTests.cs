using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
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
    public sealed class HarvestTests
    {
        private const string PerkId = "perk.harvest.test";

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            HarvestConfig harvest = Own(ScriptableObject.CreateInstance<HarvestConfig>());
            HarvestLevel level = new HarvestLevel();
            SetField(level, "_healPerTarget", 0.01f);
            SetField(level, "_bonusPerExtraTarget", 0.5f);
            SetField(harvest, "_levels", new[] { level });

            PerkConfig perk = Own(ScriptableObject.CreateInstance<PerkConfig>());
            SetField(perk, "_id", PerkId);
            SetField(perk, "_levels", new[] { new PerkLevel() });
            SetField(perk, "_behaviour", harvest);

            _world = new EcsWorld();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<MaxHealth>().Add(_hero).Value = 200f;

            _systems = new EcsSystems(_world);
            _systems.Add(new GrantHarvestHealSystem());
            _systems.Inject((IContentRegistry)new ContentRegistry(new IContentEntry[] { perk }));
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
        public void TheHealGrowsFasterThanTheTargetCount()
        {
            Take();

            EndSwing(2);
            _systems.Run();

            Assert.AreEqual(0.01f * 2f * 1.5f * 200f, HealTotal(), 1e-4f, "two targets: 1% each, +50% for the second");

            ClearHeals();
            EndSwing(4);
            _systems.Run();

            Assert.AreEqual(0.01f * 4f * 2.5f * 200f, HealTotal(), 1e-4f, "four targets: 1% each, +150%");
        }

        [Test]
        public void ASingleTargetOrNoPerkHealsNothing()
        {
            EndSwing(5);
            _systems.Run();

            Assert.AreEqual(0f, HealTotal(), "without the perk");

            Take();
            ClearHeals();
            EndSwing(1);
            _systems.Run();

            Assert.AreEqual(0f, HealTotal(), "one target is not a harvest");
        }

        private void Take()
        {
            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(_world.NewEntity());
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = PerkId;
            taken.Level = 1;
        }

        private void EndSwing(int enemyHits)
        {
            foreach (int entity in _world.Filter<SwingEndedEvent>().End())
                _world.DelEntity(entity);

            ref SwingEndedEvent ended = ref _world.GetPool<SwingEndedEvent>().Add(_world.NewEntity());
            ended.Owner = _world.PackEntity(_hero);
            ended.EnemyHits = enemyHits;
        }

        private void ClearHeals()
        {
            foreach (int entity in _world.Filter<HealEvent>().End())
                _world.DelEntity(entity);
        }

        private float HealTotal()
        {
            float total = 0f;

            foreach (int entity in _world.Filter<HealEvent>().End())
                total += _world.GetPool<HealEvent>().Get(entity).Amount;

            return total;
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
