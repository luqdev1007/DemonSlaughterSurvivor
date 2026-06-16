using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Core.StateMachine;
using UnityEngine;

public sealed class MainMenuState : IState
{
    private readonly GameStateMachine _stateMachine;
    private readonly ISceneLoader _sceneLoader;

    public MainMenuState(GameStateMachine stateMachine, ISceneLoader sceneLoader)
    {
        _stateMachine = stateMachine;
        _sceneLoader = sceneLoader;

        Debug.Log("MainMenuState ctor");
    }

    public async UniTask Enter()
    {
        Debug.Log("MainMenuState Enter");

        await _sceneLoader.LoadAsync("MainMenu");
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