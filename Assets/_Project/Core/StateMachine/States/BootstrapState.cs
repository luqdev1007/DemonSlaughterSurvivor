using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DemonSlaughter.Core.StateMachine.States
{
    public sealed class BootstrapState : IState
    {
        private readonly GameStateMachine _stateMachine;

        public BootstrapState(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public async UniTask Enter()
        {
            Debug.Log("Bootstrap");

            await UniTask.Delay(1000);

            await _stateMachine.Enter<MainMenuState>();
        }

        public UniTask Exit()
        {
            Debug.Log("BootstrapState Exit");

            return UniTask.CompletedTask;
        }
    }
}