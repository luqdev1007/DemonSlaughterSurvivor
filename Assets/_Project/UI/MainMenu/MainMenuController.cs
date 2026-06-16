using DemonSlaughter.Core.StateMachine;
using UnityEngine;
using VContainer;

public sealed class MainMenuController : MonoBehaviour
{
    private GameStateMachine _stateMachine;

    [Inject]
    public void Construct(GameStateMachine stateMachine)
    {
        Debug.Log($"INJECT: {GetEntityId()}");
        _stateMachine = stateMachine;
    }

    private void Awake()
    {
        Debug.Log($"Controller Awake: {GetEntityId()}");
    }

    public void OnNewGameClicked()
    {
        Debug.Log($"CLICK: {GetEntityId()}");
        EnterGame();
    }

    private async void EnterGame()
    {
        await _stateMachine.Enter<GameState>();
    }
}