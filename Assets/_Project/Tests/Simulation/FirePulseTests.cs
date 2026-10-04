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
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class FirePulseTests
    {
        private const float Tick = 1f / 60f;
        private const float WeaponDamage = 20f;
        private const int PulseTicks = 30;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private int _hero;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void OneSegmentHitsAnEnemyOncePerPulse()
        {
            Build();
            int enemy = SpawnEnemy(new Vector3(0.5f, 0f, 0f));
            int segment = SpawnSegment(Vector3.zero, 0.25f);

            Run(PulseTicks - 1);
            Assert.AreEqual(0, CountEvents(enemy), "no hit before the first pulse");

            Run(1);
            Assert.AreEqual(1, CountEvents(enemy));

            Run(PulseTicks);
            Assert.AreEqual(2, CountEvents(enemy), "one hit per 0.5 s");

            DamageEvent damageEvent = FirstEvent(enemy);

            Assert.AreEqual(WeaponDamage * 0.25f, damageEvent.Amount, 1e-5f);
            Assert.AreEqual(DamageKind.Perk, damageEvent.Kind);
            Assert.IsTrue(damageEvent.Source.Unpack(_world, out int source) && source == _hero);
            Assert.AreEqual(_world.GetPool<Position>().Get(segment).Value, damageEvent.SourcePosition);
        }

        [Test]
        public void AnEnemyOnTwoSegmentsIsHitOncePerPulse()
        {
            Build();
            int enemy = SpawnEnemy(new Vector3(0.3f, 0f, 0f));
            SpawnSegment(Vector3.zero, 0.75f);
            SpawnSegment(new Vector3(0.6f, 0f, 0f), 0.75f);

            Run(PulseTicks);

            Assert.AreEqual(1, CountEvents(enemy));
            Assert.AreEqual(WeaponDamage * 0.75f, FirstEvent(enemy).Amount, 1e-5f);
        }

        [Test]
        public void EveryEnemyWithinRadiusPlusBodyIsHitAndOthersAreNot()
        {
            Build();
            SpawnSegment(Vector3.zero, 0.5f);

            int near = SpawnEnemy(new Vector3(0.9f, 0f, 0f));
            int side = SpawnEnemy(new Vector3(0f, 0f, -0.99f));
            int far = SpawnEnemy(new Vector3(1.01f, 0f, 0f));
            int dead = SpawnEnemy(new Vector3(0.2f, 0f, 0f));
            _world.GetPool<Dead>().Add(dead);

            Run(PulseTicks);

            Assert.AreEqual(1, CountEvents(near), "0.9 m is within 0.6 m + body 0.4");
            Assert.AreEqual(1, CountEvents(side));
            Assert.AreEqual(0, CountEvents(far), "1.01 m is beyond 0.6 m + body 0.4");
            Assert.AreEqual(0, CountEvents(dead), "a dead enemy takes no pulse");
        }

        private void Build()
        {
            FlamingDashConfig config = AssetDatabase.LoadAssetAtPath<FlamingDashConfig>("Assets/_Project/Configs/Perks/FlamingDash_Berserk.asset");

            PerkConfig perk = ScriptableObject.CreateInstance<PerkConfig>();
            _assets.Add(perk);
            SetField(perk, "_id", "perk.flaming-dash.test");
            SetField(perk, "_behaviour", config);

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            _world = new EcsWorld();
            _clock = new SimulationClock();

            _systems = new EcsSystems(_world);
            _systems.Add(new PulseFireSegmentsSystem());
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Inject(level, new SpatialGrid(_world, 45f, 1f), _clock, new ContentRegistry(new IContentEntry[] { perk }), new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = new Vector3(20f, 0f, 20f);

            int weapon = _world.NewEntity();
            _world.GetPool<Weapon>().Add(weapon);
            _world.GetPool<OwnerLink>().Add(weapon).Owner = _world.PackEntity(_hero);

            ref WeaponDamage damage = ref _world.GetPool<WeaponDamage>().Add(weapon);
            damage.Base = WeaponDamage;
            damage.Value = WeaponDamage;
        }

        private int SpawnSegment(Vector3 position, float share)
        {
            int entity = _world.NewEntity();

            ref FireSegment segment = ref _world.GetPool<FireSegment>().Add(entity);
            segment.Owner = _world.PackEntity(_hero);
            segment.RemainingTicks = 10000;
            segment.DamageScale = share;

            _world.GetPool<Position>().Add(entity).Value = position;

            return entity;
        }

        private int SpawnEnemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;
            _world.GetPool<Health>().Add(enemy).Current = 1000f;

            return enemy;
        }

        private void Run(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private int CountEvents(int target)
        {
            int count = 0;
            EcsPool<DamageEvent> events = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                if (events.Get(entity).Target.Unpack(_world, out int unpacked) && unpacked == target)
                    count++;
            }

            return count;
        }

        private DamageEvent FirstEvent(int target)
        {
            EcsPool<DamageEvent> events = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                if (events.Get(entity).Target.Unpack(_world, out int unpacked) && unpacked == target)
                    return events.Get(entity);
            }

            Assert.Fail("no damage event reached the target");

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
    }
}
