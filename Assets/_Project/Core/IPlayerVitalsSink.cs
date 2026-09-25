namespace Game.Core
{
    public interface IPlayerVitalsSink
    {
        void Publish(float health, float maxHealth);
    }
}
