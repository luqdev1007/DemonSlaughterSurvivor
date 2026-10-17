namespace Game.Core
{
    public static class CombatTextLimits
    {
        public const float MinDamageNumberScale = 0.6f;
        public const float MaxDamageNumberScale = 1.6f;
    }

    public interface ICombatTextSettings
    {
        bool DamageNumbersEnabled { get; }

        float DamageNumberScale { get; }

        bool DpsVisible { get; }

        void SetDamageNumbersEnabled(bool enabled);

        void SetDamageNumberScale(float scale);

        void SetDpsVisible(bool visible);
    }
}
