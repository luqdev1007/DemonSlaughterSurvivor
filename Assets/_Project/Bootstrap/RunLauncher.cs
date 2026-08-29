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

        private RunLifetimeScope _runScope;

        public RunLauncher(ProjectLifetimeScope projectScope, ISceneLoader sceneLoader, ISeedSource seedSource, IContentRegistry content)
        {
            _projectScope = projectScope;
            _sceneLoader = sceneLoader;
            _seedSource = seedSource;
            _content = content;
        }

        public async UniTask StartAsync(RunRequest request, CancellationToken ct)
        {
            // The caller can still back out here: nothing has happened yet.
            ct.ThrowIfCancellationRequested();

            Stop();

            RunContext runContext = new RunContext(request.LevelId, request.CharacterId, request.Mode, _seedSource.Next());

            // A broken level id fails before the scene swap, so the caller is still alive to see it.
            LevelConfig level = _content.Get<LevelConfig>(request.LevelId);

            // Point of no return. The single-mode load destroys the scene the caller lives in,
            // which cancels the caller's token — but cancelling does not stop the load anyway,
            // so honouring that token here would only skip the lines below and leave a loaded
            // arena with no run inside it. The run's lifetime belongs to the project scope,
            // not to whoever pressed the button.
            await _sceneLoader.LoadAsync(level.SceneName, CancellationToken.None);

            _runScope = _projectScope.CreateChild<RunLifetimeScope>(
                builder =>
                {
                    builder.RegisterInstance(runContext);
                    builder.RegisterInstance(level);
                },
                RunScopeName);
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
