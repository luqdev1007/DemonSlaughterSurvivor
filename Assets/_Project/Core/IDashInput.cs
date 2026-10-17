namespace Game.Core
{
    public interface IDashInput
    {
        bool IsDashHeld { get; }

        bool ConsumeDashReleased();
    }
}
