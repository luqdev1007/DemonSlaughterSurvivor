namespace Game.Core
{
    public readonly struct StatModifierSpec
    {
        public StatModifierSpec(StatId stat, StatOp op, float value, string sourceId)
        {
            Stat = stat;
            Op = op;
            Value = value;
            SourceId = sourceId;
        }

        public StatId Stat { get; }
        public StatOp Op { get; }
        public float Value { get; }
        public string SourceId { get; }
    }
}
