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
        private readonly IViewPrewarm _viewPrewarm;
        private readonly ICameraService _cameraService;
        private readonly IPlayerVitalsSink _vitalsSink;
        private readonly IAbilityPanelSink _abilityPanelSink;
        private readonly IDamageNumberSink _damageNumberSink;
        private readonly IDamageDealtSink _damageDealtSink;
        private readonly IItemChoiceSink _itemChoiceSink;
        private ItemChoiceState _itemChoiceState;
        private readonly IUpgradeChoiceSink _choiceSink;
        private readonly IUpgradeChoiceInput _choiceInput;
        private readonly IDebugLevelUpInput _debugLevelUpInput;
        private readonly IDebugVictoryInput _debugVictoryInput;
        private readonly IExperienceSink _experienceSink;
        private readonly IBossVitalsSink _bossVitalsSink;
        private readonly IRunResultSink _resultSink;
        private readonly IPauseInput _pauseInput;
        private readonly IPointerInput _pointerInput;
        private readonly IDashInput _dashInput;
        private readonly IAimInput _aimInput;
        private readonly IPauseMenuSink _pauseMenuSink;
        private readonly IPauseMenuInput _pauseMenuInput;
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
        private bool _isVictoryFrozen;
        private float _timeScaleBeforeVictory;
        private bool _isMenuPaused;
        private float _timeScaleBeforeMenu;

        public RunEntryPoint(
            RunContext context,
            SimulationClock clock,
            RunOutcome outcome,
            IRunLauncher launcher,
            IInputService inputService,
            IContentRegistry registry,
            IViewFactory viewFactory,
            IViewPrewarm viewPrewarm,
            ICameraService cameraService,
            IPlayerVitalsSink vitalsSink,
            IAbilityPanelSink abilityPanelSink,
            IDamageNumberSink damageNumberSink,
            IDamageDealtSink damageDealtSink,
            IItemChoiceSink itemChoiceSink,
            IUpgradeChoiceSink choiceSink,
            IUpgradeChoiceInput choiceInput,
            IDebugLevelUpInput debugLevelUpInput,
            IDebugVictoryInput debugVictoryInput,
            IExperienceSink experienceSink,
            IBossVitalsSink bossVitalsSink,
            IRunResultSink resultSink,
            IPauseInput pauseInput,
            IPointerInput pointerInput,
            IDashInput dashInput,
            IAimInput aimInput,
            IPauseMenuSink pauseMenuSink,
            IPauseMenuInput pauseMenuInput,
            LevelConfig levelConfig,
            InputConfig inputConfig)
        {
            _pauseInput = pauseInput;
            _pointerInput = pointerInput;
            _dashInput = dashInput;
            _aimInput = aimInput;
            _pauseMenuSink = pauseMenuSink;
            _pauseMenuInput = pauseMenuInput;
            _context = context;
            _clock = clock;
            _outcome = outcome;
            _launcher = launcher;
            _inputService = inputService;
            _registry = registry;
            _viewFactory = viewFactory;
            _viewPrewarm = viewPrewarm;
            _cameraService = cameraService;
            _vitalsSink = vitalsSink;
            _abilityPanelSink = abilityPanelSink;
            _damageNumberSink = damageNumberSink;
            _damageDealtSink = damageDealtSink;
            _itemChoiceSink = itemChoiceSink;
            _choiceSink = choiceSink;
            _choiceInput = choiceInput;
            _debugLevelUpInput = debugLevelUpInput;
            _debugVictoryInput = debugVictoryInput;
            _experienceSink = experienceSink;
            _bossVitalsSink = bossVitalsSink;
            _resultSink = resultSink;
            _levelConfig = levelConfig;
            _inputConfig = inputConfig;
        }

        public void Initialize()
        {
            _inputService.ResetLatches();

            _viewPrewarm.Prewarm();

            _world = new EcsWorld();

            _spatialGrid = new SpatialGrid(_world, _levelConfig.ArenaRadius, _levelConfig.SpatialCellSize);

            _statModifiers = new StatModifiers(_world);

            CharacterConfig character = _registry.Get<CharacterConfig>(_context.CharacterId);

            _motionBounds = EnemyMotionBounds.From(character, _levelConfig.Waves, _registry.All<PerkConfig>());

            _choiceGate = new UpgradeChoiceGate();
            _itemChoiceState = new ItemChoiceState();

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
                _pointerInput,
                _dashInput,
                _aimInput,
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
                _abilityPanelSink,
                _damageNumberSink,
                _damageDealtSink,
                _itemChoiceSink,
                _itemChoiceState,
                _choiceGate,
                _choiceSink,
                _choiceInput,
                _debugLevelUpInput,
                _debugVictoryInput,
                _experienceSink,
                _bossVitalsSink
                );
        }

        public void Tick()
        {
            if (_isFinishing)
                return;

            if (_isMenuPaused)
            {
                bool resume = _pauseInput.ConsumePausePressed() | _pauseMenuInput.ConsumeResumeRequested();

                if (resume)
                    ResumeFromMenu();

                return;
            }

            if (_pauseInput.ConsumePausePressed())
            {
                PauseForMenu();

                return;
            }

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

            if (_outcome.Reason == RunEndReason.Victory)
            {
                FinishWithVictoryAsync().Forget();

                return;
            }

            _launcher.FinishAsync().Forget();
        }

        public void Dispose()
        {
            if (_isMenuPaused)
                ReleaseMenuPause();

            if (_isPaused)
                Resume();

            ReleaseVictoryFreeze();

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
            _itemChoiceState = null;
        }

        private async UniTaskVoid FinishWithVictoryAsync()
        {
            _resultSink.ShowVictory();

            _isVictoryFrozen = true;
            _timeScaleBeforeVictory = Time.timeScale;
            Time.timeScale = 0f;

            await UniTask.Delay(TimeSpan.FromSeconds(_levelConfig.Feedback.VictoryBannerSeconds), ignoreTimeScale: true);

            ReleaseVictoryFreeze();

            await _launcher.FinishAsync();
        }

        private void ReleaseVictoryFreeze()
        {
            if (_isVictoryFrozen == false)
                return;

            _isVictoryFrozen = false;

            Time.timeScale = _timeScaleBeforeVictory;
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

        private void PauseForMenu()
        {
            _isMenuPaused = true;
            _accumulator = 0f;

            _timeScaleBeforeMenu = Time.timeScale;
            Time.timeScale = 0f;

            AudioListener.pause = true;

            _pauseMenuSink.Show();
        }

        private void ResumeFromMenu()
        {
            ReleaseMenuPause();

            _pauseMenuSink.Hide();

            _inputService.ResetLatches();

            if (_isPaused)
                _choiceInput.Clear();
        }

        private void ReleaseMenuPause()
        {
            _isMenuPaused = false;
            _accumulator = 0f;

            Time.timeScale = _timeScaleBeforeMenu;

            AudioListener.pause = false;
        }
    }
}
