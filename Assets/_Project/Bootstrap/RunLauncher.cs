using Cysharp.Threading.Tasks;
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

        private RunLifetimeScope _runScope;

        public RunLauncher(ProjectLifetimeScope projectScope, ISceneLoader sceneLoader, ISeedSource seedSource)
        {
            _projectScope = projectScope;
            _sceneLoader = sceneLoader;
            _seedSource = seedSource;
        }

        public async UniTask StartAsync(RunRequest request, CancellationToken ct)
        {
            // A run in progress is torn down before the next one loads: two live worlds would tick in parallel.
            Stop();

            RunContext runContext = new RunContext(request.LevelId, request.CharacterId, request.Mode, _seedSource.Next());

            // TODO ContentRegistry (step 1, block 4): LevelId is a content ID, not a scene name.
            await _sceneLoader.LoadAsync(request.LevelId, ct);

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
