namespace Game.Core
{
    public readonly struct TuningSpec
    {
        public TuningSpec(string key, string parameter, StatOp op, float value, string sourceId)
        {
            Key = key;
            Parameter = parameter;
            Op = op;
            Value = value;
            SourceId = sourceId;
        }

        public string Key { get; }
        public string Parameter { get; }
        public StatOp Op { get; }
        public float Value { get; }
        public string SourceId { get; }
    }
}
