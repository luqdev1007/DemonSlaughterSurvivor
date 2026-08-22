using Cysharp.Threading.Tasks;
using System.Threading;

namespace Game.Core
{
    public interface IRunLauncher
    {
        UniTask StartAsync(RunRequest request, CancellationToken ct);
        void Stop();
    }
}
