using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DemonSlaughter.Core.StateMachine
{
    public sealed class GameStateMachine
    {
        private readonly IStateFactory _factory;

        private IExitableState _currentState;

        public GameStateMachine(IStateFactory factory)
        {
            _factory = factory;
        }

        public async UniTask Enter<TState>() where TState : class, IState
        {
            Debug.Log($"Enter state: {typeof(TState).Name}");

            if (_currentState != null)
                await _currentState.Exit();

            var state = _factory.Create<TState>();

            _currentState = state;

            await state.Enter();
        }
    }
}