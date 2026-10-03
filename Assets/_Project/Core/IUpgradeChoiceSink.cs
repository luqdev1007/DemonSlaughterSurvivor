namespace Game.Core
{
    public interface IUpgradeChoiceSink
    {
        void Show(UpgradeOffer first, UpgradeOffer second, UpgradeOffer third, int count);

        void Hide();
    }
}
