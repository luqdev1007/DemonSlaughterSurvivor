using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class SpecialAttackMovementTests
    {
        private const float Tick = 1f / 60f;
        private const float Speed = 5f;

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private int _hero;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;
        }

        [Test]
        public void WithoutTheMarkerTheHeroWalksAndTurns()
        {
            Build();

            Run(10);

            Assert.AreEqual(new Vector3(Speed, 0f, 0f), Velocity());
            Assert.Greater(Facing().x, 0.5f);
        }

        [Test]
        public void SpecialAttackStopsTurningButNotWalking()
        {
            Build();
            _world.GetPool<SpecialAttack>().Add(_hero).LocksMovement = false;

            Run(10);

            Assert.AreEqual(new Vector3(Speed, 0f, 0f), Velocity());
            Assert.AreEqual(Vector3.forward, Facing());
        }

        [Test]
        public void LockingSpecialAttackStopsWalking()
        {
            Build();
            Run(1);

            _world.GetPool<SpecialAttack>().Add(_hero).LocksMovement = true;

            Run(1);

            Assert.AreEqual(Vector3.zero, Velocity());
        }

        private void Build()
        {
            _world = new EcsWorld();
            _clock = new SimulationClock();
            _systems = new EcsSystems(_world);

            _systems.Add(new ApplyMoveSpeedSystem());
            _systems.Add(new FaceVelocitySystem());
            _systems.Inject(_clock);
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<MoveIntent>().Add(_hero).Value = Vector3.right;
            _world.GetPool<MoveSpeed>().Add(_hero).Value = Speed;
            _world.GetPool<Velocity>().Add(_hero);
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;
            _world.GetPool<TurnSpeed>().Add(_hero).Value = 720f;
        }

        private void Run(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private Vector3 Velocity()
        {
            return _world.GetPool<Velocity>().Get(_hero).Value;
        }

        private Vector3 Facing()
        {
            return _world.GetPool<Facing>().Get(_hero).Value;
        }
    }
}
