using Game.Configs;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct Weapon { public WeaponConfig Config; }

    public struct Bow { public BowConfig Config; }

    public struct Staff { public StaffConfig Config; }

    public struct StaffCast { public int RemainingTicks; public int Variant; }

    public struct StaffCastPick { public uint State; public bool HasLast; public bool LastLeft; }

    public struct BowDraw
    {
        public int RemainingTicks;
        public float PlaybackSpeed;
        public bool Empowered;
    }

    public struct WeaponDamage { public float Value; public float Base; }

    public struct WeaponCooldown { public float Value; public float Base; }

    public struct AttackSpeed { public float Value; public float Base; }

    public struct SwingReach { public float Value; public float Base; }

    public struct WeaponReady { public float Remaining; }

    public struct SwingRandom { public uint State; }

    public struct Swinging { }

    public struct SwingStarted { }

    public struct SwingEndedEvent
    {
        public EcsPackedEntity Owner;
        public int EnemyHits;
    }

    public struct SpecialSwing { }

    public struct LeapStarted { }

    public struct LeapLandedEvent
    {
        public Vector3 Position;
        public HeroicLeapConfig Config;
    }

    public struct LeapImpactFx { public int RemainingTicks; }

    public struct FireSegment
    {
        public EcsPackedEntity Owner;
        public int RemainingTicks;
        public float DamageScale;
        public PerkBehaviourConfig SourcePerk;
    }

    public struct FireTrail { public float Carried; }

    public struct HeroicLeap
    {
        public EcsPackedEntity Owner;
        public EcsPackedEntity Weapon;
        public HeroicLeapConfig Config;
        public float DamageScale;
        public int ElapsedTicks;
    }

    public struct Execution
    {
        public EcsPackedEntity Owner;
        public EcsPackedEntity Target;
        public ExecutionConfig Config;
        public int Level;
        public int ElapsedTicks;
        public int NextHit;
    }

    public struct ExecutionStarted { }

    public struct ExecutionFinishedEvent { public EcsPackedEntity Owner; }

    public struct ForceWave : IEcsAutoReset<ForceWave>
    {
        public const int InitialHitCapacity = 32;

        public EcsPackedEntity Owner;
        public ExecutionWavesConfig Config;
        public Vector3 Origin;
        public Vector3 Direction;
        public float Travelled;
        public float NextBurst;
        public float Damage;
        public EcsPackedEntity[] Hits;
        public int HitCount;

        public void AutoReset(ref ForceWave c)
        {
            if (c.Hits == null)
                c.Hits = new EcsPackedEntity[InitialHitCapacity];

            c.Owner = default;
            c.Config = null;
            c.Origin = default;
            c.Direction = default;
            c.Travelled = 0f;
            c.NextBurst = 0f;
            c.Damage = 0f;
            c.HitCount = 0;
        }
    }

    public struct FlameSwingCooldown { public int RemainingTicks; }

    public struct FlameSwing
    {
        public EcsPackedEntity Owner;
        public FlameSwingConfig Config;
        public int Level;
        public int ElapsedTicks;
    }

    public struct FlameSwingStarted { }

    public struct FlameWave : IEcsAutoReset<FlameWave>
    {
        public const int InitialHitCapacity = 32;

        public EcsPackedEntity Owner;
        public FlameSwingConfig Config;
        public Vector3 Origin;
        public Vector3 Direction;
        public float Radius;
        public float MaxRadius;
        public float Damage;
        public EcsPackedEntity[] Hits;
        public int HitCount;

        public void AutoReset(ref FlameWave c)
        {
            if (c.Hits == null)
                c.Hits = new EcsPackedEntity[InitialHitCapacity];

            c.Owner = default;
            c.Config = null;
            c.Origin = default;
            c.Direction = default;
            c.Radius = 0f;
            c.MaxRadius = 0f;
            c.Damage = 0f;
            c.HitCount = 0;
        }
    }

    public struct FlameWavePillar
    {
        public EcsPackedEntity Wave;
        public float AngleDegrees;
    }

    public struct SpecialAttack
    {
        public EcsPackedEntity Attack;
        public bool LocksMovement;
    }

    public struct Swing : IEcsAutoReset<Swing>
    {
        public const int InitialHitCapacity = 64;

        public EcsPackedEntity Weapon;
        public EcsPackedEntity Owner;
        public SwingVariant Variant;
        public float DamageScale;
        public DamageKind Kind;
        public PerkBehaviourConfig SourcePerk;
        public float PushSpeed;
        public int PushTicks;
        public float Reach;
        public bool IsSharp;
        public EcsPackedEntity Target;
        public bool HasTarget;
        public float ClipTime;
        public float PlaybackSpeed;
        public int RemainingTicks;
        public Vector3 PreviousHand;
        public Vector3 PreviousTip;
        public EcsPackedEntity[] Hits;
        public int HitCount;
        public int EnemyHits;

        public void AutoReset(ref Swing c)
        {
            if (c.Hits == null)
                c.Hits = new EcsPackedEntity[InitialHitCapacity];

            c.Weapon = default;
            c.Owner = default;
            c.Variant = null;
            c.DamageScale = 0f;
            c.Kind = DamageKind.Unmarked;
            c.SourcePerk = null;
            c.PushSpeed = 0f;
            c.PushTicks = 0;
            c.Reach = 1f;
            c.IsSharp = false;
            c.Target = default;
            c.HasTarget = false;
            c.ClipTime = 0f;
            c.PlaybackSpeed = 0f;
            c.RemainingTicks = 0;
            c.PreviousHand = default;
            c.PreviousTip = default;
            c.HitCount = 0;
            c.EnemyHits = 0;
        }
    }
}
