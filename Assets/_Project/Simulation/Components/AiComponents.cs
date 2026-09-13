using Leopotam.EcsLite;

namespace Game.Simulation.Components
{
    public struct ChaseTarget { public EcsPackedEntity Value; }

    public struct Separation { public float Radius; public float Strength; }
}
