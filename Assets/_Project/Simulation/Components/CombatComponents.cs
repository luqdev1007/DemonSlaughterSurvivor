using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct Health { public float Current; }

    public struct MaxHealth { public float Value; public float Base; }

    public struct HitInvulnerability { public float Seconds; }

    public struct DamageTaken { public float Value; public float Base; }

    public struct ContactDamage { public float Value; public float Base; }

    public struct Invulnerable { public int RemainingTicks; }

    public struct Pushed
    {
        public Vector3 Velocity;
        public int RemainingTicks;
        public int TotalTicks;
    }

    public enum DamageKind
    {
        Unmarked = 0,
        Weapon = 1,
        Contact = 2,
        Perk = 3,
    }

    public struct DamageEvent
    {
        public EcsPackedEntity Target;
        public EcsPackedEntity Source;
        public Vector3 SourcePosition;
        public float Amount;
        public DamageKind Kind;
    }

    public struct KillingBlow { public Vector3 SourcePosition; }

    public struct DamageApplied { public float Amount; }

    public struct DiedEvent { }

    public struct PendingFinish { public int RemainingTicks; }

    public struct DissolveCountdown { public int RemainingTicks; }
}
