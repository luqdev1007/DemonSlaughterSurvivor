using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Services;
using UnityEngine.SceneManagement;

namespace DemonSlaughter.Infrastructure.Services
{
    public sealed class SceneLoader : ISceneLoader
    {
        public UniTask LoadAsync(string sceneName)
        {
            return SceneManager.LoadSceneAsync(sceneName).ToUniTask();
        }
    }

    public static class SceneNames
    {
        public static string Bootstrap = nameof(Bootstrap);
        public static string MainMenu = nameof(MainMenu);
        public static string Gameplay = nameof(Gameplay);
    }
}