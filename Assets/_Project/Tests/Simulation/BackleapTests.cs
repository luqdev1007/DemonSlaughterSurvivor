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
    public sealed class BackleapTests
    {
        private const float Tick = 1f / 60f;
        private const string HeroId = "character.test";
        private const int LeapTicks = 18;
        private const int HopTicks = 21;
        private const int DiveTicks = 9;
        private const int FallTicks = 15;
        private const int HoverTicks = 72;
        private const float Clearance = 1f;

        private readonly List<Object> _assets = new List<Object>();
        private readonly SimulationClock _clock = new SimulationClock();

        private EcsWorld _world;
        private EcsSystems _systems;
        private BackleapConfig _config;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            _config = Asset<BackleapConfig>();

            TorsoAimConfig aim = Asset<TorsoAimConfig>();

            CharacterConfig character = Asset<CharacterConfig>();
            Set(character, "_id", HeroId);

            WaveTimelineConfig timeline = Asset<WaveTimelineConfig>();
            LevelConfig level = Asset<LevelConfig>();
            Set(level, "_waves", timeline);

            InputConfig input = Asset<InputConfig>();

            _world = new EcsWorld();

            ContentRegistry registry = new ContentRegistry(new IContentEntry[] { character });
            RunContext context = new RunContext("level.test", HeroId, RunMode.Story, 1, Array.Empty<StatModifierSpec>());

            _systems = new EcsSystems(_world);
            _systems.Add(new StartBackleapSystem());
            _systems.Add(new TickBackleapTimersSystem());
            _systems.Add(new AdvanceBackleapSystem());
            _systems.Add(new UpdateAirborneSystem());
            _systems.Add(new MoveSystem());
            _systems.Add(new ResolveAreaStrikeSystem());
            _systems.Add(new AdvanceProjectilesSystem());
            _systems.Add(new DetectContactDamageSystem());
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Inject(_clock, input, level, registry, context, new SpatialGrid(_world, 45f, 1f), new EnemyMotionBounds(2.5f, 0.9f, 8f, 0.5f), (IViewFactory)new RecordingViewFactory());
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);
            _world.GetPool<PreviousPosition>().Add(_hero);
            _world.GetPool<Velocity>().Add(_hero);
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;
            _world.GetPool<MoveIntent>().Add(_hero);
            _world.GetPool<BodyRadius>().Add(_hero).Value = 0.4f;
            _world.GetPool<Health>().Add(_hero).Current = 100f;
            _world.GetPool<BackleapStats>().Add(_hero).Config = _config;

            ref Height height = ref _world.GetPool<Height>().Add(_hero);
            height.Clearance = Clearance;

            ref TorsoAim torso = ref _world.GetPool<TorsoAim>().Add(_hero);
            torso.Config = aim;
            torso.Direction = Vector3.forward;
            torso.Desired = Vector3.forward;
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
        public void RunningLeapsAwayFromTheTarget()
        {
            int target = Enemy(new Vector3(0f, 0f, 6f));
            Aim(target);
            SetIntent(Vector3.right);

            Press();
            Step(LeapTicks);

            Vector3 position = Position();

            Assert.AreEqual(-_config.Distance, position.z, 0.01f, "the leap goes straight away from the target");
            Assert.AreEqual(0f, position.x, 0.01f, "not against the run when there is a target");
        }

        [Test]
        public void RunningWithoutATargetLeapsAgainstTheRun()
        {
            SetIntent(Vector3.right);

            Press();
            Step(LeapTicks);

            Assert.AreEqual(-_config.Distance, Position().x, 0.01f);
            Assert.AreEqual(0f, Position().z, 0.01f);
        }

        [Test]
        public void TheLeapGrantsInvulnerability()
        {
            SetIntent(Vector3.right);

            Press();
            Step(1);

            Assert.IsTrue(_world.GetPool<Invulnerable>().Has(_hero));
        }

        [Test]
        public void ReleasingAfterTheLeapFallsAndLandsWithACooldown()
        {
            SetIntent(Vector3.right);

            Press();
            Step(LeapTicks);

            Assert.AreEqual(BackleapPhase.Fall, Leap().Phase, "not held, so no hover");

            Step(FallTicks);

            Assert.IsFalse(_world.GetPool<Backleaping>().Has(_hero));
            Assert.AreEqual(0f, Height(), 1e-5f);
            Assert.IsTrue(_world.GetPool<BackleapCooldown>().Has(_hero));
        }

        [Test]
        public void HoldingHoversUntilTheLimit()
        {
            SetIntent(Vector3.right);
            Hold(true);

            Press();
            Step(LeapTicks);

            Assert.AreEqual(BackleapPhase.Hover, Leap().Phase);

            Step(HoverTicks - 1);

            Assert.AreEqual(BackleapPhase.Hover, Leap().Phase, "still hovering one tick before the limit");
            Assert.AreEqual(_config.LeapHeight, Height(), 1e-5f);

            Step(1);

            Assert.AreEqual(BackleapPhase.Fall, Leap().Phase, "the limit ends the hover even while the button is held");
        }

        [Test]
        public void HoldingGlidesFurtherAndSteersWithTheStick()
        {
            SetIntent(Vector3.right);
            Hold(true);

            Press();
            Step(LeapTicks);

            float leapEnd = Position().x;

            Step(20);

            Assert.Less(Position().x, leapEnd - 1f, "the glide keeps carrying the leap away");

            SetIntent(Vector3.forward);
            Step(40);

            Vector3 velocity = _world.GetPool<Velocity>().Get(_hero).Value;

            Assert.Greater(Position().z, 1.5f, "the stick turns the glide");
            Assert.AreEqual(_config.GlideSpeed, velocity.z, 0.01f, "the glide settles at its own speed along the stick");
        }

        [Test]
        public void ReleasingTheButtonEndsTheHover()
        {
            SetIntent(Vector3.right);
            Hold(true);

            Press();
            Step(LeapTicks + 10);

            Assert.AreEqual(BackleapPhase.Hover, Leap().Phase);

            Hold(false);
            Step(1);

            Assert.AreEqual(BackleapPhase.Fall, Leap().Phase);
        }

        [Test]
        public void StandingHopsAndDivesIntoAnAreaStrike()
        {
            int near = Enemy(new Vector3(1.5f, 0f, 0f));
            int far = Enemy(new Vector3(6f, 0f, 0f));

            Press();
            Step(HopTicks);

            Assert.AreEqual(BackleapPhase.Dive, Leap().Phase);
            Assert.AreEqual(Vector3.zero, Position(), "the dive stays in place");
            Assert.AreEqual(0, DamageTo(near));

            Step(DiveTicks);

            Assert.IsFalse(_world.GetPool<Backleaping>().Has(_hero));
            Assert.AreEqual(_config.ImpactDamage, DamageTo(near), 1e-4f, "the landing strikes around her");
            Assert.IsTrue(_world.GetPool<Pushed>().Has(near), "and throws the enemies back");
            Assert.AreEqual(0f, DamageTo(far));
        }

        [Test]
        public void HighInTheAirContactDamageMissesHerAndOnTheGroundItHits()
        {
            int enemy = Enemy(new Vector3(0.3f, 0f, 0f));
            _world.GetPool<ContactDamage>().Add(enemy).Value = 10f;

            _world.GetPool<Height>().Get(_hero).Value = Clearance + 0.5f;
            Step(2);

            Assert.IsTrue(_world.GetPool<Airborne>().Has(_hero));
            Assert.AreEqual(0f, DamageTo(_hero), "above the clearance contact must not reach her");

            _world.GetPool<Height>().Get(_hero).Value = 0f;
            Step(2);

            Assert.IsFalse(_world.GetPool<Airborne>().Has(_hero));
            Assert.Greater(DamageTo(_hero), 0f, "on the ground the same enemy hurts her");
        }

        [Test]
        public void BoltsFlyUnderHerWhileSheIsAirborne()
        {
            _world.GetPool<Height>().Get(_hero).Value = Clearance + 0.5f;

            int bolt = _world.NewEntity();
            ref Projectile projectile = ref _world.GetPool<Projectile>().Add(bolt);
            projectile.LastPosition = new Vector3(-1f, 0f, 0f);
            projectile.Damage = 10f;
            projectile.Radius = 0.15f;
            projectile.RemainingTicks = 30;
            _world.GetPool<Position>().Add(bolt).Value = new Vector3(-1f, 0f, 0f);
            _world.GetPool<Velocity>().Add(bolt).Value = new Vector3(30f, 0f, 0f);

            Step(6);

            Assert.AreEqual(0f, DamageTo(_hero));
            Assert.IsTrue(_world.GetPool<Projectile>().Has(bolt), "the bolt passes on instead of breaking on her");
        }

        [Test]
        public void ANewPressDuringTheCooldownDoesNothing()
        {
            SetIntent(Vector3.right);

            Press();
            Step(LeapTicks + FallTicks);
            Press();
            Step(1);

            Assert.IsFalse(_world.GetPool<Backleaping>().Has(_hero));
        }

        private int Enemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<PreviousPosition>().Add(enemy).Value = position;
            _world.GetPool<Velocity>().Add(enemy);
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;
            _world.GetPool<Health>().Add(enemy).Current = 100f;

            return enemy;
        }

        private void Aim(int target)
        {
            ref TorsoAim torso = ref _world.GetPool<TorsoAim>().Get(_hero);
            torso.HasTarget = true;
            torso.Target = _world.PackEntity(target);
        }

        private void SetIntent(Vector3 intent)
        {
            _world.GetPool<MoveIntent>().Get(_hero).Value = intent;
        }

        private void Hold(bool held)
        {
            _world.GetPool<BackleapStats>().Get(_hero).Held = held;
        }

        private void Press()
        {
            EcsPool<BackleapRequest> requests = _world.GetPool<BackleapRequest>();

            if (requests.Has(_hero) == false)
                requests.Add(_hero);

            requests.Get(_hero).Age = 0f;
        }

        private ref Backleaping Leap()
        {
            return ref _world.GetPool<Backleaping>().Get(_hero);
        }

        private Vector3 Position()
        {
            return _world.GetPool<Position>().Get(_hero).Value;
        }

        private float Height()
        {
            return _world.GetPool<Height>().Get(_hero).Value;
        }

        private float DamageTo(int target)
        {
            float total = 0f;

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                ref DamageEvent damageEvent = ref _world.GetPool<DamageEvent>().Get(entity);

                if (damageEvent.Target.Unpack(_world, out int hit) && hit == target)
                    total += damageEvent.Amount;
            }

            return total;
        }

        private void Step(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private T Asset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);

            return asset;
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");
            field.SetValue(target, value);
        }
    }
}
