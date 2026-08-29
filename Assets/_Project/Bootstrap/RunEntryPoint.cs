using Game.Configs;
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
        private const float FixedDelta = 1f / 60f;
        private const int MaxStepsPerFrame = 5;

        private readonly RunContext _context;
        private readonly SimulationClock _clock;
        private readonly IInputService _inputService;
        private readonly IContentRegistry _registry;
        private readonly IViewFactory _viewFactory;
        private readonly LevelConfig _levelConfig;

        private EcsWorld _world;
        private IEcsSystems _systems;

        private float _accumulator;

        public RunEntryPoint(
            RunContext context,
            SimulationClock clock,
            IInputService inputService,
            IContentRegistry registry,
            IViewFactory viewFactory,
            LevelConfig levelConfig)
        {
            _context = context;
            _clock = clock;
            _inputService = inputService;
            _registry = registry;
            _viewFactory = viewFactory;
            _levelConfig = levelConfig;
        }

        public void Initialize()
        {
            _world = new EcsWorld();
            _systems = RunSystems.Build(_world);

            // Strictly between Build and Init: the injected fields of every system
            // are still empty until this call, and Init is where systems start reading them.
            _systems.Inject(
                _context,
                _clock,
                _inputService,
                _registry,
                _viewFactory,
                _levelConfig
                );

            _systems.Init();
        }

        public void Tick()
        {
            _accumulator += Time.deltaTime;

            int steps = 0;

            // Frames are drawn as fast as the hardware manages; the simulation always advances
            // by the same FixedDelta. That is what keeps i-frames, cooldowns and dash distance
            // from becoming a function of the frame rate.
            while (_accumulator >= FixedDelta && steps < MaxStepsPerFrame)
            {
                _clock.Advance(FixedDelta);

                _systems.Run();

                _accumulator -= FixedDelta;
                steps++;
            }

            // Hit the ceiling: drop the debt instead of carrying it into the next frame.
            // Without this the accumulator only grows, every frame runs more ticks than the last,
            // and the game locks up without ever recovering — the death spiral.
            if (steps == MaxStepsPerFrame)
                _accumulator = 0f;
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
