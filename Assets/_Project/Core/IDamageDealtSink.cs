namespace Game.Core
{
    public interface IDamageDealtSink
    {
        void Advance(float simulationTime);

        void Record(float amount);
    }
}
