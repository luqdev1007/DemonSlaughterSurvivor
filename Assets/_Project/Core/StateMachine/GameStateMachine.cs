using Cysharp.Threading.Tasks;
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
            if (_currentState != null)
            {
                await _currentState.Exit();
            }

            var state = _resolver.Resolve<TState>();

            _currentState = state;

            await state.Enter();
        }
    }
}