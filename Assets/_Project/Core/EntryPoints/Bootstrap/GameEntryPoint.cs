using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Save;
using DemonSlaughter.Core.StateMachine;
using DemonSlaughter.Core.StateMachine.States;
using UnityEngine;
using VContainer.Unity;

namespace DemonSlaughter.Core.EntryPoints.Bootstrap
{
    public sealed class GameEntryPoint : IStartable
    {
        private readonly GameStateMachine _stateMachine;

        public GameEntryPoint(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void Start()
        {
            RunAsync().Forget();
        }

        private async UniTask RunAsync()
        {
            await _stateMachine.Enter<BootstrapState>();
        }
    }
}