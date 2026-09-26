using Cysharp.Threading.Tasks;
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
        private readonly RunOutcome _outcome;
        private readonly IRunLauncher _launcher;
        private readonly IInputService _inputService;
        private readonly IDebugDamageInput _debugDamageInput;
        private readonly IDebugStatInput _debugStatInput;
        private readonly IContentRegistry _registry;
        private readonly IViewFactory _viewFactory;
        private readonly ICameraService _cameraService;
        private readonly IPlayerVitalsSink _vitalsSink;
        private readonly LevelConfig _levelConfig;
        private readonly InputConfig _inputConfig;

        private EcsWorld _world;
        private IEcsSystems _systems;
        private SpatialGrid _spatialGrid;
        private StatModifiers _statModifiers;

        private float _accumulator;
        private bool _isFinishing;

        public RunEntryPoint(
            RunContext context,
            SimulationClock clock,
            RunOutcome outcome,
            IRunLauncher launcher,
            IInputService inputService,
            IDebugDamageInput debugDamageInput,
            IDebugStatInput debugStatInput,
            IContentRegistry registry,
            IViewFactory viewFactory,
            ICameraService cameraService,
            IPlayerVitalsSink vitalsSink,
            LevelConfig levelConfig,
            InputConfig inputConfig)
        {
            _context = context;
            _clock = clock;
            _outcome = outcome;
            _launcher = launcher;
            _inputService = inputService;
            _debugDamageInput = debugDamageInput;
            _debugStatInput = debugStatInput;
            _registry = registry;
            _viewFactory = viewFactory;
            _cameraService = cameraService;
            _vitalsSink = vitalsSink;
            _levelConfig = levelConfig;
            _inputConfig = inputConfig;
        }

        public void Initialize()
        {
            _inputService.ResetLatches();

            _world = new EcsWorld();

            _spatialGrid = new SpatialGrid(_world, _levelConfig.ArenaRadius, _levelConfig.SpatialCellSize);

            _statModifiers = new StatModifiers(_world);

            _systems = RunSystems.Build(_world);

            _systems.Inject(
                _context,
                _clock,
                _inputService,
                _registry,
                _viewFactory,
                _cameraService,
                _levelConfig,
                _inputConfig,
                _spatialGrid,
                _statModifiers,
                _outcome,
                _debugDamageInput,
                _debugStatInput,
                _vitalsSink
                );

            _systems.Init();
        }

        public void Tick()
        {
            if (_isFinishing)
                return;

            _accumulator += Time.deltaTime;

            int steps = 0;

            while (_accumulator >= FixedDelta && steps < MaxStepsPerFrame)
            {
                _clock.Advance(FixedDelta);

                _systems.Run();

                _accumulator -= FixedDelta;
                steps++;

                if (_outcome.IsFinished == false)
                    continue;

                _isFinishing = true;
                break;
            }

            if (steps == MaxStepsPerFrame)
                _accumulator = 0f;

            if (_isFinishing == false)
                return;

            _launcher.FinishAsync().Forget();
        }

        public void Dispose()
        {
            _cameraService.SetFollowTarget(null);

            _systems?.Destroy();
            _systems = null;

            _world?.Destroy();
            _world = null;

            _spatialGrid = null;
            _statModifiers = null;
        }
    }
}
