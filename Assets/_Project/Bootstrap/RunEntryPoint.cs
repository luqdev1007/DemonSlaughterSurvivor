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
        private readonly IContentRegistry _registry;
        private readonly IViewFactory _viewFactory;
        private readonly ICameraService _cameraService;
        private readonly IPlayerVitalsSink _vitalsSink;
        private readonly IUpgradeChoiceSink _choiceSink;
        private readonly IUpgradeChoiceInput _choiceInput;
        private readonly IDebugLevelUpInput _debugLevelUpInput;
        private readonly LevelConfig _levelConfig;
        private readonly InputConfig _inputConfig;

        private EcsWorld _world;
        private IEcsSystems _systems;
        private IEcsSystems _choiceSystems;
        private UpgradeChoiceGate _choiceGate;
        private SpatialGrid _spatialGrid;
        private StatModifiers _statModifiers;
        private EnemyMotionBounds _motionBounds;

        private float _accumulator;
        private bool _isFinishing;
        private bool _isPaused;
        private float _timeScaleBeforePause;

        public RunEntryPoint(
            RunContext context,
            SimulationClock clock,
            RunOutcome outcome,
            IRunLauncher launcher,
            IInputService inputService,
            IContentRegistry registry,
            IViewFactory viewFactory,
            ICameraService cameraService,
            IPlayerVitalsSink vitalsSink,
            IUpgradeChoiceSink choiceSink,
            IUpgradeChoiceInput choiceInput,
            IDebugLevelUpInput debugLevelUpInput,
            LevelConfig levelConfig,
            InputConfig inputConfig)
        {
            _context = context;
            _clock = clock;
            _outcome = outcome;
            _launcher = launcher;
            _inputService = inputService;
            _registry = registry;
            _viewFactory = viewFactory;
            _cameraService = cameraService;
            _vitalsSink = vitalsSink;
            _choiceSink = choiceSink;
            _choiceInput = choiceInput;
            _debugLevelUpInput = debugLevelUpInput;
            _levelConfig = levelConfig;
            _inputConfig = inputConfig;
        }

        public void Initialize()
        {
            _inputService.ResetLatches();

            _world = new EcsWorld();

            _spatialGrid = new SpatialGrid(_world, _levelConfig.ArenaRadius, _levelConfig.SpatialCellSize);

            _statModifiers = new StatModifiers(_world);

            CharacterConfig character = _registry.Get<CharacterConfig>(_context.CharacterId);

            _motionBounds = EnemyMotionBounds.From(character, _levelConfig.Waves);

            _choiceGate = new UpgradeChoiceGate();

            _systems = RunSystems.Build(_world);
            _choiceSystems = RunSystems.BuildChoice(_world);

            Inject(_systems);
            Inject(_choiceSystems);

            _systems.Init();
            _choiceSystems.Init();
        }

        private void Inject(IEcsSystems systems)
        {
            systems.Inject(
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
                _motionBounds,
                _outcome,
                _vitalsSink,
                _choiceGate,
                _choiceSink,
                _choiceInput,
                _debugLevelUpInput
                );
        }

        public void Tick()
        {
            if (_isFinishing)
                return;

            if (_isPaused)
            {
                _choiceSystems.Run();

                if (_choiceGate.IsAwaiting)
                    return;

                Resume();
            }

            _accumulator += Time.deltaTime;

            int steps = 0;

            while (_accumulator >= FixedDelta && steps < MaxStepsPerFrame)
            {
                _clock.Advance(FixedDelta);

                _systems.Run();

                _accumulator -= FixedDelta;
                steps++;

                if (_outcome.IsFinished)
                {
                    _isFinishing = true;
                    break;
                }

                if (_choiceGate.IsAwaiting == false)
                    continue;

                Pause();
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
            if (_isPaused)
                Resume();

            _cameraService.SetFollowTarget(null);

            _choiceSystems?.Destroy();
            _choiceSystems = null;

            _systems?.Destroy();
            _systems = null;

            _world?.Destroy();
            _world = null;

            _spatialGrid = null;
            _statModifiers = null;
            _motionBounds = null;
            _choiceGate = null;
        }

        private void Pause()
        {
            _isPaused = true;
            _accumulator = 0f;

            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
        }

        private void Resume()
        {
            _isPaused = false;
            _accumulator = 0f;

            Time.timeScale = _timeScaleBeforePause;

            _inputService.ResetLatches();
        }
    }
}
