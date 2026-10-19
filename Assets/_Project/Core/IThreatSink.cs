namespace Game.Core
{
    public interface IThreatSink
    {
        void SetThreat(float waveProgress, bool bossAlive);
    }
}
