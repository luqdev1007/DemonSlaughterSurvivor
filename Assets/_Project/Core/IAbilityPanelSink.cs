namespace Game.Core
{
    public interface IAbilityPanelSink
    {
        void Publish(string abilityId, float remainingSeconds);
    }
}
