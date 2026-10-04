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

        [Test]
        public void HeroPlaysTheClipThenDissolvesAfterTheDelayOnce()
        {
            FeedbackConfig feedback = AssetDatabase.LoadAssetAtPath<FeedbackConfig>("Assets/_Project/Configs/Feedback_Default.asset");
            RecordingView view = new RecordingView();

            Build(feedback, withFeedback: true);
            int hero = SpawnHero(view);

            for (int tick = 1; tick <= 400; tick++)
            {
                view.Tick = tick;
                _clock.Advance(Tick);
                _systems.Run();
            }

            int delayTicks = Mathf.RoundToInt(feedback.HeroDissolveDelaySeconds / Tick);

            Assert.AreEqual(1, view.Deaths.Count, "the death clip is triggered once");
            Assert.AreEqual(1, view.Dissolves.Count, "the dissolve starts once");
            Assert.AreEqual(1, view.Deaths[0]);
            Assert.AreEqual(1 + delayTicks, view.Dissolves[0], $"dissolve {feedback.HeroDissolveDelaySeconds} s after the clip");
            Assert.IsFalse(_world.GetPool<DissolveCountdown>().Has(hero));
        }

        [Test]
        public void RunEndsNoEarlierThanTheEndOfTheDissolve()
        {
            FeedbackConfig feedback = AssetDatabase.LoadAssetAtPath<FeedbackConfig>("Assets/_Project/Configs/Feedback_Default.asset");

            Build(feedback, withFeedback: true);
            int hero = SpawnHero(new RecordingView());

            _clock.Advance(Tick);
            _systems.Run();

            int finishTicks = _world.GetPool<PendingFinish>().Get(hero).RemainingTicks;
            int dissolveEnd = Mathf.RoundToInt(feedback.HeroDissolveDelaySeconds / Tick) + Mathf.RoundToInt(feedback.DissolveSeconds / Tick);

            Assert.AreEqual(Mathf.RoundToInt(feedback.PlayerDeathDelaySeconds / Tick), finishTicks);
            Assert.GreaterOrEqual(finishTicks, dissolveEnd);
        }

        private const float Tick = 1f / 60f;

        private SimulationClock _clock;

        private int SpawnHero(IView view)
        {
            int hero = _world.NewEntity();
            _world.GetPool<Player>().Add(hero);
            _world.GetPool<Health>().Add(hero).Current = 0f;
            _world.GetPool<View>().Add(hero).Value = view;

            return hero;
        }

        private sealed class RecordingView : IView
        {
            public readonly List<int> Deaths = new List<int>();
            public readonly List<int> Dissolves = new List<int>();
            public int Tick;

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
                Deaths.Add(Tick);
            }

            public void Dissolve()
            {
                Dissolves.Add(Tick);
            }
        }

        private void Build(FeedbackConfig feedback, bool withFeedback = false)
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
            _clock = new SimulationClock();
            _systems.Add(new MarkDeadSystem());

            if (withFeedback)
            {
                _systems.Add(new PlayDeathFeedbackSystem());
                _systems.Add(new CleanupEventsSystem());
            }

            _systems.Inject(level, _clock);
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
