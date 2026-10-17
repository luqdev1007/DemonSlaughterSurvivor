namespace Game.Core
{
    public interface IDashInput
    {
        bool IsChargeHeld { get; }

        bool ConsumeChargeReleased();
    }
}
