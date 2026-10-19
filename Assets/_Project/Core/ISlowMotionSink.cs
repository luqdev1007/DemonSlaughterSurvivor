namespace Game.Core
{
    public interface ISlowMotionSink
    {
        void RequestSlowMotion(float timeScale, float realSeconds);
    }
}
