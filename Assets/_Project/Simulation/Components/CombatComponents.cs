using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct Health { public float Current; }

    public struct MaxHealth { public float Value; }

    public struct HitInvulnerability { public float Seconds; }

    public struct ContactDamage { public float Value; }

    public struct Invulnerable { public int RemainingTicks; }

    public struct Pushed
    {
        public Vector3 Velocity;
        public int RemainingTicks;
        public int TotalTicks;
    }

    public struct DamageEvent
    {
        public EcsPackedEntity Target;
        public EcsPackedEntity Source;
        public Vector3 SourcePosition;
        public float Amount;
    }

    public struct KillingBlow { public Vector3 SourcePosition; }

    public struct DamageApplied { }

    public struct DiedEvent { }

    public struct PendingFinish { public int RemainingTicks; }
}
