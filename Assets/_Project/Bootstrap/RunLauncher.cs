using Cysharp.Threading.Tasks;
using Game.Configs;
using Game.Core;
using System;
using System.Threading;
using VContainer;

namespace Game.Bootstrap
{
    public sealed class RunLauncher : IRunLauncher, IDisposable
    {
        private const string RunScopeName = "RunLifetimeScope";

        private readonly ProjectLifetimeScope _projectScope;
        private readonly ISceneLoader _sceneLoader;
        private readonly ISeedSource _seedSource;
        private readonly IContentRegistry _content;
        private readonly ScenesConfig _scenes;

        private RunLifetimeScope _runScope;
        private bool _isStarting;
        private bool _isFinishing;

        public RunLauncher(
            ProjectLifetimeScope projectScope,
            ISceneLoader sceneLoader,
            ISeedSource seedSource,
            IContentRegistry content,
            ScenesConfig scenes)
        {
            _projectScope = projectScope;
            _sceneLoader = sceneLoader;
            _seedSource = seedSource;
            _content = content;
            _scenes = scenes;
        }

        public async UniTask StartAsync(RunRequest request, CancellationToken ct)
        {
            if (_isStarting)
                throw new InvalidOperationException(
                    $"{nameof(RunLauncher)}.{nameof(StartAsync)} was called while a previous start was still in flight. " +
                    "Starting twice would load the scene twice and build a second run scope with its own ECS world, " +
                    "leaking the first one. Wait for the running start to finish, or call Stop first.");

            ct.ThrowIfCancellationRequested();

            _isStarting = true;

            try
            {
                Stop();

                RunContext runContext = new RunContext(request.LevelId, request.CharacterId, request.Mode, _seedSource.Next(), Array.Empty<StatModifierSpec>());

                LevelConfig level = _content.Get<LevelConfig>(request.LevelId);

                await _sceneLoader.LoadAsync(level.SceneName, CancellationToken.None);

                _runScope = _projectScope.CreateChild<RunLifetimeScope>(
                    builder =>
                    {
                        builder.RegisterInstance(runContext);
                        builder.RegisterInstance(level);
                    },
                    RunScopeName);
            }
            finally
            {
                _isStarting = false;
            }
        }

        public async UniTask FinishAsync()
        {
            if (_isFinishing)
                return;

            if (_runScope == null)
                return;

            _isFinishing = true;

            try
            {
                await UniTask.NextFrame();

                Stop();

                await _sceneLoader.LoadAsync(_scenes.MainMenuScene, CancellationToken.None);
            }
            finally
            {
                _isFinishing = false;
            }
        }

        public void Stop()
        {
            _runScope?.Dispose();
            _runScope = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
