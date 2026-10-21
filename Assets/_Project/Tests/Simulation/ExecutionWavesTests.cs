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
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class ExecutionWavesTests
    {
        private const float Tick = 1f / 60f;
        private const float WeaponDamage = 20f;
        private const string PerkPath = "Assets/_Project/Configs/Perks/Perk_ExecutionWaves.asset";
        private const string SwordPath = "Assets/_Project/Configs/Weapons/Weapon_BerserkSword.asset";
        private const string LeapPath = "Assets/_Project/Configs/Perks/HeroicLeap_Berserk.asset";

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private RecordingViewFactory _views;
        private PerkConfig _perk;
        private ExecutionWavesConfig _config;
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
        public void ThreeWavesCoverTheConeInFrontAndHitEachEnemyOnce()
        {
            Build();

            int ahead = SpawnEnemy(Polar(0f, 4f));
            int left = SpawnEnemy(Polar(-40f, 6f));
            int edge = SpawnEnemy(Polar(55f, 3f));
            int outside = SpawnEnemy(Polar(80f, 3f));
            int behind = SpawnEnemy(Polar(180f, 2f));
            int beyond = SpawnEnemy(Polar(0f, _config.MaxRadius + 1.5f));

            FinishExecution();
            Run(Mathf.CeilToInt(_config.MaxRadius / _config.Speed / Tick) + 2);

            float damage = WeaponDamage * _config.Level(1).DamageShare;

            Assert.AreEqual(3, _views.Created, "one view per wave");
            Assert.AreEqual(3, _views.Released, "the waves are gone at the end of their range");
            Assert.AreEqual(damage, Total(ahead), 1e-4f);
            Assert.AreEqual(damage, Total(left), 1e-4f);
            Assert.AreEqual(damage, Total(edge), 1e-4f, "55 deg is inside the 120 deg cone");
            Assert.AreEqual(0f, Total(outside), "80 deg is outside the cone");
            Assert.AreEqual(0f, Total(behind));
            Assert.AreEqual(0f, Total(beyond));
        }

        [Test]
        public void AWavePushesTheEnemyAwayByMetres()
        {
            Build();

            int ahead = SpawnEnemy(Polar(0f, 3f));
            Vector3 start = Position(ahead);

            FinishExecution();
            Run(90);

            float pushed = Vector3.Distance(start, Position(ahead));
            TestContext.WriteLine($"wave push: {pushed:F2} m");

            Assert.Greater(Position(ahead).z, start.z, "pushed away from the hero");
            Assert.AreEqual(_config.PushSpeed * _config.PushSeconds * 0.5f, pushed, 0.2f);
        }

        [Test]
        public void NoWavesWithoutThePerk()
        {
            Build();
            _world.DelEntity(FindTaken());

            int ahead = SpawnEnemy(Polar(0f, 3f));

            FinishExecution();
            Run(60);

            Assert.AreEqual(0, _views.Created);
            Assert.AreEqual(0f, Total(ahead));
        }

        [Test]
        public void SwordAndLeapKnockbackTravelVisibleDistances()
        {
            WeaponConfig sword = AssetDatabase.LoadAssetAtPath<WeaponConfig>(SwordPath);
            HeroicLeapConfig leap = AssetDatabase.LoadAssetAtPath<HeroicLeapConfig>(LeapPath);

            float swordDistance = PushDistance(sword.HitPushSpeed, sword.HitPushSeconds);
            float leapDistance = PushDistance(leap.PushSpeed, leap.PushSeconds);

            TestContext.WriteLine($"sword hit push: {swordDistance:F2} m, heroic leap push: {leapDistance:F2} m");

            Assert.Greater(swordDistance, 0.6f, "a sword hit moves the enemy more than its own width");
            Assert.Less(swordDistance, 1.5f, "the innate knockback stays small");
            Assert.Greater(leapDistance, 3f);
        }

        private float PushDistance(float speed, float seconds)
        {
            EcsWorld world = new EcsWorld();
            SimulationClock clock = new SimulationClock();
            EcsSystems systems = new EcsSystems(world);
            systems.Add(new ApplyPushSystem());
            systems.Add(new MoveSystem());
            systems.Inject(clock);
            systems.Init();

            int enemy = world.NewEntity();
            world.GetPool<Position>().Add(enemy);
            world.GetPool<Velocity>().Add(enemy);
            PushFromPoint.Apply(world.GetPool<Pushed>(), enemy, Vector3.forward, speed, Mathf.Max(1, Mathf.RoundToInt(seconds / Tick)));

            for (int tick = 0; tick < 120; tick++)
            {
                clock.Advance(Tick);
                systems.Run();
            }

            float distance = world.GetPool<Position>().Get(enemy).Value.magnitude;

            systems.Destroy();
            world.Destroy();

            return distance;
        }

        private void Build()
        {
            _perk = AssetDatabase.LoadAssetAtPath<PerkConfig>(PerkPath);
            Assert.IsNotNull(_perk, $"{PerkPath} is missing.");
            _config = (ExecutionWavesConfig)_perk.Behaviour;

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            typeof(LevelConfig).GetField("_waves", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(level, timeline);
            _assets.Add(level);

            _world = new EcsWorld();
            _clock = new SimulationClock();
            _views = new RecordingViewFactory();
            SpatialGrid grid = new SpatialGrid(_world, 45f, 1f);

            _systems = new EcsSystems(_world);
            _systems.Add(new ExecutionWavesSystem());
            _systems.Add(new ApplyPushSystem());
            _systems.Add(new MoveSystem());
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Inject(level, grid, _clock, (IViewFactory)_views, (IContentRegistry)new ContentRegistry(new IContentEntry[] { _perk }),
                new EnemyMotionBounds(2.5f, 0.9f, _config.PushSpeed, 0.5f));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;

            int weapon = _world.NewEntity();
            _world.GetPool<Weapon>().Add(weapon);
            _world.GetPool<OwnerLink>().Add(weapon).Owner = _world.PackEntity(_hero);

            ref WeaponDamage damage = ref _world.GetPool<WeaponDamage>().Add(weapon);
            damage.Base = WeaponDamage;
            damage.Value = WeaponDamage;

            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(_world.NewEntity());
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = _perk.Id;
            taken.Level = 1;

            Run(1);
        }

        private void FinishExecution()
        {
            _world.GetPool<ExecutionFinishedEvent>().Add(_world.NewEntity()).Owner = _world.PackEntity(_hero);
            Run(1);

            foreach (int entity in _world.Filter<ExecutionFinishedEvent>().End())
                _world.DelEntity(entity);
        }

        private int FindTaken()
        {
            foreach (int entity in _world.Filter<TakenPerk>().End())
                return entity;

            return -1;
        }

        private int SpawnEnemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<Velocity>().Add(enemy);
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;
            _world.GetPool<Health>().Add(enemy).Current = 1000f;

            Run(1);

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

        private float Total(int target)
        {
            float total = 0f;
            EcsPool<DamageEvent> events = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                if (events.Get(entity).Target.Unpack(_world, out int unpacked) && unpacked == target)
                    total += events.Get(entity).Amount;
            }

            return total;
        }

        private Vector3 Position(int entity)
        {
            return _world.GetPool<Position>().Get(entity).Value;
        }

        private static Vector3 Polar(float degrees, float distance)
        {
            return Quaternion.AngleAxis(degrees, Vector3.up) * Vector3.forward * distance;
        }
    }
}
