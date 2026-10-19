namespace Game.Core
{
    public interface IPlayerVitalsSink
    {
        void Publish(float health, float maxHealth, bool hasUltimate, float ultimateCharge, bool ultimateActive);

        void PublishDuality(bool hasDuality, float demonic, float formThreshold);

        void PublishConcentration(bool hasConcentration, float fraction);
    }
}
