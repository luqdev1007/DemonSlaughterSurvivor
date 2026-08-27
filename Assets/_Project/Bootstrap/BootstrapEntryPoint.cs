using Cysharp.Threading.Tasks;
using Game.Configs;
using Game.Core;
using System.Threading;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class BootstrapEntryPoint : IAsyncStartable
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly ScenesConfig _scenes;

        public BootstrapEntryPoint(ISceneLoader sceneLoader, ScenesConfig scenes)
        {
            _sceneLoader = sceneLoader;
            _scenes = scenes;
        }

        public UniTask StartAsync(CancellationToken ct)
        {
            return _sceneLoader.LoadAsync(_scenes.MainMenuScene, ct);
        }
    }
}
