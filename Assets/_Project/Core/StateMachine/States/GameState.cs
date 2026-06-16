using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Save;
using DemonSlaughter.Core.StateMachine;

public sealed class GameState : IState
{
    private readonly ISaveService _save;
    private readonly ILevelRunner _levelRunner;

    public GameState(ISaveService save, ILevelRunner levelRunner)
    {
        _save = save;
        _levelRunner = levelRunner;
    }

    public async UniTask Enter()
    {
        var data = _save.Load();

        await _levelRunner.RunLevel(data.CurrentLevelId);
    }

    public UniTask Exit()
    {
        return UniTask.CompletedTask;
    }
}