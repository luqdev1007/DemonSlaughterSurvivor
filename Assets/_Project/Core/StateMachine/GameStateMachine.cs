using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace DemonSlaughter.Core.StateMachine
{
    public sealed class GameStateMachine
    {
        private readonly IObjectResolver _resolver;

        private IExitableState _currentState;

        public GameStateMachine(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public async UniTask Enter<TState>() where TState : class, IState
        {
            Debug.Log($"Enter state: {typeof(TState).Name}");

            if (_currentState != null)
                await _currentState.Exit();

            var state = _resolver.Resolve<TState>();

            Debug.Log($"Resolved state: {typeof(TState).Name}");

            _currentState = state;

            await state.Enter();
        }
    }
}