using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Loading;
using DemonSlaughter.Core.Services;

namespace DemonSlaughter.Core.StateMachine.States
{
    public sealed class MainMenuState : IState
    {
        private readonly GameStateMachine _stateMachine;
        private readonly ISceneLoader _sceneLoader;
        private readonly ILoadingScreen _loadingScreen;

        public MainMenuState(
            GameStateMachine stateMachine,
            ISceneLoader sceneLoader,
            ILoadingScreen loadingScreen)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
            _loadingScreen = loadingScreen;
        }

        public async UniTask Enter()
        {
            await _sceneLoader.LoadAsync("MainMenu");

            await _loadingScreen.HideAsync();
        }

        public UniTask Exit()
        {
            return UniTask.CompletedTask;
        }

        public async UniTask OnNewGame()
        {
            await _loadingScreen.ShowAsync();
            await _stateMachine.Enter<GameState>();
        }
    }
}