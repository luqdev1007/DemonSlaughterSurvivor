namespace Game.Core
{
    public interface IBossVitalsSink
    {
        void Publish(bool visible, string displayName, float health, float maxHealth);
    }
}
