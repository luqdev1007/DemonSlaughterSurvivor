using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class PushLiftTests
    {
        private const int PushTicks = 36;
        private const float ArcHeight = 1.2f;
        private const float Tolerance = 1e-4f;

        private EcsWorld _world;
        private EcsSystems _systems;

        [SetUp]
        public void SetUp()
        {
            _world = new EcsWorld();
            _systems = new EcsSystems(_world);
            _systems.Add(new ApplyPushSystem());
            _systems.Add(new SyncPushLiftViewSystem());
            _systems.Inject(Clock());
            _systems.Init();
        }

        [TearDown]
        public void TearDown()
        {
            _systems.Destroy();
            _world.Destroy();
        }

        [Test]
        public void DashPushLiftsTheEnemyAlongAnArcAndSettlesItOnTheFloor()
        {
            RecordingView view = new RecordingView();
            int enemy = SpawnEnemy(view);

            Push(enemy, ArcHeight);

            for (int tick = 0; tick < PushTicks + 4; tick++)
                _systems.Run();

            Assert.AreEqual(PushTicks - 1 + 2, view.Lifts.Count, "35 airborne ticks, then two zeros to settle the interpolation");

            float peak = 0f;

            for (int index = 0; index < PushTicks - 1; index++)
            {
                Assert.Greater(view.Lifts[index], 0f, $"airborne tick {index + 1} must be above the floor");
                peak = Mathf.Max(peak, view.Lifts[index]);
            }

            Assert.AreEqual(ArcHeight, peak, Tolerance, "the arc peaks at the configured height, halfway through the push");
            Assert.AreEqual(0f, view.Lifts[PushTicks - 1]);
            Assert.AreEqual(0f, view.Lifts[PushTicks]);
            Assert.IsFalse(_world.GetPool<Lifted>().Has(enemy), "after settling the view is left alone");
        }

        [Test]
        public void FlatPushNeverTouchesTheLift()
        {
            RecordingView view = new RecordingView();
            int enemy = SpawnEnemy(view);

            Push(enemy, 0f);

            for (int tick = 0; tick < PushTicks + 4; tick++)
                _systems.Run();

            Assert.AreEqual(0, view.Lifts.Count);
        }

        [Test]
        public void PushFromAPointMidArcFlattensAndSettles()
        {
            RecordingView view = new RecordingView();
            int enemy = SpawnEnemy(view);

            Push(enemy, ArcHeight);

            for (int tick = 0; tick < 10; tick++)
                _systems.Run();

            PushFromPoint.Apply(_world.GetPool<Pushed>(), enemy, Vector3.forward, 5f, PushTicks);

            Assert.AreEqual(0f, _world.GetPool<Pushed>().Get(enemy).ArcHeight, "a push from a point has no arc of its own");

            view.Lifts.Clear();
            _systems.Run();
            _systems.Run();
            _systems.Run();

            Assert.AreEqual(new List<float> { 0f, 0f }, view.Lifts, "the enemy drops to the floor and the lift is settled twice");
        }

        [Test]
        public void PushedEnemyKeepsItsFacingWhileItFlies()
        {
            SimulationClock clock = new SimulationClock();
            clock.Advance(1f / 60f);

            EcsSystems facing = new EcsSystems(_world);
            facing.Add(new ApplyPushSystem());
            facing.Add(new FaceVelocitySystem());
            facing.Inject(clock);
            facing.Init();

            int enemy = SpawnEnemy(new RecordingView());
            _world.GetPool<Facing>().Add(enemy).Value = Vector3.forward;
            _world.GetPool<TurnSpeed>().Add(enemy).Value = 720f;

            Push(enemy, ArcHeight);

            for (int tick = 0; tick < PushTicks - 1; tick++)
                facing.Run();

            Assert.AreEqual(Vector3.forward, _world.GetPool<Facing>().Get(enemy).Value, "a shoved body must not turn to face where it is shoved");

            facing.Destroy();
        }

        [Test]
        public void PushReactionPlaysOnceForTheWholePushInSeconds()
        {
            RecordingView view = new RecordingView();
            int enemy = SpawnEnemy(view);

            Push(enemy, ArcHeight);
            Assert.AreEqual(0, view.PushReactions.Count, "nothing plays before the view systems run");

            ShowPushStart();

            for (int tick = 0; tick < PushTicks + 4; tick++)
                _systems.Run();

            Assert.AreEqual(1, view.PushReactions.Count, "one reaction per push, not per airborne tick");
            Assert.AreEqual(PushTicks / 60f, view.PushReactions[0], Tolerance, "the reaction lasts exactly as long as the push");
        }

        [Test]
        public void FlatPushPlaysTheReactionToo()
        {
            RecordingView view = new RecordingView();
            int enemy = SpawnEnemy(view);

            Push(enemy, 0f);
            ShowPushStart();

            Assert.AreEqual(1, view.PushReactions.Count, "a push from a point (heroic leap) has no arc but still shoves the body");
            Assert.AreEqual(0, view.Lifts.Count);
        }

        [Test]
        public void ANewPushMidFlightRestartsTheReaction()
        {
            RecordingView view = new RecordingView();
            int enemy = SpawnEnemy(view);

            Push(enemy, ArcHeight);
            ShowPushStart();

            for (int tick = 0; tick < 10; tick++)
                _systems.Run();

            PushFromPoint.Apply(_world.GetPool<Pushed>(), enemy, Vector3.forward, 5f, PushTicks);
            ShowPushStart();

            Assert.AreEqual(2, view.PushReactions.Count);
        }

        [TestCase(0.3f, 1f)]
        [TestCase(0.8f, 1f)]
        [TestCase(1.3f, 0.75f)]
        [TestCase(1.8f, 0.5f)]
        public void DashPushFadesFromFullAtContactToTheEdgeScaleAtTheReach(float distance, float expected)
        {
            Assert.AreEqual(expected, SweepDashPushSystem.ResolvePushScale(distance, 0.8f, 1f, 0.5f), Tolerance);
        }

        [Test]
        public void ZeroReachPushesOnlyAtContactWithFullForce()
        {
            Assert.AreEqual(1f, SweepDashPushSystem.ResolvePushScale(0.5f, 0.8f, 0f, 0.5f), Tolerance);
        }

        private void ShowPushStart()
        {
            EcsSystems sync = new EcsSystems(_world);
            sync.Add(new SyncPushLiftViewSystem());
            sync.Inject(Clock());
            sync.Init();
            sync.Run();
            sync.Destroy();
        }

        private static SimulationClock Clock()
        {
            SimulationClock clock = new SimulationClock();
            clock.Advance(1f / 60f);

            return clock;
        }

        private int SpawnEnemy(IView view)
        {
            int enemy = _world.NewEntity();

            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Velocity>().Add(enemy);
            _world.GetPool<View>().Add(enemy).Value = view;

            return enemy;
        }

        private void Push(int enemy, float arcHeight)
        {
            ref Pushed pushed = ref _world.GetPool<Pushed>().Add(enemy);
            pushed.Velocity = Vector3.right * 10f;
            pushed.RemainingTicks = PushTicks;
            pushed.TotalTicks = PushTicks;
            pushed.ArcHeight = arcHeight;
        }

        private sealed class RecordingView : IView
        {
            public readonly List<float> Lifts = new List<float>();
            public readonly List<float> PushReactions = new List<float>();

            public Transform Transform => null;

            public void SetPosition(Vector3 position)
            {
            }

            public void SetRotation(Quaternion rotation)
            {
            }

            public void PlayWeaponHit()
            {
            }

            public void PlayHit(Vector3 direction)
            {
            }

            public void SetInvulnerable(bool value)
            {
            }

            public void SetBerserk(bool value)
            {
            }

            public void SetPowerUp(bool active, float progress)
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

            public void SetLift(float height)
            {
                Lifts.Add(height);
            }

            public void PlayPushReaction(float seconds)
            {
                PushReactions.Add(seconds);
            }

            public void PlayAttack(string trigger, float speed)
            {
            }

            public void SetAction(in ActionCue cue)
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
