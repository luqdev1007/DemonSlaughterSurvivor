using Cysharp.Threading.Tasks;
using System.Threading;

namespace Game.Core
{
    public interface ISceneLoader
    {
        UniTask LoadAsync(string sceneName, CancellationToken ct);
    }
}
