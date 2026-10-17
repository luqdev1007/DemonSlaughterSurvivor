using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "PerkConfig", menuName = "Game/Content/Perk Config")]
    public sealed class PerkConfig : ContentConfig
    {
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Rarity _rarity;
        [SerializeField] private PerkLevel[] _levels;

        [Header("Behaviour")]
        [SerializeField, FormerlySerializedAs("_counterStrike")] private PerkBehaviourConfig _behaviour;

        [Header("Combo")]
        [SerializeField] private PerkConfig[] _requires = Array.Empty<PerkConfig>();

        public string DisplayName => _displayName;

        public Sprite Icon => _icon;

        public Rarity Rarity => _rarity;

        public PerkBehaviourConfig Behaviour => _behaviour;

        public bool HasBehaviour => _behaviour != null;

        public IReadOnlyList<PerkConfig> Requires => _requires ?? Array.Empty<PerkConfig>();

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
