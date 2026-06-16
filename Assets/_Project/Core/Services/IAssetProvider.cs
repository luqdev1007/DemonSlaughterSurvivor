using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DemonSlaughter.Core.Services
{
    public interface IAssetProvider
    {
        UniTask<T> LoadAsync<T>(string address) where T : Object;
        void Release(string address);
    }
}