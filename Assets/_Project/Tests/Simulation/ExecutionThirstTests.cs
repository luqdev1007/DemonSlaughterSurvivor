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
    public sealed class ExecutionThirstTests
    {
        private const string ExecutionPerkId = "perk.execution.test";
        private const string ThirstPerkId = "perk.execution-thirst.test";

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private ExecutionThirstConfig _thirst;
        private PerkConfig _thirstPerk;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            ExecutionConfig execution = Own(ScriptableObject.CreateInstance<ExecutionConfig>());

            _thirst = Own(ScriptableObject.CreateInstance<ExecutionThirstConfig>());
            ExecutionThirstLevel level = new ExecutionThirstLevel();
            SetField(level, "_damageReduction", 0.3f);
            SetField(_thirst, "_levels", new[] { level });

            PerkConfig executionPerk = Perk(ExecutionPerkId, execution);
            _thirstPerk = Perk(ThirstPerkId, _thirst);
            SetField(_thirstPerk, "_requires", new[] { executionPerk });

            _world = new EcsWorld();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);

            _systems = new EcsSystems(_world);
            _systems.Add(new ApplyExecutionThirstSystem());
            _systems.Inject((IContentRegistry)new ContentRegistry(new IContentEntry[] { executionPerk, _thirstPerk }), new StatModifiers(_world));
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
        public void TheExecutionLowersTheDamageTakenAndItsEndRestoresIt()
        {
            Take(ThirstPerkId);

            int execution = StartExecution();
            _systems.Run();

            StatModifier modifier = SingleModifier();

            Assert.AreEqual(StatId.DamageTaken, modifier.Stat);
            Assert.AreEqual(StatOp.More, modifier.Op);
            Assert.AreEqual(-0.3f, modifier.Value, 1e-6f);

            _systems.Run();

            Assert.AreEqual(1, Modifiers(), "the modifier is added once, not every tick");

            _world.DelEntity(execution);
            _systems.Run();

            Assert.AreEqual(0, Modifiers());
            Assert.IsFalse(_world.GetPool<ExecutionThirstActive>().Has(_hero));
        }

        [Test]
        public void WithoutThePerkTheExecutionChangesNothing()
        {
            StartExecution();
            _systems.Run();

            Assert.AreEqual(0, Modifiers());
        }

        [Test]
        public void DeathEndsTheReduction()
        {
            Take(ThirstPerkId);

            StartExecution();
            _systems.Run();

            _world.GetPool<Dead>().Add(_hero);
            _systems.Run();

            Assert.AreEqual(0, Modifiers());
        }

        [Test]
        public void ThePerkRefusesToExistWithoutAnExecutionParent()
        {
            SetField(_thirstPerk, "_requires", Array.Empty<PerkConfig>());

            Assert.Throws<InvalidOperationException>(() => _thirst.Validate(_thirstPerk));
        }

        private int StartExecution()
        {
            int entity = _world.NewEntity();
            _world.GetPool<Execution>().Add(entity).Owner = _world.PackEntity(_hero);

            return entity;
        }

        private void Take(string perkId)
        {
            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(_world.NewEntity());
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = perkId;
            taken.Level = 1;
        }

        private int Modifiers()
        {
            return _world.Filter<StatModifier>().End().GetEntitiesCount();
        }

        private StatModifier SingleModifier()
        {
            Assert.AreEqual(1, Modifiers());

            foreach (int entity in _world.Filter<StatModifier>().End())
                return _world.GetPool<StatModifier>().Get(entity);

            return default;
        }

        private PerkConfig Perk(string id, PerkBehaviourConfig behaviour)
        {
            PerkConfig perk = Own(ScriptableObject.CreateInstance<PerkConfig>());
            SetField(perk, "_id", id);
            SetField(perk, "_levels", new[] { new PerkLevel() });
            SetField(perk, "_behaviour", behaviour);

            return perk;
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
