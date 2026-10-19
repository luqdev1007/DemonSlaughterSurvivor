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

    public struct ArrowRainImpact
    {
        public ArrowRainConfig Config;
        public EcsPackedEntity Owner;
        public EcsPackedEntity Target;
        public int DelayTicks;
        public int FallTicks;
        public int LingerTicks;
        public float Damage;
    }
}
