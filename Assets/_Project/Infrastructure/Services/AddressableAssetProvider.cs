using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Services;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DemonSlaughter.Infrastructure.Services
{
    public sealed class AddressableAssetProvider : IAssetProvider
    {
        private readonly Dictionary<string, AsyncOperationHandle> _cache = new();

        public async UniTask<T> LoadAsync<T>(string address) where T : Object
        {
            if (_cache.TryGetValue(address, out var cached))
                return (T)cached.Result;

            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Failed to load asset: {address}");
                return null;
            }

            _cache[address] = handle;
            return handle.Result;
        }

        public void Release(string address)
        {
            if (!_cache.TryGetValue(address, out var handle)) return;

            Addressables.Release(handle);
            _cache.Remove(address);
        }
    }
}