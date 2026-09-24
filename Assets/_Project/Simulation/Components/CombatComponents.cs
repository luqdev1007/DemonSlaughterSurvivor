using Leopotam.EcsLite;

namespace Game.Simulation.Components
{
    public struct Health { public float Current; }

    public struct MaxHealth { public float Value; }

    public struct HitInvulnerability { public float Seconds; }

    public struct ContactDamage { public float Value; }

    public struct Invulnerable { public int RemainingTicks; }

    public struct DamageEvent
    {
        public EcsPackedEntity Target;
        public EcsPackedEntity Source;
        public float Amount;
    }
}
