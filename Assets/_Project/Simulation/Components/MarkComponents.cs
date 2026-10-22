using Game.Configs;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct HunterMark
    {
        public int RemainingTicks;
        public float DamageTakenBonus;
        public float CritChanceBonus;
        public EcsPackedEntity Fx;
    }

    public struct MarkFx { public EcsPackedEntity Target; }

    public struct ExplosiveArrowRandom { public uint State; }

    public struct ArrowRainCooldown { public int RemainingTicks; }

    public struct LightningArrowCooldown { public int RemainingTicks; }

    public struct ChakramCooldown { public int RemainingTicks; }

    public struct Chakram : IEcsAutoReset<Chakram>
    {
        public const int InitialHitCapacity = 32;

        public ChakramConfig Config;
        public EcsPackedEntity Owner;
        public float StartAngleDegrees;
        public int ElapsedTicks;
        public int TotalTicks;
        public float Damage;
        public float BleedTickDamage;
        public int BleedTicks;
        public bool Returns;
        public float ReturnDamage;
        public EcsPackedEntity[] Hits;
        public int HitCount;

        public void AutoReset(ref Chakram c)
        {
            if (c.Hits == null)
                c.Hits = new EcsPackedEntity[InitialHitCapacity];

            c.Config = null;
            c.Owner = default;
            c.StartAngleDegrees = 0f;
            c.ElapsedTicks = 0;
            c.TotalTicks = 0;
            c.Damage = 0f;
            c.BleedTickDamage = 0f;
            c.BleedTicks = 0;
            c.Returns = false;
            c.ReturnDamage = 0f;
            c.HitCount = 0;
        }
    }

    public struct LightningArrowRandom { public uint State; }

    public struct LightningChain
    {
        public LightningArrowConfig Config;
        public EcsPackedEntity Owner;
        public Vector3 From;
        public EcsPackedEntity[] Hits;
        public int HitCount;
        public int JumpsLeft;
        public int TicksToJump;
        public float Damage;
    }

    public struct ArrowRainRandom { public uint State; }

    public struct BombardmentRandom { public uint State; }

    public struct ArrowRainImpact
    {
        public ArrowRainConfig Config;
        public EcsPackedEntity Owner;
        public EcsPackedEntity Target;
        public int DelayTicks;
        public int FallTicks;
        public int LingerTicks;
        public float Damage;
        public float BowDamage;
    }

    public struct KillShotCooldown { public int RemainingTicks; }

    public struct KillShotAim
    {
        public EcsPackedEntity Target;
        public int Ticks;
    }

    public struct KillShot : IEcsAutoReset<KillShot>
    {
        public const int InitialHitCapacity = 32;

        public KillShotConfig Config;
        public EcsPackedEntity Owner;
        public EcsPackedEntity Target;
        public Vector3 Direction;
        public float Damage;
        public float PierceDamage;
        public float ExecuteThreshold;
        public int RemainingTicks;
        public EcsPackedEntity[] Hits;
        public int HitCount;

        public void AutoReset(ref KillShot c)
        {
            if (c.Hits == null)
                c.Hits = new EcsPackedEntity[InitialHitCapacity];

            c.Config = null;
            c.Owner = default;
            c.Target = default;
            c.Direction = Vector3.forward;
            c.Damage = 0f;
            c.PierceDamage = 0f;
            c.ExecuteThreshold = 0f;
            c.RemainingTicks = 0;
            c.HitCount = 0;
        }
    }
}
