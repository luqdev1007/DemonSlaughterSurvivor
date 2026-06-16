using DemonSlaughter.Core.StateMachine;
using UnityEngine;
using VContainer;

public sealed class MainMenuController : MonoBehaviour
{
    private GameStateMachine _stateMachine;

    [Inject]
    public void Construct(GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public void OnNewGameClicked()
    {
        EnterGame();
    }

    private async void EnterGame()
    {
        await _stateMachine.Enter<GameState>();
    }
}