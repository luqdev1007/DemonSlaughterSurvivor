using System.Collections.Generic;

namespace Game.Core
{
    public readonly struct StatLine
    {
        public StatLine(StatId stat, StatOp op, float value)
        {
            Stat = stat;
            Op = op;
            Value = value;
        }

        public StatId Stat { get; }

        public StatOp Op { get; }

        public float Value { get; }
    }

    public sealed class ItemOfferData
    {
        public readonly List<StatLine> Minors = new List<StatLine>();

        public string ItemId;
        public int Rank;
        public int Rarity;
        public StatLine Main;
        public ItemOfferData Equipped;
    }
}
