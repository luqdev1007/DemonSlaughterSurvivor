using Game.Configs;
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
    public sealed class HeroicLeapTests
    {
        private const float Tick = 1f / 60f;
        private const string LeapPath = "Assets/_Project/Configs/Perks/HeroicLeap_Berserk.asset";
        private const float WeaponDamage = 20f;
        private const float Share = 1.5f;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private HeroicLeapConfig _config;
        private int _hero;
        private int _weapon;

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
        public void LandsInTheRunThatIsTheLandingTickAfterTheStart()
        {
            Build();
            int enemy = SpawnEnemy(new Vector3(0f, 0f, 1.5f));

            StartLeap();
            Run(_config.LandingTick);

            Assert.AreEqual(10, _config.LandingTick);
            Assert.AreEqual(0, CountEvents(enemy), "no strike before the landing tick");

            Run(1);

            Assert.AreEqual(1, CountEvents(enemy));
            Assert.IsTrue(_world.GetPool<Pushed>().Has(enemy));

            Run(3);

            Assert.AreEqual(1, CountEvents(enemy), "one landing, one strike");
        }

        [Test]
        public void StrikeHitsTheSectorInFrontOnly()
        {
            Build();

            int ahead = SpawnEnemy(Polar(0f, 2f));
            int inside = SpawnEnemy(Polar(55f, 2f));
            int outside = SpawnEnemy(Polar(70f, 2f));
            int behind = SpawnEnemy(Polar(180f, 2f));
            int edge = SpawnEnemy(Polar(-10f, 2.85f));
            int beyond = SpawnEnemy(Polar(0f, 3f));

            StartLeap();
            Run(_config.LandingTick + 1);

            Assert.AreEqual(1, CountEvents(ahead));
            Assert.AreEqual(1, CountEvents(inside));
            Assert.AreEqual(0, CountEvents(outside), "70 deg is outside the 60 deg half angle");
            Assert.AreEqual(0, CountEvents(behind));
            Assert.AreEqual(1, CountEvents(edge), "2.85 m is within 2.5 m + body radius 0.4");
            Assert.AreEqual(0, CountEvents(beyond), "3.0 m is beyond 2.5 m + body radius 0.4");
        }

        [Test]
        public void StrikeIsAShareOfTheWeaponMarkedAsPerk()
        {
            Build();
            int enemy = SpawnEnemy(new Vector3(0f, 0f, 1.5f));

            StartLeap();
            Run(_config.LandingTick + 1);

            DamageEvent damageEvent = FirstEvent(enemy);

            Assert.AreEqual(WeaponDamage * Share, damageEvent.Amount, 1e-4f);
            Assert.AreEqual(DamageKind.Perk, damageEvent.Kind);
            Assert.IsTrue(damageEvent.Source.Unpack(_world, out int source) && source == _hero);
            Assert.AreEqual(Vector3.zero, damageEvent.SourcePosition);
        }

        [Test]
        public void LandingPushesEveryoneWithinThePushRadiusAway()
        {
            Build();

            int behind = SpawnEnemy(Polar(180f, 1f));
            int side = SpawnEnemy(Polar(90f, 2.4f));
            int far = SpawnEnemy(Polar(90f, 2.6f));

            StartLeap();
            Run(_config.LandingTick + 1);

            EcsPool<Pushed> pushes = _world.GetPool<Pushed>();
            int ticks = Mathf.RoundToInt(_config.PushSeconds / Tick);

            Assert.IsTrue(pushes.Has(behind));
            Assert.AreEqual(new Vector3(0f, 0f, -_config.PushSpeed), Round(pushes.Get(behind).Velocity));
            Assert.AreEqual(ticks, pushes.Get(behind).TotalTicks);
            Assert.AreEqual(new Vector3(_config.PushSpeed, 0f, 0f), Round(pushes.Get(side).Velocity));
            Assert.IsFalse(pushes.Has(far), "2.6 m is outside the 2.5 m push radius");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DeathOrDashBeforeTheLandingCancelsIt(bool dead)
        {
            Build();
            int enemy = SpawnEnemy(new Vector3(0f, 0f, 1.5f));

            StartLeap();
            Run(5);

            if (dead)
                _world.GetPool<Dead>().Add(_hero);
            else
                _world.GetPool<Dashing>().Add(_hero);

            Run(1);

            Assert.AreEqual(0, _world.Filter<HeroicLeap>().End().GetEntitiesCount());

            Run(1);

            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero), "the marker goes on the next tick");

            Run(_config.LandingTick);

            Assert.AreEqual(0, CountEvents(enemy));
            Assert.IsFalse(_world.GetPool<Pushed>().Has(enemy));
        }

        [Test]
        public void LeapLastsTotalTicksAndTheMarkerGoesWithIt()
        {
            Build();

            StartLeap();
            Run(_config.TotalTicks - 1);

            Assert.AreEqual(1, _world.Filter<HeroicLeap>().End().GetEntitiesCount());

            Run(1);

            Assert.AreEqual(0, _world.Filter<HeroicLeap>().End().GetEntitiesCount());
            Assert.IsTrue(_world.GetPool<SpecialAttack>().Has(_hero), "the marker is dropped on the next tick, before weapons start");

            Run(1);

            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero));
        }

        private void StartLeap()
        {
            int entity = _world.NewEntity();

            ref HeroicLeap leap = ref _world.GetPool<HeroicLeap>().Add(entity);
            leap.Owner = _world.PackEntity(_hero);
            leap.Weapon = _world.PackEntity(_weapon);
            leap.Config = _config;
            leap.DamageScale = Share;

            ref SpecialAttack special = ref _world.GetPool<SpecialAttack>().Add(_hero);
            special.Attack = _world.PackEntity(entity);
            special.LocksMovement = _config.LocksMovement;
        }

        private void Build()
        {
            _config = AssetDatabase.LoadAssetAtPath<HeroicLeapConfig>(LeapPath);
            Assert.IsNotNull(_config, $"{LeapPath} is missing.");

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            _world = new EcsWorld();
            _clock = new SimulationClock();
            SpatialGrid grid = new SpatialGrid(_world, 45f, 1f);

            _systems = new EcsSystems(_world);
            _systems.Add(new ExpireSpecialAttackSystem());
            _systems.Add(new AdvanceHeroicLeapSystem());
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Inject(level, grid, _clock, new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;

            _weapon = _world.NewEntity();
            _world.GetPool<Weapon>().Add(_weapon);
            _world.GetPool<OwnerLink>().Add(_weapon).Owner = _world.PackEntity(_hero);

            ref WeaponDamage damage = ref _world.GetPool<WeaponDamage>().Add(_weapon);
            damage.Base = WeaponDamage;
            damage.Value = WeaponDamage;

            Run(1);
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

        private static Vector3 Polar(float degrees, float distance)
        {
            float radians = degrees * Mathf.Deg2Rad;

            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * distance;
        }

        private static Vector3 Round(Vector3 value)
        {
            return new Vector3(Mathf.Round(value.x * 1000f) / 1000f, 0f, Mathf.Round(value.z * 1000f) / 1000f);
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
