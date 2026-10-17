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
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class AreaStrikeTests
    {
        private const float Tick = 1f / 60f;
        private const float EnemyRadius = 0.4f;
        private const float BoundPushSpeed = 5f;

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
        public void SectorHitsOnlyEnemiesInsideTheArcAndReach()
        {
            Build();

            int ahead = SpawnEnemy(new Vector3(0f, 0f, 2f));
            int edge = SpawnEnemy(new Vector3(0f, 0f, 3f + EnemyRadius - 0.01f));
            int far = SpawnEnemy(new Vector3(0f, 0f, 3f + EnemyRadius + 0.05f));
            int side = SpawnEnemy(new Vector3(2f, 0f, 0.2f));
            int behind = SpawnEnemy(new Vector3(0f, 0f, -2f));

            AddStrike(Strike(3f, 45f, 10f));
            Step();

            HashSet<int> hit = DamagedTargets();

            Assert.IsTrue(hit.Contains(ahead));
            Assert.IsTrue(hit.Contains(edge), "body radius counts towards the reach");
            Assert.IsFalse(hit.Contains(far));
            Assert.IsFalse(hit.Contains(side));
            Assert.IsFalse(hit.Contains(behind));
            Assert.AreEqual(2, StrikeEvent().Hits);
        }

        [Test]
        public void FullCircleHitsBehindToo()
        {
            Build();

            int behind = SpawnEnemy(new Vector3(0f, 0f, -2f));
            int side = SpawnEnemy(new Vector3(2f, 0f, 0f));

            AddStrike(Strike(3f, 180f, 10f));
            Step();

            HashSet<int> hit = DamagedTargets();

            Assert.IsTrue(hit.Contains(behind));
            Assert.IsTrue(hit.Contains(side));
        }

        [Test]
        public void DelayWaitsTheGivenTicksAndFollowsTheOwner()
        {
            Build();

            int enemy = SpawnEnemy(new Vector3(5f, 0f, 2f));

            AreaStrike strike = Strike(2f, 180f, 10f);
            strike.DelayTicks = 3;
            strike.FollowOwner = true;
            AddStrike(strike);

            Step(3);

            Assert.AreEqual(0, DamagedTargets().Count);

            _world.GetPool<Position>().Get(_hero).Value = new Vector3(5f, 0f, 0f);
            Step();

            Assert.IsTrue(DamagedTargets().Contains(enemy));
            Assert.AreEqual(new Vector3(5f, 0f, 0f), StrikeEvent().Origin);
        }

        [Test]
        public void HitEnemiesArePushedAwayWithTheArc()
        {
            Build();

            int enemy = SpawnEnemy(new Vector3(1f, 0f, 1f));
            int boss = SpawnEnemy(new Vector3(-1f, 0f, 1f));
            _world.GetPool<NotPushable>().Add(boss);

            AreaStrike strike = Strike(3f, 180f, 0f);
            strike.PushSpeed = 4f;
            strike.PushSeconds = 0.5f;
            strike.PushArcHeight = 0.3f;
            AddStrike(strike);
            Step();

            ref Pushed pushed = ref _world.GetPool<Pushed>().Get(enemy);

            Assert.AreEqual(4f, pushed.Velocity.magnitude, 1e-4f);
            Assert.Greater(Vector3.Dot(pushed.Velocity.normalized, new Vector3(1f, 0f, 1f).normalized), 0.999f);
            Assert.AreEqual(30, pushed.TotalTicks);
            Assert.AreEqual(0.3f, pushed.ArcHeight);
            Assert.IsFalse(_world.GetPool<Pushed>().Has(boss));
            Assert.AreEqual(0, DamagedTargets().Count, "zero damage pushes without damage events");
        }

        [Test]
        public void DeadOwnerCancelsThePendingStrike()
        {
            Build();

            SpawnEnemy(new Vector3(0f, 0f, 1f));

            AreaStrike strike = Strike(3f, 180f, 10f);
            strike.DelayTicks = 2;
            AddStrike(strike);

            Step();
            _world.GetPool<Dead>().Add(_hero);
            Step(3);

            Assert.AreEqual(0, DamagedTargets().Count);
            Assert.AreEqual(0, _world.Filter<AreaStrike>().End().GetEntitiesCount());
        }

        [Test]
        public void PushFasterThanTheIndexBudgetThrows()
        {
            Build();

            AreaStrike strike = Strike(3f, 180f, 0f);
            strike.PushSpeed = BoundPushSpeed + 1f;
            strike.PushSeconds = 0.5f;
            AddStrike(strike);

            Assert.Throws<InvalidOperationException>(() => Step());
        }

        private void Build()
        {
            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            _world = new EcsWorld();
            _clock = new SimulationClock();
            SpatialGrid grid = new SpatialGrid(_world, 45f, 1f);

            _systems = new EcsSystems(_world);
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Add(new ResolveAreaStrikeSystem());
            _systems.Inject(level, grid, _clock, new EnemyMotionBounds(2.5f, 0.9f, BoundPushSpeed, 0.5f));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;
        }

        private AreaStrike Strike(float reach, float halfAngle, float damage)
        {
            AreaStrike strike = default;
            strike.Owner = _world.PackEntity(_hero);
            strike.Origin = Vector3.zero;
            strike.Forward = Vector3.forward;
            strike.Reach = reach;
            strike.HalfAngleDegrees = halfAngle;
            strike.Damage = damage;
            strike.Kind = DamageKind.Perk;

            return strike;
        }

        private void AddStrike(AreaStrike strike)
        {
            _world.GetPool<AreaStrike>().Add(_world.NewEntity()) = strike;
        }

        private int SpawnEnemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<Velocity>().Add(enemy);
            _world.GetPool<BodyRadius>().Add(enemy).Value = EnemyRadius;

            return enemy;
        }

        private void Step(int ticks = 1)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                ClearDamage();
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private void ClearDamage()
        {
            foreach (int entity in _world.Filter<DamageEvent>().End())
                _world.DelEntity(entity);

            foreach (int entity in _world.Filter<AreaStrikeEvent>().End())
                _world.DelEntity(entity);
        }

        private HashSet<int> DamagedTargets()
        {
            HashSet<int> targets = new HashSet<int>();
            EcsPool<DamageEvent> pool = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                if (pool.Get(entity).Target.Unpack(_world, out int target))
                    targets.Add(target);
            }

            return targets;
        }

        private AreaStrikeEvent StrikeEvent()
        {
            EcsPool<AreaStrikeEvent> pool = _world.GetPool<AreaStrikeEvent>();

            foreach (int entity in _world.Filter<AreaStrikeEvent>().End())
                return pool.Get(entity);

            Assert.Fail("No area strike event.");

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
