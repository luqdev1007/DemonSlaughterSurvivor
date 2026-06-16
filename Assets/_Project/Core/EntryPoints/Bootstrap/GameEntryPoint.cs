using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Services;
using UnityEngine;
using VContainer.Unity;

namespace DemonSlaughter.Core.EntryPoints.Bootstrap
{
    public sealed class GameEntryPoint : IStartable
    {
        private readonly ISceneLoader _sceneLoader;

        public GameEntryPoint(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public void Start()
        {
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            Debug.Log("Bootstrap started");

            await UniTask.Delay(1000);

            Debug.Log("Services inited, start loading main menu scene");

            await _sceneLoader.LoadAsync("MainMenu");

            Debug.Log("MainMenu loaded");
        }
    }
}