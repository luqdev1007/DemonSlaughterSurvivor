using Game.Configs;
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
    public sealed class FaceNearestEnemyTests
    {
        private const float Tick = 1f / 60f;
        private const int ReachBin = 4;
        private const float TurnSpeed = 1000f;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private int _hero;
        private int _weapon;

        private static float Reach => SwingCoverage.MinDistance + (ReachBin + 0.5f) * SwingCoverage.DistanceStep;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();

            _systems = null;
            _world = null;

            for (int index = 0; index < _assets.Count; index++)
                Object.DestroyImmediate(_assets[index]);

            _assets.Clear();
        }

        [Test]
        public void ReachIsTheFarthestCoveredDistanceBinEdge()
        {
            Assert.AreEqual(Reach, CreateCoverage().MaxCoveredDistance, 1e-5f);
            Assert.AreEqual(0f, new SwingCoverage().MaxCoveredDistance);
        }

        [Test]
        public void IdleHeroTurnsToTheNearestEnemyBehind()
        {
            Build();
            SpawnEnemy(new Vector3(0f, 0f, -1f));
            SpawnEnemy(new Vector3(1.4f, 0f, 0f));

            Run(30);

            AssertFacing(Vector3.back);
        }

        [Test]
        public void EnemyBeyondReachIsIgnored()
        {
            Build();
            SpawnEnemy(new Vector3(0f, 0f, -(Reach + 0.05f)));

            Run(30);

            AssertFacing(Vector3.forward);
        }

        [Test]
        public void MoveInputKeepsFacingToTheVelocitySystem()
        {
            Build();
            SpawnEnemy(new Vector3(0f, 0f, -1f));
            _world.GetPool<MoveIntent>().Get(_hero).Value = Vector3.right;
            _world.GetPool<Velocity>().Get(_hero).Value = Vector3.right * 5f;

            Run(30);

            AssertFacing(Vector3.right);
        }

        [Test]
        public void DashingHeroDoesNotTurnToEnemies()
        {
            Build();
            SpawnEnemy(new Vector3(0f, 0f, -1f));
            _world.GetPool<Dashing>().Add(_hero);

            Run(30);

            AssertFacing(Vector3.forward);
        }

        [Test]
        public void SwingingWeaponLocksFacing()
        {
            Build();
            SpawnEnemy(new Vector3(0f, 0f, -1f));
            _world.GetPool<Swinging>().Add(_weapon);

            Run(30);

            AssertFacing(Vector3.forward);
        }

        [Test]
        public void SpecialAttackLocksFacing()
        {
            Build();
            SpawnEnemy(new Vector3(0f, 0f, -1f));
            _world.GetPool<SpecialAttack>().Add(_hero);

            Run(30);

            AssertFacing(Vector3.forward);
        }

        [Test]
        public void TurnIsLimitedByTurnSpeed()
        {
            Build();
            SpawnEnemy(new Vector3(0f, 0f, -1f));

            Run(2);

            float turned = Vector3.Angle(Vector3.forward, _world.GetPool<Facing>().Get(_hero).Value);

            Assert.AreEqual(TurnSpeed * Tick, turned, 0.01f);
        }

        private void Build()
        {
            _world = new EcsWorld();
            _clock = new SimulationClock();

            _systems = new EcsSystems(_world);
            _systems.Add(new FaceVelocitySystem());
            _systems.Add(new FaceNearestEnemySystem());
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Inject(CreateLevel(), new SpatialGrid(_world, 45f, 1f), _clock, new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<MoveIntent>().Add(_hero);
            _world.GetPool<Velocity>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;
            _world.GetPool<TurnSpeed>().Add(_hero).Value = TurnSpeed;

            WeaponConfig config = ScriptableObject.CreateInstance<WeaponConfig>();
            config.OverwriteTriggerCoverage(CreateCoverage());
            _assets.Add(config);

            _weapon = _world.NewEntity();
            _world.GetPool<Weapon>().Add(_weapon).Config = config;
            _world.GetPool<OwnerLink>().Add(_weapon).Owner = _world.PackEntity(_hero);
        }

        private void Run(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private void SpawnEnemy(Vector3 position)
        {
            int entity = _world.NewEntity();

            _world.GetPool<Enemy>().Add(entity);
            _world.GetPool<Position>().Add(entity).Value = position;
        }

        private void AssertFacing(Vector3 expected)
        {
            Vector3 facing = _world.GetPool<Facing>().Get(_hero).Value;

            Assert.Less(Vector3.Angle(expected, facing), 0.01f, $"Facing {facing} instead of {expected}.");
        }

        private static SwingCoverage CreateCoverage()
        {
            bool[] cells = new bool[SwingCoverage.AngleBins * SwingCoverage.DistanceBins];

            for (int distance = 0; distance <= ReachBin; distance++)
                cells[(SwingCoverage.AngleBins / 2) * SwingCoverage.DistanceBins + distance] = true;

            SwingCoverage coverage = new SwingCoverage();
            coverage.Overwrite(cells);

            return coverage;
        }

        private LevelConfig CreateLevel()
        {
            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            return level;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (System.Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }
    }
}
