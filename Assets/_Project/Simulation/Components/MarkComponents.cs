using Game.Configs;
using Leopotam.EcsLite;

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
