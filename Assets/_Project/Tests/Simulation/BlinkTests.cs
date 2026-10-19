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
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class BlinkTests
    {
        private const float Tick = 1f / 60f;
        private const float MaxDistance = 7f;
        private const float ArenaRadius = 45f;
        private const float BodyRadius = 0.4f;

        private readonly List<Object> _assets = new List<Object>();
        private readonly SimulationClock _clock = new SimulationClock();

        private EcsWorld _world;
        private EcsSystems _systems;
        private FakeInput _input;
        private RecordingViewFactory _views;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            GameObject fx = new GameObject("Fx");
            _assets.Add(fx);

            BlinkConfig blink = Asset<BlinkConfig>();
            Set(blink, "_id", "ability.blink.test");
            Set(blink, "_cooldownSeconds", 3f);
            Set(blink, "_maxDistance", MaxDistance);
            Set(blink, "_probeStep", 0.1f);
            Set(blink, "_departFxPrefab", fx);
            Set(blink, "_arriveFxPrefab", fx);

            LevelConfig level = Asset<LevelConfig>();
            Set(level, "_arenaRadius", ArenaRadius);

            _world = new EcsWorld();
            _input = new FakeInput();
            _views = new RecordingViewFactory();

            _systems = new EcsSystems(_world);
            _systems.Add(new ReadBlinkInputSystem());
            _systems.Add(new StartBlinkSystem());
            _systems.Inject(_clock, level, Asset<InputConfig>(), (IInputService)_input, (IViewFactory)_views);
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);
            _world.GetPool<PreviousPosition>().Add(_hero);
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;
            _world.GetPool<BodyRadius>().Add(_hero).Value = BodyRadius;
            _world.GetPool<InputState>().Add(_hero);
            _world.GetPool<BlinkStats>().Add(_hero).Config = blink;
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
        public void ACloseCursorIsReachedExactly()
        {
            Blink(new Vector3(3f, 0f, 4f));

            AssertNear(new Vector3(3f, 0f, 4f), Hero());
            AssertNear(Hero(), _world.GetPool<PreviousPosition>().Get(_hero).Value);
            Assert.AreEqual(2, _views.Created, "a flash where he left and where he arrived");
        }

        [Test]
        public void AFarCursorIsCappedAtTheBlinkDistance()
        {
            Blink(new Vector3(0f, 0f, 20f));

            AssertNear(new Vector3(0f, 0f, MaxDistance), Hero());
        }

        [Test]
        public void TheBlinkNeverLeavesTheArena()
        {
            _world.GetPool<Position>().Get(_hero).Value = new Vector3(42f, 0f, 0f);

            Blink(new Vector3(60f, 0f, 0f));

            Vector3 hero = Hero();
            Assert.LessOrEqual(new Vector2(hero.x, hero.z).magnitude, ArenaRadius - BodyRadius + 1e-4f);
            Assert.AreEqual(ArenaRadius - BodyRadius, hero.x, 1e-3f, "he stops on the edge, not short of it");
        }

        [Test]
        public void ABlinkIntoACrateStopsShortOfIt()
        {
            int crate = _world.NewEntity();
            _world.GetPool<Breakable>().Add(crate);
            _world.GetPool<Position>().Add(crate).Value = new Vector3(0f, 0f, 5f);
            _world.GetPool<BodyRadius>().Add(crate).Value = 0.6f;

            Blink(new Vector3(0f, 0f, 5f));

            Vector3 hero = Hero();
            Assert.AreEqual(0f, hero.x, 1e-4f, "he stays on the blink line");
            Assert.GreaterOrEqual(Vector3.Distance(hero, new Vector3(0f, 0f, 5f)), 0.6f + BodyRadius, "not inside the crate");
            Assert.Greater(hero.z, 3.8f, "only as far back as needed");
        }

        [Test]
        public void TheCooldownSwallowsASecondPress()
        {
            Blink(new Vector3(0f, 0f, 3f));
            Blink(new Vector3(0f, 0f, 6f));

            AssertNear(new Vector3(0f, 0f, 3f), Hero());
            Assert.IsTrue(_world.GetPool<BlinkCooldown>().Has(_hero));

            Step(Mathf.CeilToInt(3f / Tick) + 1);
            Blink(new Vector3(0f, 0f, 6f));

            AssertNear(new Vector3(0f, 0f, 6f), Hero());
        }

        private void Blink(Vector3 cursor)
        {
            ref InputState state = ref _world.GetPool<InputState>().Get(_hero);
            state.HasAim = true;
            state.AimPoint = cursor;

            _input.DashPressed = true;
            Step(1);
        }

        private Vector3 Hero()
        {
            return _world.GetPool<Position>().Get(_hero).Value;
        }

        private static void AssertNear(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), 1e-3f, $"expected {expected}, got {actual}");
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

            for (System.Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");
            field.SetValue(target, value);
        }

        private sealed class FakeInput : IInputService
        {
            public bool DashPressed;

            public Vector2 MoveAxis => Vector2.zero;

            public bool ConsumeDashPressed()
            {
                bool pressed = DashPressed;
                DashPressed = false;

                return pressed;
            }

            public bool ConsumeUltimatePressed()
            {
                return false;
            }

            public void ResetLatches()
            {
                DashPressed = false;
            }
        }
    }
}
