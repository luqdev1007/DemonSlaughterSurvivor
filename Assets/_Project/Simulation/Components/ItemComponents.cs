using Game.Configs;
using Game.Core;
using Leopotam.EcsLite;

namespace Game.Simulation.Components
{
    public struct PendingItemDrops { public int Count; }

    public struct PendingItemChoice { }

    public struct ItemRandom { public uint State; }

    public struct EquippedItem
    {
        public EcsPackedEntity Owner;
        public ItemSlot Slot;
        public ItemOfferData Roll;
    }
}
