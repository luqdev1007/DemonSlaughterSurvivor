using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Services;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace DemonSlaughter.Infrastructure.Services
{
    public sealed class SceneLoader : ISceneLoader
    {
        public UniTask LoadAsync(string sceneName)
        {
            return SceneManager.LoadSceneAsync(sceneName).ToUniTask();
        }

        public async UniTask LoadAddressableAsync(string address, IProgress<float> progress = null)
        {
            var handle = Addressables.LoadSceneAsync(address, LoadSceneMode.Single);

            while (!handle.IsDone)
            {
                progress?.Report(handle.PercentComplete);
                await UniTask.Yield();
            }

            progress?.Report(1f);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Failed to load scene: {address}");
            }
        }
    }
}