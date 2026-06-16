using Cysharp.Threading.Tasks;

namespace DemonSlaughter.Core.Services
{
    public interface ISceneLoader
    {
        UniTask LoadAsync(string sceneName);
    }
}