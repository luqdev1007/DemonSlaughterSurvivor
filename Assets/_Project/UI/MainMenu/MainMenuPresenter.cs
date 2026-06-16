using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.StateMachine.States;
using VContainer.Unity;

namespace DemonSlaughter.UI.MainMenu
{
    public sealed class MainMenuPresenter : IStartable
    {
        private readonly MainMenuView _view;
        private readonly MainMenuState _mainMenuState;

        public MainMenuPresenter(MainMenuView view, MainMenuState mainMenuState)
        {
            _view = view;
            _mainMenuState = mainMenuState;
        }

        public void Start()
        {
            _view.NewGameButton.onClick.AddListener(OnNewGameClicked);
        }

        private void OnNewGameClicked()
        {
            OnNewGameClickedAsync().Forget();
        }

        private async UniTask OnNewGameClickedAsync()
        {
            _view.NewGameButton.interactable = false;

            await _mainMenuState.OnNewGame();
        }
    }
}