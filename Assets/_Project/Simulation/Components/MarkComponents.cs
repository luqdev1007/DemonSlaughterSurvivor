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
}
