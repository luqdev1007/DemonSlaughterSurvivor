using Game.Core;
using Leopotam.EcsLite;

namespace Game.Simulation.Components
{
    public struct StatModifier
    {
        public EcsPackedEntity Target;
        public StatId Stat;
        public StatOp Op;
        public float Value;
        public string SourceId;
    }

    public struct StatsDirty { }

    public struct OwnerLink { public EcsPackedEntity Owner; }
}
