namespace Game.Core
{
    public interface IRunStatsSink
    {
        void RecordDamageTaken(float amount);

        void RecordGemCollected();
    }
}
