namespace Game.Core
{
    public interface IRunResultSink
    {
        void ShowVictory();

        void ShowRunEnd(RunReport report, RunRewards rewards);
    }
}
