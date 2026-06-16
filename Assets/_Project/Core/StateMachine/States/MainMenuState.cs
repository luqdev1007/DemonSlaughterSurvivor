using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Core.StateMachine;

public sealed class MainMenuState : IState
{
    private readonly GameStateMachine _stateMachine;
    private readonly ISceneLoader _sceneLoader;

    public MainMenuState(GameStateMachine stateMachine, ISceneLoader sceneLoader)
    {
        _stateMachine = stateMachine;
        _sceneLoader = sceneLoader;
    }

    public async UniTask Enter()
    {
        await _sceneLoader.LoadAsync("MainMenu");

        // пока временно:
        // позже сюда придет UI событие
    }

    public UniTask Exit()
    {
        return UniTask.CompletedTask;
    }

    public async UniTask OnNewGame()
    {
        await _stateMachine.Enter<GameState>();
    }
}