using Game.Configs;
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
    public sealed class DeathFeedbackTests
    {
        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;

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
        public void TheShippedFeedbackLetsTheHeroDissolveBeforeTheRunEnds()
        {
            FeedbackConfig feedback = AssetDatabase.LoadAssetAtPath<FeedbackConfig>("Assets/_Project/Configs/Feedback_Default.asset");

            Assert.GreaterOrEqual(feedback.PlayerDeathDelaySeconds, feedback.HeroDissolveDelaySeconds + feedback.DissolveSeconds);
            Assert.DoesNotThrow(() => Build(feedback));
        }

        [TestCase(3.6f, 0.8f, 4.3f)]
        [TestCase(-0.1f, 0.8f, 4.5f)]
        public void FeedbackThatEndsTheRunBeforeTheDissolveIsRejected(float delay, float dissolve, float finish)
        {
            FeedbackConfig feedback = ScriptableObject.CreateInstance<FeedbackConfig>();
            _assets.Add(feedback);
            SetField(feedback, "_heroDissolveDelaySeconds", delay);
            SetField(feedback, "_dissolveSeconds", dissolve);
            SetField(feedback, "_playerDeathDelaySeconds", finish);

            Assert.Throws<InvalidOperationException>(() => Build(feedback));
        }

        private void Build(FeedbackConfig feedback)
        {
            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            SetField(level, "_feedback", feedback);
            _assets.Add(level);

            _systems?.Destroy();
            _world?.Destroy();

            _world = new EcsWorld();
            _systems = new EcsSystems(_world);
            _systems.Add(new MarkDeadSystem());
            _systems.Inject(level, new SimulationClock());
            _systems.Init();
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
