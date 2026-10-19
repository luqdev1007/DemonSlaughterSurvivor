namespace Game.Core
{
    public interface IScreenShake
    {
        float Multiplier { get; set; }

        void Shake(ShakeKind kind);
    }
}
