namespace DemonSlaughter.Gameplay.ECS.Components
{
    public struct HealthComponent
    {
        public float Current;
        public float Max;
    }

    public struct DamageComponent
    {
        public float Value;
    }

    public struct AttackCooldownComponent
    {
        public float Remaining;
    }
}