using Game.Configs;
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

    public enum DamageElement
    {
        None = 0,
        Fire = 1,
        Ice = 2,
        Arcane = 3,
        Nature = 4,
    }

    public struct DamageEvent
    {
        public EcsPackedEntity Target;
        public EcsPackedEntity Source;
        public Vector3 SourcePosition;
        public float Amount;
        public DamageKind Kind;
        public bool IsSlash;
        public bool IsCrit;
        public bool IsMagic;
        public DamageElement Element;
        public float CritChanceBonus;
        public PerkBehaviourConfig SourcePerk;
    }

    public struct KillingBlow
    {
        public EcsPackedEntity Source;
        public Vector3 SourcePosition;
        public bool IsSlash;
        public bool IsEmpowered;
        public DamageElement Element;
    }

    public struct DamageApplied
    {
        public float Amount;
        public bool OnEnemy;
        public int TargetKey;
        public Vector3 Anchor;
    }

    public struct DamageEvaded
    {
        public int TargetKey;
        public Vector3 Anchor;
    }

    public struct CombatStats
    {
        public float CritChance;
        public float CritChanceBase;
        public float CritMultiplier;
        public float Armor;
        public float ArmorBase;
        public float MagicResist;
        public float MagicResistBase;
        public float DefenseScale;
        public float Luck;
        public float LuckBase;
        public float Evasion;
        public float EvasionBase;
        public float MaxEvasion;
        public float AttackPower;
        public float AttackPowerBase;
        public float SpellPower;
        public float SpellPowerBase;
        public float DamageDealt;
        public float DamageDealtBase;
    }

    public struct CombatRandom { public uint State; }

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
        public bool IsMagic;
        public DamageElement Element;
        public PerkBehaviourConfig SourcePerk;
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
