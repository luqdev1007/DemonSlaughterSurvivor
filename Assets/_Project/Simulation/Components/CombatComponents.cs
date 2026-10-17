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

    public struct ProjectilePassThrough { public int RemainingTicks; }

    public struct Pushed
    {
        public Vector3 Velocity;
        public int RemainingTicks;
        public int TotalTicks;
        public float ArcHeight;
    }

    public struct Lifted { public int SettleTicks; }

    public enum DamageKind
    {
        Unmarked = 0,
        Weapon = 1,
        Contact = 2,
        Perk = 3,
        Projectile = 4,
        EnemyAbility = 5,
    }

    public struct DamageEvent
    {
        public EcsPackedEntity Target;
        public EcsPackedEntity Source;
        public Vector3 SourcePosition;
        public float Amount;
        public DamageKind Kind;
        public bool IsSlash;
    }

    public struct KillingBlow
    {
        public EcsPackedEntity Source;
        public Vector3 SourcePosition;
        public bool IsSlash;
        public bool IsEmpowered;
    }

    public struct DamageApplied { public float Amount; }

    public struct AreaStrike
    {
        public EcsPackedEntity Owner;
        public EcsPackedEntity Anchor;
        public bool Anchored;
        public int DelayTicks;
        public bool FollowOwner;
        public Vector3 Origin;
        public Vector3 Forward;
        public float Reach;
        public float HalfAngleDegrees;
        public float Damage;
        public DamageKind Kind;
        public bool IsSlash;
        public float PushSpeed;
        public float PushSeconds;
        public float PushArcHeight;
    }

    public struct AreaStrikeEvent
    {
        public EcsPackedEntity Owner;
        public Vector3 Origin;
        public Vector3 Forward;
        public float Reach;
        public float HalfAngleDegrees;
        public int Hits;
    }

    public struct DiedEvent { }

    public struct HealEvent
    {
        public EcsPackedEntity Target;
        public float Amount;
    }

    public struct LifeSteal
    {
        public float OnKill;
        public float OnKillBase;
        public float OnHit;
        public float OnHitBase;
    }

    public struct PendingFinish { public int RemainingTicks; }

    public struct DissolveCountdown { public int RemainingTicks; }
}
