using Game.Configs;
using Game.Core;
using System.Collections.Generic;

namespace Game.Simulation.Services
{
    public sealed class ItemChoiceState
    {
        public readonly List<ItemOfferData> Offers = new List<ItemOfferData>();
        public readonly List<ItemSlot> Slots = new List<ItemSlot>();

        public static string ModifierSource(ItemSlot slot)
        {
            switch (slot)
            {
                case ItemSlot.Weapon:
                    return "item.weapon";
                case ItemSlot.Armor:
                    return "item.armor";
                case ItemSlot.Ring:
                    return "item.ring";
                case ItemSlot.Boots:
                    return "item.boots";
                default:
                    return "item.amulet";
            }
        }
    }
}
