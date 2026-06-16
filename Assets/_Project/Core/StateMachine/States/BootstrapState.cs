using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Loading;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DemonSlaughter.Core.StateMachine.States
{
    public sealed class BootstrapState : IState
    {
        private readonly GameStateMachine _stateMachine;
        private readonly ILoadingScreen _loadingScreen;

        public BootstrapState(GameStateMachine stateMachine, ILoadingScreen loadingScreen)
        {
            _stateMachine = stateMachine;
            _loadingScreen = loadingScreen;
        }

        public async UniTask Enter()
        {
            await _loadingScreen.ShowAsync();

            await InitializeAddressables();

            await _stateMachine.Enter<MainMenuState>();
        }

        public UniTask Exit()
        {
            return UniTask.CompletedTask;
        }

        private async UniTask InitializeAddressables()
        {
            _loadingScreen.SetProgress(0f);

            await Addressables.InitializeAsync();

            _loadingScreen.SetProgress(1f);
        }
    }
}