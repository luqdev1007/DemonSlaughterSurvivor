namespace Game.Core
{
    public interface IRunResultInput
    {
        bool TryConsumeExit(out RunExitChoice choice);
    }
}
