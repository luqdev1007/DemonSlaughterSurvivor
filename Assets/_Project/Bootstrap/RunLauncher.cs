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
            Stop();

            RunContext runContext = new RunContext(request.LevelId, request.CharacterId, request.Mode, _seedSource.Next());

            LevelConfig level = _content.Get<LevelConfig>(request.LevelId);

            await _sceneLoader.LoadAsync(level.SceneName, ct);

            _runScope = _projectScope.CreateChild<RunLifetimeScope>(
                builder => builder.RegisterInstance(runContext),
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
