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

        // Маппинг levelId -> Addressables адрес сцены
        private static readonly string[] LevelAddresses = new[]
        {
            "Gameplay", // 0 - первый уровень за Гатса
        };

        public LevelRunner(ISceneLoader sceneLoader, ILoadingScreen loadingScreen)
        {
            _sceneLoader = sceneLoader;
            _loadingScreen = loadingScreen;
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
        }
    }
}