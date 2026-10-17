using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class ReadInputStateTests
    {
        private const float DeadZone = 1.2f;
        private const float CameraYaw = 45f;

        private static readonly Vector3 HeroPosition = new Vector3(3f, 0f, -2f);

        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private FakeInput _input;
        private FakePointer _pointer;
        private FakeAim _aim;
        private int _hero;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;

            foreach (ScriptableObject asset in _assets)
                UnityEngine.Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void MouseRunStandsInsideTheDeadZone()
        {
            Build(ControlScheme.MouseRun);

            _aim.Point = HeroPosition + new Vector3(DeadZone - 0.05f, 0f, 0f);
            Tick();

            Assert.AreEqual(Vector3.zero, Intent());
        }

        [Test]
        public void MouseRunRunsTowardsTheCursorOutsideTheDeadZone()
        {
            Build(ControlScheme.MouseRun);

            _aim.Point = HeroPosition + new Vector3(0f, 0f, DeadZone + 0.05f);
            Tick();

            AssertDirection(Vector3.forward, Intent());

            _aim.Point = HeroPosition + new Vector3(-6f, 0f, 6f);
            Tick();

            AssertDirection(new Vector3(-1f, 0f, 1f).normalized, Intent());
        }

        [Test]
        public void MouseRunIgnoresTheCursorHeightAboveTheGround()
        {
            Build(ControlScheme.MouseRun);

            _aim.Point = HeroPosition + new Vector3(5f, 3f, 0f);
            Tick();

            AssertDirection(Vector3.right, Intent());
        }

        [Test]
        public void MouseRunStandsWhileTheStandButtonIsHeld()
        {
            Build(ControlScheme.MouseRun);

            _aim.Point = HeroPosition + new Vector3(10f, 0f, 0f);
            _pointer.StandHeld = true;
            Tick();

            Assert.AreEqual(Vector3.zero, Intent());
            Assert.IsTrue(State().StandHeld);

            _pointer.StandHeld = false;
            Tick();

            AssertDirection(Vector3.right, Intent());
        }

        [Test]
        public void MouseRunStandsWithoutAnAimPoint()
        {
            Build(ControlScheme.MouseRun);

            _aim.HasAim = false;
            Tick();

            Assert.AreEqual(Vector3.zero, Intent());
            Assert.IsFalse(State().HasAim);
        }

        [Test]
        public void MouseRunIgnoresTheKeyboard()
        {
            Build(ControlScheme.MouseRun);

            _aim.Point = HeroPosition;
            _input.Axis = new Vector2(1f, 1f);
            Tick();

            Assert.AreEqual(Vector3.zero, Intent());
        }

        [Test]
        public void KeyboardKeepsTheCameraRelativeDirection()
        {
            Build(ControlScheme.Keyboard);

            _aim.Point = HeroPosition + new Vector3(10f, 0f, 0f);

            Vector2[] axes = { new Vector2(0f, 1f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-0.3f, 0.7f), Vector2.zero };

            foreach (Vector2 axis in axes)
            {
                _input.Axis = axis;
                Tick();

                Vector3 expected = Quaternion.Euler(0f, CameraYaw, 0f) * new Vector3(axis.x, 0f, axis.y).normalized;

                Assert.AreEqual(expected, Intent(), $"axis {axis}");
            }
        }

        [Test]
        public void StateCarriesTheAimPoint()
        {
            Build(ControlScheme.Keyboard);

            _aim.Point = new Vector3(7f, 0f, 8f);
            Tick();

            Assert.IsTrue(State().HasAim);
            Assert.AreEqual(_aim.Point, State().AimPoint);
        }

        [Test]
        public void MouseHoldIsNotImplementedYet()
        {
            Build(ControlScheme.MouseHold);

            Assert.Throws<NotImplementedException>(Tick);
        }

        private void Build(ControlScheme scheme)
        {
            InputConfig inputConfig = ScriptableObject.CreateInstance<InputConfig>();
            SetField(inputConfig, "_defaultScheme", scheme);
            _assets.Add(inputConfig);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_cameraYaw", CameraYaw);
            _assets.Add(level);

            _input = new FakeInput();
            _pointer = new FakePointer();
            _aim = new FakeAim { HasAim = true, Point = HeroPosition };

            _world = new EcsWorld();
            _systems = new EcsSystems(_world);
            _systems.Add(new ReadInputStateSystem());
            _systems.Inject(_input, (IPointerInput)_pointer, (IAimInput)_aim, inputConfig, level);
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<MoveIntent>().Add(_hero);
            _world.GetPool<InputState>().Add(_hero);
            _world.GetPool<Aim>().Add(_hero).DeadZone = DeadZone;
            _world.GetPool<Position>().Add(_hero).Value = HeroPosition;
        }

        private void Tick()
        {
            _systems.Run();
        }

        private Vector3 Intent()
        {
            return _world.GetPool<MoveIntent>().Get(_hero).Value;
        }

        private InputState State()
        {
            return _world.GetPool<InputState>().Get(_hero);
        }

        private static void AssertDirection(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(1f, actual.magnitude, 1e-5f, $"intent {actual} is not a unit vector");
            Assert.AreEqual(0f, Vector3.Distance(expected, actual), 1e-5f, $"intent {actual}, expected {expected}");
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }

        private sealed class FakeInput : IInputService
        {
            public Vector2 Axis;

            public Vector2 MoveAxis => Axis;

            public bool ConsumeDashPressed()
            {
                return false;
            }

            public bool ConsumeUltimatePressed()
            {
                return false;
            }

            public void ResetLatches()
            {
            }
        }

        private sealed class FakePointer : IPointerInput
        {
            public bool StandHeld;

            public Vector2 PointerPosition => Vector2.zero;

            public bool IsStandHeld => StandHeld;
        }

        private sealed class FakeAim : IAimInput
        {
            public bool HasAim;
            public Vector3 Point;

            public bool TryGetAimPoint(out Vector3 point)
            {
                point = Point;

                return HasAim;
            }
        }
    }
}
