namespace Game.Core
{
    public interface IPlayerVitalsSink
    {
        void Publish(float health, float maxHealth, bool hasUltimate, float ultimateCharge, bool ultimateActive);
    }
}
