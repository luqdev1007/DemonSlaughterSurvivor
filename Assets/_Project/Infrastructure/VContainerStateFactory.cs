using DemonSlaughter.Core.StateMachine;
using VContainer;

namespace DemonSlaughter.Infrastructure
{
    public sealed class VContainerStateFactory : IStateFactory
    {
        private readonly IObjectResolver _resolver;

        public VContainerStateFactory(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public TState Create<TState>() where TState : class, IState
        {
            return _resolver.Resolve<TState>();
        }
    }
}