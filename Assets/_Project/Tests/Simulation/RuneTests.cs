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
    public sealed class RuneTests
    {
        private const float Tick = 1f / 60f;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private StatModifiers _modifiers;
        private RuneTableConfig _table;
        private int _hero;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void ARuneAppearsAfterTheDelayAndOnlyOneAtATime()
        {
            Build(Rune("rune.test.fury", 10f, StatId.DamageDealt, StatOp.More, 2f, 0f));

            Step(0.9f);
            Assert.AreEqual(0, RuneCount(), "not before the first delay");

            Step(0.3f);
            Assert.AreEqual(1, RuneCount(), "one rune after the first delay");

            Step(10f);
            Assert.AreEqual(1, RuneCount(), "no second rune while one lies on the arena");
        }

        [Test]
        public void PickingUpARuneAppliesItsBuffUntilItExpiresAndBlocksTheNextRune()
        {
            Build(Rune("rune.test.ward", 2f, StatId.DamageTaken, StatOp.More, -0.5f, 0f));

            Step(1.2f);
            MoveHeroTo(RunePosition());
            Step(Tick);

            Assert.AreEqual(0, RuneCount(), "the rune is picked up");
            Assert.IsTrue(_world.GetPool<ActiveRune>().Has(_hero));
            Assert.AreEqual(1, ModifierCount("rune.rune.test.ward"));

            Step(1.5f);
            Assert.AreEqual(0, RuneCount(), "no new rune while the effect lasts");

            Step(0.6f);
            Assert.IsFalse(_world.GetPool<ActiveRune>().Has(_hero), "the effect expires");
            Assert.AreEqual(0, ModifierCount("rune.rune.test.ward"), "and takes its modifiers away");
        }

        [Test]
        public void TheLifeRuneRestoresATenthOfHealthAndRageEveryHalfSecond()
        {
            Build(Rune("rune.test.life", 2f, StatId.MoveSpeed, StatOp.Flat, 0f, 0.1f));

            Step(1.2f);
            MoveHeroTo(RunePosition());
            Step(Tick);

            _world.GetPool<Health>().Get(_hero).Current = 10f;
            _world.GetPool<UltimateCharge>().Get(_hero).Value = 0f;

            Step(1.0f);

            Assert.AreEqual(30f, _world.GetPool<Health>().Get(_hero).Current, 0.5f, "two pulses of 10% of 100 health");
            Assert.AreEqual(20f, _world.GetPool<UltimateCharge>().Get(_hero).Value, 0.5f, "two pulses of 10% of 100 rage");
        }

        private RuneConfig Rune(string id, float duration, StatId stat, StatOp op, float value, float regen)
        {
            GameObject prefab = new GameObject("RunePrefab");
            _assets.Add(prefab);

            RuneConfig rune = ScriptableObject.CreateInstance<RuneConfig>();
            _assets.Add(rune);

            StatModifierEntry entry = new StatModifierEntry();
            SetField(entry, "_stat", stat);
            SetField(entry, "_op", op);
            SetField(entry, "_value", value);

            SetField(rune, "_id", id);
            SetField(rune, "_displayName", id);
            SetField(rune, "_viewPrefab", prefab);
            SetField(rune, "_durationSeconds", duration);
            SetField(rune, "_modifiers", regen > 0f ? Array.Empty<StatModifierEntry>() : new[] { entry });
            SetField(rune, "_regenFraction", regen);
            SetField(rune, "_regenIntervalSeconds", 0.5f);

            return rune;
        }

        private void Build(RuneConfig rune)
        {
            WeightedRune weighted = new WeightedRune();
            SetField(weighted, "_rune", rune);

            _table = ScriptableObject.CreateInstance<RuneTableConfig>();
            _assets.Add(_table);
            SetField(_table, "_id", "runes.test");
            SetField(_table, "_runes", new[] { weighted });
            SetField(_table, "_firstDelaySeconds", 1f);
            SetField(_table, "_minIntervalSeconds", 1f);
            SetField(_table, "_maxIntervalSeconds", 1f);
            SetField(_table, "_minDistanceFromHero", 5f);
            SetField(_table, "_maxDistanceFromHero", 5f);

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);
            SetField(timeline, "_runes", _table);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            _assets.Add(level);
            SetField(level, "_waves", timeline);

            _world = new EcsWorld();
            _clock = new SimulationClock();
            _modifiers = new StatModifiers(_world);

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = Vector3.zero;
            _world.GetPool<Health>().Add(_hero).Current = 100f;
            _world.GetPool<MaxHealth>().Add(_hero).Value = 100f;
            ref UltimateCharge charge = ref _world.GetPool<UltimateCharge>().Add(_hero);
            charge.Max = 100f;

            _systems = new EcsSystems(_world);
            _systems.Add(new SpawnRuneSystem());
            _systems.Add(new PickupRuneSystem());
            _systems.Add(new TickActiveRuneSystem());
            _systems.Add(new ApplyHealSystem());
            _systems.Add(new CleanupEventsSystem());
            _systems.Inject(
                level,
                _clock,
                _modifiers,
                (IViewFactory)new NullViewFactory(),
                new RunContext("level.test", "character.test", RunMode.Story, 987654321, Array.Empty<StatModifierSpec>()));
            _systems.Init();
        }

        private void Step(float seconds)
        {
            int ticks = Mathf.Max(1, Mathf.RoundToInt(seconds / Tick));

            for (int index = 0; index < ticks; index++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private int RuneCount()
        {
            return _world.Filter<Rune>().End().GetEntitiesCount();
        }

        private Vector3 RunePosition()
        {
            foreach (int rune in _world.Filter<Rune>().End())
                return _world.GetPool<Position>().Get(rune).Value;

            Assert.Fail("no rune on the arena");

            return Vector3.zero;
        }

        private void MoveHeroTo(Vector3 position)
        {
            _world.GetPool<Position>().Get(_hero).Value = position;
        }

        private int ModifierCount(string source)
        {
            int count = 0;

            foreach (int entity in _world.Filter<StatModifier>().End())
            {
                if (_world.GetPool<StatModifier>().Get(entity).SourceId == source)
                    count++;
            }

            return count;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

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
