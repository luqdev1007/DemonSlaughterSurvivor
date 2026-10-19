using Leopotam.EcsLite;

namespace Game.Simulation.Components
{
    public struct Frozen { public int RemainingTicks; public bool HasFx; }

    public struct FrozenFx { public EcsPackedEntity Owner; }

    public struct IceRingCooldown { public int RemainingTicks; }
}
