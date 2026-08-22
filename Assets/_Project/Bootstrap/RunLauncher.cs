using Cysharp.Threading.Tasks;
using Game.Core;
using System;
using System.Threading;
using UnityEngine;
using VContainer;

namespace Game.Bootstrap
{
    public sealed class RunLauncher : IRunLauncher, IDisposable
    {
        private readonly ProjectLifetimeScope _projectScope;
        private readonly ISceneLoader _sceneLoader;

        private RunLifetimeScope _runScope;

        public RunLauncher(ProjectLifetimeScope projectScope, ISceneLoader sceneLoader)
        {
            _projectScope = projectScope;
            _sceneLoader = sceneLoader;
        }

        public async UniTask StartAsync(RunRequest request, CancellationToken ct)
        {
            int seed = Environment.TickCount;
            RunContext runContext = new RunContext(request.LevelId, request.CharacterId, request.Mode, seed);
            Debug.Log($"seed: {seed}");

            await _sceneLoader.LoadAsync(request.LevelId, ct);

            _runScope = _projectScope.CreateChild<RunLifetimeScope>(b => b.RegisterInstance(runContext));
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            _runScope?.Dispose();
            _runScope = null;
        }
    }
}
