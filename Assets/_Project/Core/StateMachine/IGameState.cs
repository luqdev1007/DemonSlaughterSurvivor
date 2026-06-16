using Cysharp.Threading.Tasks;

namespace DemonSlaughter.Core.StateMachine
{
    public interface IGameState
    {
        UniTask Enter();
        UniTask Exit();
    }
}