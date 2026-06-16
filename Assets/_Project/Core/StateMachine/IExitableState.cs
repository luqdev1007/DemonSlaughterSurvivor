using Cysharp.Threading.Tasks;

namespace DemonSlaughter.Core.StateMachine
{
    public interface IExitableState
    {
        UniTask Exit();
    }
}