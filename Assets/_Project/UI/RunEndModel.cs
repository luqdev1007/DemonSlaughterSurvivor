using System.Collections.Generic;

namespace Game.UI
{
    public sealed class RunEndModel
    {
        public bool Victory;
        public string Title;
        public string Subtitle;
        public int MemoryShards;
        public readonly List<(string Label, string Value)> MemoryLines = new List<(string, string)>();
        public string MemoryNote;
        public int MagicShards;
        public readonly List<(string Label, string Value)> MagicLines = new List<(string, string)>();
        public string MagicNote;
        public bool RelicActive;
        public string RelicEyebrow;
        public string RelicName;
        public string RelicText;
        public readonly List<(string Label, string Value)> Stats = new List<(string, string)>();
    }
}
