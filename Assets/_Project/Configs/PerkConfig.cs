using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "PerkConfig", menuName = "Game/Content/Perk Config")]
    public sealed class PerkConfig : ContentConfig
    {
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private PerkLevel[] _levels;

        [Header("Behaviour")]
        [SerializeField] private CounterStrikeConfig _counterStrike;

        public string DisplayName => _displayName;

        public CounterStrikeConfig CounterStrike => _counterStrike;

        public bool HasBehaviour => _counterStrike != null;

        public string Description => _description;

        public int MaxLevel => _levels == null ? 0 : _levels.Length;

        public PerkLevel Level(int level)
        {
            if (level < 1 || level > MaxLevel)
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"{nameof(PerkConfig)} '{Id}' has levels 1..{MaxLevel}.");

            return _levels[level - 1];
        }
    }

    [Serializable]
    public sealed class PerkLevel
    {
        [SerializeField] private StatModifierEntry[] _modifiers;

        public IReadOnlyList<StatModifierEntry> Modifiers => _modifiers;
    }
}
