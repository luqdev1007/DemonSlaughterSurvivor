namespace DemonSlaughter.Core.StateMachine
{
    public interface IStateFactory
    {
        TState Create<TState>() where TState : class, IState;
    }
}