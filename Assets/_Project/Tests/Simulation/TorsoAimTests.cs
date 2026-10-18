using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class TorsoAimTests
    {
        private const float SearchRadius = 10f;
        private const float KeepMargin = 2f;
        private const float SwitchMargin = 1.5f;
        private const float TwistLimit = 110f;

        private EcsWorld _world;
        private EcsSystems _systems;
        private TorsoAimConfig _config;
        private int _hero;
        private readonly SimulationClock _clock = new SimulationClock();

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TorsoAimConfig>();
            SetField(_config, "_searchRadius", SearchRadius);
            SetField(_config, "_keepMargin", KeepMargin);
            SetField(_config, "_switchMargin", SwitchMargin);
            SetField(_config, "_torsoTurnSpeed", 100000f);
            SetField(_config, "_twistLimit", TwistLimit);
            SetField(_config, "_backpedalHysteresis", 20f);
            SetField(_config, "_turnInPlaceSpeed", 240f);
            SetField(_config, "_turnInPlaceSettle", 8f);

            _world = new EcsWorld();
            _systems = new EcsSystems(_world);
            _systems.Add(new AimTorsoSystem());
            _systems.Add(new FaceLegsSystem());
            _systems.Inject(_clock);
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;
            _world.GetPool<Velocity>().Add(_hero);
            _world.GetPool<TurnSpeed>().Add(_hero).Value = 100000f;
            _world.GetPool<InputState>().Add(_hero);

            ref TorsoAim aim = ref _world.GetPool<TorsoAim>().Add(_hero);
            aim.Config = _config;
            aim.Direction = Vector3.forward;
            aim.Desired = Vector3.forward;
        }

        [TearDown]
        public void TearDown()
        {
            _systems.Destroy();
            _world.Destroy();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void TheTorsoTurnsToTheNearestEnemyInsideTheSearchRadius()
        {
            int near = Enemy(new Vector3(6f, 0f, 0f));
            Enemy(new Vector3(0f, 0f, 8f));
            Enemy(new Vector3(-12f, 0f, 0f));

            Step();

            Assert.AreEqual(near, Target());
            AssertDirection(Vector3.right, Aim().Direction);
        }

        [Test]
        public void NoEnemyInsideTheRadiusLeavesTheTorsoWithoutATarget()
        {
            Enemy(new Vector3(0f, 0f, SearchRadius + 0.5f));

            Step();

            Assert.IsFalse(Aim().HasTarget);
            AssertDirection(Vector3.forward, Aim().Direction);
        }

        [Test]
        public void ACloserEnemyTakesOverOnlyPastTheSwitchMargin()
        {
            int first = Enemy(new Vector3(5f, 0f, 0f));

            Step();

            Assert.AreEqual(first, Target());

            int second = Enemy(new Vector3(0f, 0f, 4f));

            Step();

            Assert.AreEqual(first, Target(), "1 m closer is inside the 1.5 m margin, so the target must not flicker");

            Move(second, new Vector3(0f, 0f, 3.4f));
            Step();

            Assert.AreEqual(second, Target(), "1.6 m closer is past the margin");
        }

        [Test]
        public void TheTargetIsKeptPastTheSearchRadiusUntilTheKeepRadius()
        {
            int target = Enemy(new Vector3(9f, 0f, 0f));

            Step();
            Move(target, new Vector3(SearchRadius + KeepMargin - 0.5f, 0f, 0f));
            Step();

            Assert.AreEqual(target, Target(), "out of the search radius but inside the keep radius");

            Move(target, new Vector3(SearchRadius + KeepMargin + 0.5f, 0f, 0f));
            Step();

            Assert.IsFalse(Aim().HasTarget);
        }

        [Test]
        public void ADeadTargetIsDropped()
        {
            int target = Enemy(new Vector3(4f, 0f, 0f));
            int other = Enemy(new Vector3(0f, 0f, 7f));

            Step();
            _world.GetPool<Dead>().Add(target);
            Step();

            Assert.AreEqual(other, Target());
        }

        [Test]
        public void HoldingRightMouseAimsAtTheCursorAndOverridesTheAutoTarget()
        {
            Enemy(new Vector3(4f, 0f, 0f));

            ref InputState input = ref _world.GetPool<InputState>().Get(_hero);
            input.StandHeld = true;
            input.HasAim = true;
            input.AimPoint = new Vector3(-3f, 0f, 0f);

            Step();

            Assert.IsTrue(Aim().Manual);
            Assert.IsFalse(Aim().HasTarget);
            AssertDirection(Vector3.left, Aim().Direction);
        }

        [Test]
        public void RunningAwayFromTheTargetTurnsTheLegsToRunBackwards()
        {
            Enemy(new Vector3(0f, 0f, 6f));
            SetVelocity(new Vector3(0f, 0f, -5f));

            Step();

            Assert.IsTrue(Aim().Backpedal);
            AssertDirection(Vector3.forward, Facing(), "the legs face the target and run backwards");
        }

        [Test]
        public void RunningSidewaysInsideTheTwistLimitKeepsTheLegsOnTheRun()
        {
            Enemy(new Vector3(0f, 0f, 6f));
            SetVelocity(new Vector3(5f, 0f, 0f));

            Step();

            Assert.IsFalse(Aim().Backpedal);
            AssertDirection(Vector3.right, Facing());
        }

        [Test]
        public void StandingWithTheTorsoPastTheLimitTurnsInPlace()
        {
            Enemy(new Vector3(0f, 0f, -6f));

            Step();

            Assert.AreNotEqual(0, Aim().TurnSide, "180 degrees of twist is past the 110 degree limit");

            for (int tick = 0; tick < 60; tick++)
                Step();

            Assert.AreEqual(0, Aim().TurnSide, "the turn settles once the legs catch up");
            Assert.Less(Vector3.Angle(Facing(), Vector3.back), 8.5f);
        }

        [Test]
        public void StandingWithTheTorsoInsideTheLimitDoesNotTurnTheLegs()
        {
            Enemy(new Vector3(6f, 0f, 0f));

            Step();

            Assert.AreEqual(0, Aim().TurnSide);
            AssertDirection(Vector3.forward, Facing());
        }

        private void Step()
        {
            _clock.Advance(1f / 60f);
            _systems.Run();
        }

        private int Enemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;

            return enemy;
        }

        private void Move(int entity, Vector3 position)
        {
            _world.GetPool<Position>().Get(entity).Value = position;
        }

        private void SetVelocity(Vector3 velocity)
        {
            _world.GetPool<Velocity>().Get(_hero).Value = velocity;
        }

        private ref TorsoAim Aim()
        {
            return ref _world.GetPool<TorsoAim>().Get(_hero);
        }

        private Vector3 Facing()
        {
            return _world.GetPool<Facing>().Get(_hero).Value;
        }

        private int Target()
        {
            Assert.IsTrue(Aim().HasTarget, "the torso has no target");
            Assert.IsTrue(Aim().Target.Unpack(_world, out int target));

            return target;
        }

        private static void AssertDirection(Vector3 expected, Vector3 actual, string message = null)
        {
            Assert.Less(Vector3.Angle(expected, actual), 0.01f, message);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");
            field.SetValue(target, value);
        }
    }
}
