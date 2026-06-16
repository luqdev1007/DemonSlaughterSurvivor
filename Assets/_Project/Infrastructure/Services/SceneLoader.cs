using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemonSlaughter.Infrastructure.Services
{
    public sealed class SceneLoader : ISceneLoader
    {
        public SceneLoader()
        {
            Debug.Log("SceneLoader ctor");
        }

        public UniTask LoadAsync(string sceneName)
        {
            return SceneManager.LoadSceneAsync(sceneName).ToUniTask();
        }
    }
}