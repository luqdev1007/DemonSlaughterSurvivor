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
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class PushFromPointTests
    {
        private const float Tick = 1f / 60f;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;

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
        public void PushAt5For06SecondsCarriesTheEnemyAbout146Metres()
        {
            Build();

            int enemy = SpawnEnemy(new Vector3(2f, 0f, 0f));
            int ticks = Mathf.RoundToInt(0.6f / Tick);

            PushFromPoint.Apply(_world.GetPool<Pushed>(), enemy, PushFromPoint.Direction(Vector3.zero, new Vector3(2f, 0f, 0f), Vector3.forward), 5f, ticks);

            Run(ticks + 4);

            Vector3 position = _world.GetPool<Position>().Get(enemy).Value;

            Assert.AreEqual(36, ticks);
            Assert.AreEqual(2f + 5f * Tick * 17.5f, position.x, 1e-4f, "35 ticks of a linearly fading 5 m/s push");
            Assert.AreEqual(1.4583f, position.x - 2f, 1e-3f);
            Assert.AreEqual(0f, position.z, 1e-6f);
            Assert.IsFalse(_world.GetPool<Pushed>().Has(enemy), "the push must end");
        }

        [Test]
        public void PushPointsAwayFromTheOriginInThePlane()
        {
            Vector3 direction = PushFromPoint.Direction(new Vector3(1f, 0.5f, 1f), new Vector3(1f, 2f, 4f), Vector3.forward);

            Assert.AreEqual(new Vector3(0f, 0f, 1f), direction);
        }

        [Test]
        public void CoincidentPositionsFallBackToTheFacing()
        {
            Vector3 direction = PushFromPoint.Direction(Vector3.one, Vector3.one, new Vector3(-2f, 1f, 0f));

            Assert.AreEqual(new Vector3(-1f, 0f, 0f), direction);
            Assert.AreEqual(Vector3.forward, PushFromPoint.Direction(Vector3.one, Vector3.one, Vector3.up));
        }

        [Test]
        public void PushOverwritesAnEarlierPush()
        {
            Build();

            int enemy = SpawnEnemy(Vector3.zero);
            EcsPool<Pushed> pushes = _world.GetPool<Pushed>();

            PushFromPoint.Apply(pushes, enemy, Vector3.left, 9f, 3);
            PushFromPoint.Apply(pushes, enemy, Vector3.right, 5f, 36);

            ref Pushed pushed = ref pushes.Get(enemy);

            Assert.AreEqual(new Vector3(5f, 0f, 0f), pushed.Velocity);
            Assert.AreEqual(36, pushed.RemainingTicks);
            Assert.AreEqual(36, pushed.TotalTicks);
        }

        [Test]
        public void BoundsTakeTheFastestPushSource()
        {
            CharacterConfig character = AssetDatabase.LoadAssetAtPath<CharacterConfig>("Assets/_Project/Configs/CharacterConfig.asset");
            LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/_Project/Configs/Levels/Level_Gameplay.asset");
            HeroicLeapConfig leap = Object.Instantiate(AssetDatabase.LoadAssetAtPath<HeroicLeapConfig>("Assets/_Project/Configs/Perks/HeroicLeap_Berserk.asset"));
            _assets.Add(leap);
            SetField(leap, "_pushSpeed", 7f);

            PerkConfig perk = ScriptableObject.CreateInstance<PerkConfig>();
            _assets.Add(perk);
            SetField(perk, "_behaviour", leap);

            float dashOnly = EnemyMotionBounds.From(character, level.Waves, Array.Empty<PerkConfig>()).PushSpeed;
            float withLeap = EnemyMotionBounds.From(character, level.Waves, new[] { perk }).PushSpeed;

            Assert.AreEqual(character.Dash.PushSpeed, dashOnly);
            Assert.AreEqual(7f, withLeap);

            SetField(leap, "_pushSpeed", 1f);

            Assert.AreEqual(character.Dash.PushSpeed, EnemyMotionBounds.From(character, level.Waves, new[] { perk }).PushSpeed, "a slower push must not lower the bound");
        }

        private void Build()
        {
            _world = new EcsWorld();
            _clock = new SimulationClock();
            _systems = new EcsSystems(_world);
            _systems.Add(new ApplyPushSystem());
            _systems.Add(new MoveSystem());
            _systems.Inject(_clock);
            _systems.Init();
        }

        private int SpawnEnemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<Velocity>().Add(enemy);

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
