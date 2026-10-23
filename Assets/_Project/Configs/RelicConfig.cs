using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "RelicConfig", menuName = "Game/Meta/Relic Config")]
    public sealed class RelicConfig : ContentConfig
    {
        [SerializeField] private BossConfig _boss;
        [SerializeField] private string _displayName;
        [SerializeField] private string _bossName;
        [SerializeField] private string _echo;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private int _menuOrder;
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(1)] private int _maxLevel = 5;
        [SerializeField, Min(0)] private int _upgradeCostPerLevel = 80;
        [SerializeField] private MetaEffect _effect = new MetaEffect();

        public BossConfig Boss => _boss;
        public string DisplayName => _displayName;
        public string BossName => _bossName;
        public string Echo => _echo;
        public string Description => _description;
        public IReadOnlyList<string> Tags => Effect.GrantedPerk != null && Effect.GrantedPerk.Behaviour != null ? Effect.GrantedPerk.Behaviour.Tags : Array.Empty<string>();
        public int MenuOrder => _menuOrder;
        public Sprite Icon => _icon;
        public int MaxLevel => _maxLevel;
        public MetaEffect Effect => _effect ?? new MetaEffect();

        public int UpgradeCost(int currentLevel)
        {
            return _upgradeCostPerLevel * currentLevel;
        }

        public void Validate(List<string> errors)
        {
            if (_boss == null)
                errors.Add($"{Id}: no boss");

            Effect.Validate(Id, errors);

            PerkConfig perk = Effect.GrantedPerk;

            if (perk != null && perk.MaxLevel < _maxLevel)
                errors.Add($"{Id}: grants '{perk.Id}' with {perk.MaxLevel} levels for {_maxLevel} relic levels");
        }
    }
}
