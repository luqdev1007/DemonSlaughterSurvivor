using Cysharp.Threading.Tasks;
using Game.Core;
using System.Threading;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class BootstrapEntryPoint : IAsyncStartable
    {
        private readonly ISceneLoader _sceneLoader;

        public BootstrapEntryPoint(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public UniTask StartAsync(CancellationToken ct)
        {
            return _sceneLoader.LoadAsync("MainMenu", ct);
        }
    }
}
