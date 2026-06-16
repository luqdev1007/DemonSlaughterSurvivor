using Cysharp.Threading.Tasks;

namespace DemonSlaughter.Core.StateMachine
{
    public interface IState : IExitableState
    {
        UniTask Enter();
    }
}