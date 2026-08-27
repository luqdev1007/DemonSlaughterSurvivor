using Game.Core;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class RunEntryPoint : IInitializable, ITickable, IDisposable
    {
        private readonly RunContext _context;
        private readonly SimulationClock _clock;

        private EcsWorld _world;
        private IEcsSystems _systems;

        public RunEntryPoint(RunContext context, SimulationClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public void Initialize()
        {
            _world = new EcsWorld();
            _systems = RunSystems.Build(_world);

            _systems.Inject(
                _clock
                );

            _systems.Init();
        }

        public void Tick()
        {
            _clock.Advance(Time.deltaTime);

            _systems.Run();
        }

        public void Dispose()
        {
            _systems?.Destroy();
            _systems = null;

            _world?.Destroy();
            _world = null;
        }
    }
}
