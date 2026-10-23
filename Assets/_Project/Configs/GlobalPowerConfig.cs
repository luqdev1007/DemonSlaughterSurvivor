using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "GlobalPowerConfig", menuName = "Game/Meta/Global Power Config")]
    public sealed class GlobalPowerConfig : ContentConfig
    {
        [SerializeField] private MetaPassive[] _passives = Array.Empty<MetaPassive>();
        [SerializeField, Min(1)] private int _maxRank = 5;
        [SerializeField, Min(0)] private int _rankCostBase = 60;

        [Header("Paragon")]
        [SerializeField] private MetaPassive[] _paragon = Array.Empty<MetaPassive>();
        [SerializeField, Min(0)] private int _paragonUnlockRanks = 20;
        [SerializeField, Min(0f)] private float _paragonCostBase = 200f;
        [SerializeField, Min(1f)] private float _paragonCostGrowth = 1.06f;

        public IReadOnlyList<MetaPassive> Passives => _passives ?? Array.Empty<MetaPassive>();
        public IReadOnlyList<MetaPassive> Paragon => _paragon ?? Array.Empty<MetaPassive>();
        public int MaxRank => _maxRank;
        public int ParagonUnlockRanks => _paragonUnlockRanks;

        public int RankCost(int currentRank)
        {
            return _rankCostBase * (currentRank + 1);
        }

        public int ParagonCost(int currentRank)
        {
            return Mathf.RoundToInt(_paragonCostBase * Mathf.Pow(_paragonCostGrowth, currentRank));
        }

        public MetaPassive FindPassive(string id)
        {
            return Find(Passives, id);
        }

        public MetaPassive FindParagon(string id)
        {
            return Find(Paragon, id);
        }

        public void Validate(List<string> errors)
        {
            MetaIds.ValidateUnique(Id + " passives", Passives, passive => passive?.Id, errors);
            MetaIds.ValidateUnique(Id + " paragon", Paragon, passive => passive?.Id, errors);

            foreach (MetaPassive passive in Passives)
                passive?.Effect.Validate($"{Id} passive '{passive.Id}'", errors);

            foreach (MetaPassive passive in Paragon)
                passive?.Effect.Validate($"{Id} paragon '{passive.Id}'", errors);
        }

        private static MetaPassive Find(IReadOnlyList<MetaPassive> list, string id)
        {
            for (int index = 0; index < list.Count; index++)
            {
                if (list[index] != null && list[index].Id == id)
                    return list[index];
            }

            return null;
        }
    }

    [Serializable]
    public sealed class MetaPassive
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private float _displayPerRank;
        [SerializeField] private string _displayUnit;
        [SerializeField] private Sprite _icon;
        [SerializeField] private MetaEffect _effect = new MetaEffect();

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public float DisplayPerRank => _displayPerRank;
        public string DisplayUnit => _displayUnit;
        public Sprite Icon => _icon;
        public MetaEffect Effect => _effect ?? new MetaEffect();
    }
}
