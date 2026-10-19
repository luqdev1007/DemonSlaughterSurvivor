namespace Game.Core
{
    public interface IRunResultSink
    {
        void ShowVictory();

        void ShowSummary(float runSeconds);
    }
}
