namespace Game.Core
{
    public interface IMenuRoute
    {
        void RequestUpgrade();

        bool ConsumeUpgradeRequest();
    }
}
