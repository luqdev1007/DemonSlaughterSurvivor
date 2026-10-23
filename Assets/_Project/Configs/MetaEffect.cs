using Game.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class MetaEffect
    {
        [SerializeField] private StatModifierEntry[] _modifiers = Array.Empty<StatModifierEntry>();
        [SerializeField] private TuningEntry[] _tuning = Array.Empty<TuningEntry>();
        [SerializeField] private PerkConfig _grantedPerk;
        [SerializeField] private bool _notImplemented;

        public IReadOnlyList<StatModifierEntry> Modifiers => _modifiers ?? Array.Empty<StatModifierEntry>();
        public IReadOnlyList<TuningEntry> Tuning => _tuning ?? Array.Empty<TuningEntry>();
        public PerkConfig GrantedPerk => _grantedPerk;
        public bool NotImplemented => _notImplemented;

        public void Validate(string label, List<string> errors)
        {
            IReadOnlyList<StatModifierEntry> modifiers = Modifiers;

            for (int index = 0; index < modifiers.Count; index++)
            {
                if (modifiers[index] == null)
                {
                    errors.Add($"{label}: empty modifier slot {index}");
                    continue;
                }

                for (int other = 0; other < index; other++)
                {
                    if (modifiers[other] != null && modifiers[other].Stat == modifiers[index].Stat)
                        errors.Add($"{label}: lists {modifiers[index].Stat} twice; both share one source, the second would replace the first");
                }
            }

            IReadOnlyList<TuningEntry> tuning = Tuning;

            for (int index = 0; index < tuning.Count; index++)
            {
                TuningEntry entry = tuning[index];

                if (entry == null)
                {
                    errors.Add($"{label}: empty tuning slot {index}");
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Key))
                    errors.Add($"{label}: tuning {index} has neither a target nor a tag");

                if (string.IsNullOrEmpty(entry.Parameter))
                    errors.Add($"{label}: tuning {index} has no parameter");
            }
        }
    }

    [Serializable]
    public sealed class TuningEntry
    {
        public const string TagPrefix = "tag.";

        [SerializeField] private ContentConfig _target;
        [SerializeField] private string _tag;
        [SerializeField] private string _parameter;
        [SerializeField] private StatOp _op = StatOp.Increased;
        [SerializeField] private float _value;

        public string Key
        {
            get
            {
                if (string.IsNullOrEmpty(_tag) == false)
                    return TagPrefix + _tag;

                return _target == null ? null : _target.Id;
            }
        }

        public string Parameter => _parameter;
        public StatOp Op => _op;
        public float Value => _value;
    }
}
