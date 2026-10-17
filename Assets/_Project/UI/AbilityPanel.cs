using Game.Core;
using System.Collections.Generic;

namespace Game.UI
{
    public sealed class AbilityPanel : IAbilityPanelSink
    {
        private readonly List<AbilityPanelEntry> _entries = new List<AbilityPanelEntry>();
        private readonly Dictionary<string, int> _indices = new Dictionary<string, int>();

        public int Count => _entries.Count;

        public AbilityPanelEntry Entry(int index)
        {
            return _entries[index];
        }

        public void Publish(string abilityId, float remainingSeconds)
        {
            if (_indices.TryGetValue(abilityId, out int index) == false)
            {
                index = _entries.Count;
                _indices.Add(abilityId, index);
                _entries.Add(new AbilityPanelEntry(abilityId));
            }

            _entries[index].Update(remainingSeconds);
        }
    }

    public sealed class AbilityPanelEntry
    {
        public AbilityPanelEntry(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public float Remaining { get; private set; }

        public float Peak { get; private set; }

        public float Fill => Peak > 0f ? Remaining / Peak : 0f;

        public void Update(float remaining)
        {
            if (remaining > Remaining + 1e-4f || remaining <= 0f)
                Peak = remaining;

            Remaining = remaining;
        }
    }
}
