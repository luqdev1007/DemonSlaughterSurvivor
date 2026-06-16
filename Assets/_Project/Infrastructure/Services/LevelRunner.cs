using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Loading;
using DemonSlaughter.Core.Services;
using System;
using UnityEngine;

namespace DemonSlaughter.Infrastructure.Services
{
    public sealed class LevelRunner : ILevelRunner
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly ILoadingScreen _loadingScreen;
        private readonly GameplayReadySignal _readySignal;

        private static readonly string[] LevelAddresses = new[]
        {
            "Gameplay",
        };

        public LevelRunner(
            ISceneLoader sceneLoader,
            ILoadingScreen loadingScreen,
            GameplayReadySignal readySignal)
        {
            _sceneLoader = sceneLoader;
            _loadingScreen = loadingScreen;
            _readySignal = readySignal;
        }

        public async UniTask RunLevel(int levelId)
        {
            Debug.Log($"RunLevel: {levelId}");

            if (levelId < 0 || levelId >= LevelAddresses.Length)
            {
                Debug.LogError($"Unknown levelId: {levelId}");
                return;
            }

            var progress = new Progress<float>(value =>
                _loadingScreen.SetProgress(value));

            await _sceneLoader.LoadAddressableAsync(LevelAddresses[levelId], progress);

            await WaitForReadyAsync();

            await _loadingScreen.HideAsync();
        }

        private UniTask WaitForReadyAsync()
        {
            var utcs = new UniTaskCompletionSource();
            _readySignal.OnReady += () => utcs.TrySetResult();
            return utcs.Task;
        }
    }
}