using Game.Configs;
using Game.Core;
using System;
using System.Collections.Generic;

namespace Game.Simulation.Services
{
    public sealed class Tuning
    {
        public const string TagPrefix = TuningEntry.TagPrefix;

        private readonly TuningSpec[] _specs;
        private readonly Dictionary<PerkBehaviourConfig, string> _perkIds = new Dictionary<PerkBehaviourConfig, string>();

        public Tuning(IReadOnlyList<TuningSpec> specs, IContentRegistry content)
        {
            if (specs == null)
                throw new ArgumentNullException(nameof(specs));

            _specs = new TuningSpec[specs.Count];

            for (int index = 0; index < _specs.Length; index++)
                _specs[index] = specs[index];

            if (content == null)
                return;

            foreach (PerkConfig perk in content.All<PerkConfig>())
            {
                if (perk.Behaviour != null && _perkIds.ContainsKey(perk.Behaviour) == false)
                    _perkIds.Add(perk.Behaviour, perk.Id);
            }
        }

        public int Count => _specs.Length;

        public float Resolve(string key, string parameter, float baseValue)
        {
            StatTotals totals = StatTotals.Neutral;
            bool any = false;

            for (int index = 0; index < _specs.Length; index++)
            {
                ref TuningSpec spec = ref _specs[index];

                if (string.Equals(spec.Key, key, StringComparison.Ordinal) == false || string.Equals(spec.Parameter, parameter, StringComparison.Ordinal) == false)
                    continue;

                StatFormula.Accumulate(ref totals, spec.Op, spec.Value);
                any = true;
            }

            return any ? StatFormula.Evaluate(baseValue, totals) : baseValue;
        }

        public float Resolve(PerkBehaviourConfig behaviour, string parameter, float baseValue)
        {
            if (behaviour == null || _perkIds.TryGetValue(behaviour, out string perkId) == false)
                return baseValue;

            return Resolve(perkId, parameter, baseValue);
        }

        public float ResolveTags(IReadOnlyList<string> tags, string parameter, float baseValue)
        {
            float value = baseValue;

            for (int index = 0; index < tags.Count; index++)
                value = Resolve(TagPrefix + tags[index], parameter, value);

            return value;
        }
    }
}
