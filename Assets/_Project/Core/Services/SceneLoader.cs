using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace DemonSlaughter.Core.Services
{
    public sealed class SceneLoader : ISceneLoader
    {
        public UniTask LoadAsync(string sceneName)
        {
            return SceneManager.LoadSceneAsync(sceneName).ToUniTask();
        }
    }
}