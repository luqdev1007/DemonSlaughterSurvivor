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
    public sealed class FireTrailTests
    {
        private const float Tick = 1f / 60f;
        private const string PerkId = "perk.flaming-dash.test";
        private const string DashPath = "Assets/_Project/Configs/Perks/FlamingDash_Berserk.asset";

        private static readonly float[] DashSteps = { 0.05f, 0.12f, 0.2f, 0.3f, 0.42f, 0.55f, 0.66f, 0.74f, 0.8f, 0.83f, 0.84f, 0.84f, 0.84f, 0.81f };

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private CountingViewFactory _views;
        private FlamingDashConfig _config;
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
        public void An8MetreDashLeaves13Segments()
        {
            Build(maxLive: 200);
            TakePerk(1);

            Dash(Vector3.zero, Vector3.right);

            Assert.AreEqual(13, Segments());
            Assert.AreEqual(13, _views.Created);
        }

        [TestCase(1, 3f)]
        [TestCase(5, 5f)]
        public void SegmentsLiveTheLevelLifetimeAndReturnTheirViewsOnce(int level, float seconds)
        {
            Build(maxLive: 200);
            TakePerk(level);

            Dash(Vector3.zero, Vector3.right);

            int lifetime = Mathf.RoundToInt(seconds / Tick);

            Run(lifetime - DashSteps.Length - 1);

            Assert.Greater(Segments(), 0, "the first segments are still alive just before their lifetime");

            Run(DashSteps.Length + 2);

            Assert.AreEqual(0, Segments());
            Assert.AreEqual(13, _views.Released, "every view returned");
            Assert.AreEqual(0, _views.DoubleReleases);
        }

        [Test]
        public void ANewDashStartsTheSpacingAfresh()
        {
            Build(maxLive: 200);
            TakePerk(1);

            Dash(Vector3.zero, Vector3.right, new[] { 0.5f });
            Assert.AreEqual(0, Segments());

            Dash(new Vector3(0f, 0f, 5f), Vector3.right, new[] { 0.5f });

            Assert.AreEqual(0, Segments(), "0.5 m + 0.5 m in two dashes is not 0.6 m of one trail");
        }

        [Test]
        public void TheCapStopsNewSegments()
        {
            Build(maxLive: 5);
            TakePerk(1);

            Dash(Vector3.zero, Vector3.right);

            Assert.AreEqual(5, Segments());
            Assert.AreEqual(5, _views.Created);
        }

        [Test]
        public void SegmentsOutliveTheirHero()
        {
            Build(maxLive: 200);
            TakePerk(1);

            Dash(Vector3.zero, Vector3.right);
            _world.GetPool<Dead>().Add(_hero);

            Run(Mathf.RoundToInt(3f / Tick) + 1);

            Assert.AreEqual(0, Segments());
            Assert.AreEqual(13, _views.Released);
            Assert.AreEqual(0, _views.DoubleReleases);
        }

        [Test]
        public void WithoutThePerkTheDashLeavesNothing()
        {
            Build(maxLive: 200);

            Dash(Vector3.zero, Vector3.right);

            Assert.AreEqual(0, Segments());
            Assert.IsFalse(_world.GetPool<FireTrail>().Has(_hero));
        }

        private void Dash(Vector3 start, Vector3 direction, float[] steps = null)
        {
            steps = steps ?? DashSteps;

            EcsPool<Dashing> dashes = _world.GetPool<Dashing>();
            ref Dashing dashing = ref dashes.Add(_hero);
            dashing.TotalTicks = steps.Length;
            dashing.RemainingTicks = steps.Length;

            Vector3 at = start;
            _world.GetPool<Position>().Get(_hero).Value = at;

            for (int index = 0; index < steps.Length; index++)
            {
                _world.GetPool<PreviousPosition>().Get(_hero).Value = at;
                at += direction * steps[index];
                _world.GetPool<Position>().Get(_hero).Value = at;

                Run(1);

                ref Dashing running = ref dashes.Get(_hero);
                running.RemainingTicks--;
            }

            dashes.Del(_hero);
            _world.GetPool<PreviousPosition>().Get(_hero).Value = at;
        }

        private int Segments()
        {
            return _world.Filter<FireSegment>().End().GetEntitiesCount();
        }

        private void Run(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private void TakePerk(int level)
        {
            int entity = _world.NewEntity();

            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(entity);
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = PerkId;
            taken.Level = level;
        }

        private void Build(int maxLive)
        {
            _config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<FlamingDashConfig>(DashPath));
            _assets.Add(_config);

            GameObject prefab = new GameObject("StubSegment");
            _assets.Add(prefab);
            SetField(_config, "_segmentPrefab", prefab);
            SetField(_config, "_maxLiveSegments", maxLive);

            PerkConfig perk = ScriptableObject.CreateInstance<PerkConfig>();
            _assets.Add(perk);

            PerkLevel[] levels = new PerkLevel[_config.LevelCount];

            for (int index = 0; index < levels.Length; index++)
            {
                levels[index] = new PerkLevel();
                SetField(levels[index], "_modifiers", Array.Empty<StatModifierEntry>());
            }

            SetField(perk, "_id", PerkId);
            SetField(perk, "_levels", levels);
            SetField(perk, "_behaviour", _config);

            _world = new EcsWorld();
            _clock = new SimulationClock();
            _views = new CountingViewFactory();

            _systems = new EcsSystems(_world);
            _systems.Add(new SpawnFireTrailSystem());
            _systems.Add(new TickFireSegmentsSystem());
            _systems.Inject(new ContentRegistry(new IContentEntry[] { perk }), (IViewFactory)_views, _clock);
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);
            _world.GetPool<PreviousPosition>().Add(_hero);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }

        private sealed class CountingViewFactory : IViewFactory
        {
            private readonly HashSet<IView> _live = new HashSet<IView>();

            public int Created;
            public int Released;
            public int DoubleReleases;

            public IView Create(GameObject prefab, Vector3 position)
            {
                Created++;
                StubView view = new StubView();
                _live.Add(view);

                return view;
            }

            public void Release(IView view)
            {
                if (_live.Remove(view) == false)
                {
                    DoubleReleases++;

                    return;
                }

                Released++;
            }

            public void Retire(IView view, float seconds, Vector3 knockbackDirection)
            {
                Release(view);
            }
        }

        private sealed class StubView : IView
        {
            public Transform Transform => null;

            public void SetPosition(Vector3 position)
            {
            }

            public void SetRotation(Quaternion rotation)
            {
            }

            public void PlayHit()
            {
            }

            public void SetInvulnerable(bool value)
            {
            }

            public void SetBerserk(bool value)
            {
            }

            public void SetDashing(bool value)
            {
            }

            public void SetRunning(bool value)
            {
            }

            public void SetSpecialAttack(bool value)
            {
            }

            public void PlayAttack(string trigger, float speed)
            {
            }

            public void PlayDeath()
            {
            }

            public void Dissolve()
            {
            }
        }
    }
}
