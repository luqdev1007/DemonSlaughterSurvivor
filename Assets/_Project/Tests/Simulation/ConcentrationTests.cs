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
    public sealed class ConcentrationTests
    {
        private const float Tick = 1f / 60f;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private ConcentrationConfig _config;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ConcentrationConfig>();
            Set(_config, "_regenPerSecond", 6f);
            Set(_config, "_perEliteKill", 25f);
            _assets.Add(_config);

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            Set(level, "_waves", timeline);
            _assets.Add(level);

            RunContext context = new RunContext("level.test", "character.test", RunMode.Story, 1, Array.Empty<StatModifierSpec>());

            _world = new EcsWorld();
            _clock = new SimulationClock();
            _systems = new EcsSystems(_world);
            _systems.Add(new ResolveHitOutcomeSystem());
            _systems.Add(new ApplyDamageSystem());
            _systems.Add(new MarkDeadSystem());
            _systems.Add(new RestoreConcentrationSystem());
            _systems.Add(new CleanupDamageForTests());
            _systems.Inject(context, level, _clock);

            FeedbackConfig feedback = ScriptableObject.CreateInstance<FeedbackConfig>();
            _assets.Add(feedback);
            Set(level, "_feedback", feedback);

            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Health>().Add(_hero).Current = 100f;

            ref Concentration concentration = ref _world.GetPool<Concentration>().Add(_hero);
            concentration.Config = _config;
            concentration.Value = 100f;
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
        public void TheShieldTakesHalfTheHitAndPaysForItWithConcentration()
        {
            Hit(_hero, 20f, false, -1);
            Step(1);

            Assert.AreEqual(90f, _world.GetPool<Health>().Get(_hero).Current, 1e-3f);
            Assert.AreEqual(90f + 6f * Tick, _world.GetPool<Concentration>().Get(_hero).Value, 1e-3f);
        }

        [Test]
        public void AnEmptyShieldLetsTheWholeHitThrough()
        {
            _world.GetPool<Concentration>().Get(_hero).Value = 4f;

            Hit(_hero, 20f, false, -1);
            Step(1);

            Assert.AreEqual(84f, _world.GetPool<Health>().Get(_hero).Current, 1e-3f, "4 absorbed, the rest goes through");
            Assert.AreEqual(6f * Tick, _world.GetPool<Concentration>().Get(_hero).Value, 1e-3f);
        }

        [Test]
        public void ADrainedShieldStaysDownUntilFivePoints()
        {
            _world.GetPool<Concentration>().Get(_hero).Value = 4f;

            Hit(_hero, 20f, false, -1);
            Step(1);

            Assert.IsTrue(_world.GetPool<Concentration>().Get(_hero).ShieldDown, "emptied by the hit");

            _world.GetPool<Concentration>().Get(_hero).Value = 4.5f;
            Hit(_hero, 10f, false, -1);
            Step(1);

            Assert.AreEqual(84f - 10f, _world.GetPool<Health>().Get(_hero).Current, 1e-3f, "below 5 nothing is absorbed");

            _world.GetPool<Concentration>().Get(_hero).Value = 5f;
            Step(1);

            Assert.IsFalse(_world.GetPool<Concentration>().Get(_hero).ShieldDown, "back up from 5");
        }

        [Test]
        public void KillingAnEliteRestoresConcentration()
        {
            _world.GetPool<Concentration>().Get(_hero).Value = 10f;

            int elite = Enemy(10f);
            _world.GetPool<Elite>().Add(elite);
            int plain = Enemy(10f);

            Hit(elite, 50f, false, _hero);
            Hit(plain, 50f, false, _hero);
            Step(1);

            Assert.AreEqual(35f + 6f * Tick, _world.GetPool<Concentration>().Get(_hero).Value, 1e-3f);
        }

        [Test]
        public void FullConcentrationEmpowersSpellsOnly()
        {
            int magicTarget = Enemy(1000f);
            int weaponTarget = Enemy(1000f);

            Hit(magicTarget, 10f, true, _hero);
            Hit(weaponTarget, 10f, false, _hero);
            Step(1);

            Assert.AreEqual(1000f - 10f * (1f + _config.DamageBonus), _world.GetPool<Health>().Get(magicTarget).Current, 1e-3f);
            Assert.AreEqual(990f, _world.GetPool<Health>().Get(weaponTarget).Current, 1e-3f);
        }

        private int Enemy(float health)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Health>().Add(enemy).Current = health;

            return enemy;
        }

        private void Hit(int target, float amount, bool magic, int source)
        {
            ref DamageEvent damage = ref _world.GetPool<DamageEvent>().Add(_world.NewEntity());
            damage.Target = _world.PackEntity(target);
            damage.Source = source >= 0 ? _world.PackEntity(source) : default;
            damage.Amount = amount;
            damage.IsMagic = magic;
            damage.IsCrit = true;
        }

        private void Step(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");
            field.SetValue(target, value);
        }

        private sealed class CleanupDamageForTests : IEcsRunSystem
        {
            public void Run(IEcsSystems systems)
            {
                EcsWorld world = systems.GetWorld();
                EcsFilter events = world.Filter<DamageEvent>().End();
                int[] entities = events.GetRawEntities();
                int count = events.GetEntitiesCount();
                int[] copy = new int[count];
                Array.Copy(entities, copy, count);

                foreach (int entity in copy)
                    world.DelEntity(entity);
            }
        }
    }
}
