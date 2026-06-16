using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Save;
using DemonSlaughter.Core.Services;
using UnityEngine;

namespace DemonSlaughter.Core.StateMachine.States
{
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
            Debug.Log("GameState Enter START");

            var data = _save.Load();
            Debug.Log("Save loaded");

            await _levelRunner.RunLevel(data.CurrentLevelId);

            Debug.Log("LevelRunner finished");
        }

        public UniTask Exit()
        {
            return UniTask.CompletedTask;
        }
    }
}