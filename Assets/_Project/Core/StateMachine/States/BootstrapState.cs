using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Loading;

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

            // Здесь позже будет загрузка Addressables, конфигов и т.д.
            await SimulateLoadingAsync();

            await _stateMachine.Enter<MainMenuState>();
        }

        public UniTask Exit()
        {
            return UniTask.CompletedTask;
        }

        private async UniTask SimulateLoadingAsync()
        {
            for (int i = 0; i <= 10; i++)
            {
                _loadingScreen.SetProgress(i / 10f);
                await UniTask.Delay(100);
            }
        }
    }
}