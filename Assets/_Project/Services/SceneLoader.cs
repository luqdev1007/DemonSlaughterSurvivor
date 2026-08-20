using Cysharp.Threading.Tasks;
using Game.Core;
using System.Threading;
using UnityEngine.SceneManagement;

namespace Game.Services
{
    public sealed class SceneLoader : ISceneLoader
    {
        public UniTask LoadAsync(string sceneName, CancellationToken ct)
        {
            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single).ToUniTask(cancellationToken: ct);
        }
    }
}
