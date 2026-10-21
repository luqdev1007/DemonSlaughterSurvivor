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
    public sealed class ChakramTests
    {
        private const float Tick = 1f / 60f;
        private const float BowDamage = 14f;
        private const string PerkPath = "Assets/_Project/Configs/Perks/Perk_Chakram.asset";

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private RecordingViewFactory _views;
        private ChakramConfig _config;
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
        public void TheChakramSpiralsOutAndCutsEveryoneOnItsPathOnce()
        {
            Build();

            int near = SpawnEnemy(OnSpiral(0.2f));
            int middle = SpawnEnemy(OnSpiral(0.55f));
            int far = SpawnEnemy(OnSpiral(0.95f));
            int offPath = SpawnEnemy(OnSpiral(0.5f) + OnSpiral(0.5f).normalized * 2.7f);

            float maxRadius = 0f;
            int lifeTicks = Mathf.RoundToInt(_config.LifeSeconds / Tick);

            for (int tick = 0; tick < lifeTicks + 2; tick++)
            {
                Run(1);

                foreach (int chakram in _world.Filter<Chakram>().Inc<Position>().End())
                    maxRadius = Mathf.Max(maxRadius, _world.GetPool<Position>().Get(chakram).Value.magnitude);
            }

            float hit = BowDamage * _config.Level(1).DamageScale;
            TestContext.WriteLine($"chakram reached {maxRadius:F2} m from the hero in {_config.LifeSeconds} s");

            Assert.AreEqual(_config.EndRadius, maxRadius, 0.2f);
            Assert.AreEqual(1, _views.Created - Bleeds(), "one chakram at level 1");
            Assert.AreEqual(hit, HitDamage(near), 1e-4f);
            Assert.AreEqual(hit, HitDamage(middle), 1e-4f);
            Assert.AreEqual(hit, HitDamage(far), 1e-4f);
            Assert.AreEqual(0f, HitDamage(offPath), "2.7 m off the path is not cut");
            Assert.IsTrue(_world.GetPool<Slowed>().Has(far));
            Assert.IsTrue(_world.GetPool<Bleeding>().Has(far));
            Assert.IsFalse(_world.GetPool<Slowed>().Has(offPath));
        }

        [Test]
        public void BleedingDealsItsDamageOverItsDurationAndStops()
        {
            Build();

            int enemy = SpawnEnemy(OnSpiral(0.1f));

            Run(Mathf.RoundToInt(_config.LifeSeconds / Tick) + Mathf.RoundToInt(_config.Level(1).BleedSeconds / Tick) + 30);

            float expected = BowDamage * _config.Level(1).BleedDamageScale * _config.Level(1).BleedSeconds;
            TestContext.WriteLine($"bleeding: {BleedDamage(enemy):F2} over {_config.Level(1).BleedSeconds} s");

            Assert.AreEqual(expected, BleedDamage(enemy), 0.01f);
            Assert.IsFalse(_world.GetPool<Bleeding>().Has(enemy));
        }

        [Test]
        public void TheNextChakramWaitsForTheCooldown()
        {
            Build();
            SpawnEnemy(new Vector3(0f, 0f, 5f));

            int cooldownTicks = Mathf.RoundToInt(_config.Level(1).CooldownSeconds / Tick);

            Run(cooldownTicks - 2);

            int thrown = _views.Created - Bleeds();

            Run(4);

            Assert.AreEqual(1, thrown);
            Assert.AreEqual(2, _views.Created - Bleeds());
        }

        private Vector3 OnSpiral(float progress)
        {
            float radius = Mathf.Lerp(_config.StartRadius, _config.EndRadius, progress);
            float angle = 360f * _config.Turns * progress * Mathf.Deg2Rad;

            return new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
        }

        private void Build()
        {
            PerkConfig perk = AssetDatabase.LoadAssetAtPath<PerkConfig>(PerkPath);
            Assert.IsNotNull(perk, $"{PerkPath} is missing.");
            _config = (ChakramConfig)perk.Behaviour;

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
            _systems.Add(new ChakramSystem());
            _systems.Add(new BleedingSystem());
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Inject(level, grid, _clock, (IViewFactory)_views, (IContentRegistry)new ContentRegistry(new IContentEntry[] { perk }),
                new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;

            int bow = _world.NewEntity();
            _world.GetPool<Bow>().Add(bow);
            _world.GetPool<OwnerLink>().Add(bow).Owner = _world.PackEntity(_hero);

            ref WeaponDamage damage = ref _world.GetPool<WeaponDamage>().Add(bow);
            damage.Base = BowDamage;
            damage.Value = BowDamage;

            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(_world.NewEntity());
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = perk.Id;
            taken.Level = 1;
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

        private int Bleeds()
        {
            return _world.Filter<StatusFx>().End().GetEntitiesCount();
        }

        private float HitDamage(int target)
        {
            return Sum(target, true);
        }

        private float BleedDamage(int target)
        {
            return Sum(target, false);
        }

        private float Sum(int target, bool slash)
        {
            float total = 0f;
            EcsPool<DamageEvent> events = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                DamageEvent damage = events.Get(entity);

                if (damage.IsSlash == slash && damage.Target.Unpack(_world, out int unpacked) && unpacked == target)
                    total += damage.Amount;
            }

            return total;
        }
    }
}
