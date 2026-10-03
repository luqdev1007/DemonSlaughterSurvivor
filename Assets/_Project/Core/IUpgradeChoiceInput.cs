namespace Game.Core
{
    public interface IUpgradeChoiceInput
    {
        bool TryConsume(out int index);

        void Clear();
    }
}
