using Game.Configs;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct Weapon { public WeaponConfig Config; }

    public struct WeaponDamage { public float Value; public float Base; }

    public struct WeaponCooldown { public float Value; public float Base; }

    public struct AttackSpeed { public float Value; public float Base; }

    public struct WeaponReady { public float Remaining; }

    public struct SwingRandom { public uint State; }

    public struct Swinging { }

    public struct SwingStarted { }

    public struct SpecialSwing { }

    public struct LeapStarted { }

    public struct HeroicLeap
    {
        public EcsPackedEntity Owner;
        public EcsPackedEntity Weapon;
        public HeroicLeapConfig Config;
        public float DamageScale;
        public int ElapsedTicks;
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
        public float ClipTime;
        public float PlaybackSpeed;
        public int RemainingTicks;
        public Vector3 PreviousHand;
        public Vector3 PreviousTip;
        public EcsPackedEntity[] Hits;
        public int HitCount;

        public void AutoReset(ref Swing c)
        {
            if (c.Hits == null)
                c.Hits = new EcsPackedEntity[InitialHitCapacity];

            c.Weapon = default;
            c.Owner = default;
            c.Variant = null;
            c.DamageScale = 0f;
            c.Kind = DamageKind.Unmarked;
            c.ClipTime = 0f;
            c.PlaybackSpeed = 0f;
            c.RemainingTicks = 0;
            c.PreviousHand = default;
            c.PreviousTip = default;
            c.HitCount = 0;
        }
    }
}
