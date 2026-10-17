using Game.Configs;

namespace Game.UI
{
    public static class ItemSlotText
    {
        public static string Name(ItemSlot slot)
        {
            switch (slot)
            {
                case ItemSlot.Weapon:
                    return "Оружие";
                case ItemSlot.Armor:
                    return "Броня";
                case ItemSlot.Ring:
                    return "Кольцо";
                case ItemSlot.Boots:
                    return "Сапоги";
                default:
                    return "Амулет";
            }
        }
    }
}
