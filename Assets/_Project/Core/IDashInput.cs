namespace Game.Core
{
    public interface IDashInput
    {
        bool IsChargeHeld { get; }

        bool IsDashHeld { get; }

        bool ConsumeChargeReleased();
    }
}
