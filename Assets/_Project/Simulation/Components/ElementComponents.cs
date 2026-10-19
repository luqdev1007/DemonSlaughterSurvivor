using Game.Configs;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public enum StatusKind
    {
        Burning = 0,
        Marked = 1,
    }

    public struct StatusFx
    {
        public EcsPackedEntity Owner;
        public StatusKind Kind;
        public float Height;
    }

    public struct Burning
    {
        public SearingFireballConfig Config;
        public EcsPackedEntity Source;
        public float TickDamage;
        public float CritChanceBonus;
        public int RemainingTicks;
        public int TickCountdown;
        public bool HasFx;
    }

    public struct Slowed
    {
        public float Factor;
        public int RemainingTicks;
    }

    public struct ElementMark
    {
        public ElementalWeaknessConfig Config;
        public EcsPackedEntity Source;
        public DamageElement Element;
        public float DamageBonus;
        public float ExplosionDamage;
        public int RemainingTicks;
        public bool HasFx;
    }

    public struct ElementalWeaknessState
    {
        public int Countdown;
        public uint Random;
    }

    public struct FireballVolleyCooldown { public int RemainingTicks; }

    public struct FireballVolley
    {
        public EcsPackedEntity Owner;
        public int Variant;
        public int Remaining;
        public int TicksToNext;
        public int Index;
        public float Damage;
    }

    public struct BlinkEcho { public float Remaining; }

    public struct ArcaneWaveCooldown { public int RemainingTicks; }

    public struct ArcaneWave : IEcsAutoReset<ArcaneWave>
    {
        public const int InitialHitCapacity = 32;

        public ArcaneWaveConfig Config;
        public EcsPackedEntity Owner;
        public Vector3 Origin;
        public float Radius;
        public float MaxRadius;
        public float Damage;
        public float AffectedBonus;
        public EcsPackedEntity[] Hits;
        public int HitCount;

        public void AutoReset(ref ArcaneWave c)
        {
            if (c.Hits == null)
                c.Hits = new EcsPackedEntity[InitialHitCapacity];

            c.Config = null;
            c.Owner = default;
            c.Origin = default;
            c.Radius = 0f;
            c.MaxRadius = 0f;
            c.Damage = 0f;
            c.AffectedBonus = 0f;
            c.HitCount = 0;
        }
    }

    public struct LightningShield
    {
        public int Countdown;
        public EcsPackedEntity Aura;
        public bool HasAura;
    }

    public struct LightningAuraFx { public EcsPackedEntity Owner; }

    public struct TornadoCooldown { public int RemainingTicks; }

    public struct TornadoRandom { public uint State; }

    public struct Tornado
    {
        public TornadoConfig Config;
        public EcsPackedEntity Owner;
        public Vector3 Heading;
        public float Damage;
        public int RemainingTicks;
        public int PulseCountdown;
        public int TurnCountdown;
        public uint Random;
    }
}
