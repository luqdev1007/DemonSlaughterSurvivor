using Cysharp.Threading.Tasks;
using System;

namespace DemonSlaughter.Core.Services
{
    public interface ISceneLoader
    {
        UniTask LoadAsync(string sceneName);
        UniTask LoadAddressableAsync(string address, IProgress<float> progress = null);
    }
}