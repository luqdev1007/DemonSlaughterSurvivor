namespace Game.Core
{
    public interface IExperienceSink
    {
        void Publish(int level, int current, int required);
    }
}
