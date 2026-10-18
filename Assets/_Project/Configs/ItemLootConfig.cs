using Game.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ItemLoot", menuName = "Game/Content/Item Loot Config")]
    public sealed class ItemLootConfig : ContentConfig
    {
        public const int RarityCount = 4;

        [SerializeField] private int _offerCount = 4;

        [Header("Drop frequency")]
        [SerializeField, Range(0f, 1f)] private float _dropChance = 0.35f;
        [SerializeField, Min(0f)] private float _minDropIntervalSeconds = 40f;
        [SerializeField, Min(1)] private int _maxPendingDrops = 1;
        [SerializeField] private WeightedItem[] _items = Array.Empty<WeightedItem>();

        [Header("Weights: common, rare, epic, legendary")]
        [SerializeField] private float[] _rarityWeights = { 60f, 28f, 10f, 2f };
        [SerializeField] private int[] _minorBonusesByRarity = { 0, 1, 2, 3 };

        [Header("Weights: rank 1..5")]
        [SerializeField] private float[] _rankWeights = { 35f, 30f, 20f, 10f, 5f };

        [Header("Progression: the lowest rank rises by one every N loot windows")]
        [SerializeField, Min(0)] private int _rankFloorEveryWindows = 3;

        [Header("Minor bonuses")]
        [SerializeField] private MinorBonus[] _minorPool = Array.Empty<MinorBonus>();

        public int OfferCount => _offerCount;

        public float DropChance => _dropChance;

        public float MinDropIntervalSeconds => _minDropIntervalSeconds;

        public int MaxPendingDrops => _maxPendingDrops;

        public IReadOnlyList<WeightedItem> Items => _items;

        public IReadOnlyList<MinorBonus> MinorPool => _minorPool;

        public int RankFloor(int windows)
        {
            if (_rankFloorEveryWindows <= 0)
                return 1;

            return Mathf.Min(ItemConfig.RankCount, 1 + windows / _rankFloorEveryWindows);
        }

        public float RarityWeight(Rarity rarity)
        {
            return _rarityWeights[(int)rarity];
        }

        public int MinorBonuses(Rarity rarity)
        {
            return _minorBonusesByRarity[(int)rarity];
        }

        public float RankWeight(int rank)
        {
            return _rankWeights[rank - 1];
        }

        public void Validate(string owner)
        {
            string label = $"{owner} item loot '{Id}'";

            if (_offerCount < 1)
                throw new InvalidOperationException($"{label} offers {_offerCount} items; at least one.");

            if (_items == null || _items.Length == 0)
                throw new InvalidOperationException($"{label} has no items.");

            for (int index = 0; index < _items.Length; index++)
            {
                if (_items[index] == null || _items[index].Item == null)
                    throw new InvalidOperationException($"{label} has an empty item at index {index}.");

                if (_items[index].Weight <= 0f)
                    throw new InvalidOperationException($"{label} item '{_items[index].Item.Id}' has a non-positive weight.");

                _items[index].Item.Validate(label);
            }

            if (_rarityWeights == null || _rarityWeights.Length != RarityCount || _minorBonusesByRarity == null || _minorBonusesByRarity.Length != RarityCount)
                throw new InvalidOperationException($"{label} needs {RarityCount} rarity weights and {RarityCount} minor bonus counts.");

            if (_rankWeights == null || _rankWeights.Length != ItemConfig.RankCount)
                throw new InvalidOperationException($"{label} needs {ItemConfig.RankCount} rank weights.");

            for (int index = 0; index < _minorBonusesByRarity.Length; index++)
            {
                if (_minorBonusesByRarity[index] > _minorPool.Length)
                    throw new InvalidOperationException($"{label} gives {_minorBonusesByRarity[index]} minor bonuses but the pool has only {_minorPool.Length}.");
            }
        }
    }

    [Serializable]
    public sealed class WeightedItem
    {
        [SerializeField] private ItemConfig _item;
        [SerializeField] private float _weight = 1f;

        public ItemConfig Item => _item;

        public float Weight => _weight;
    }

    [Serializable]
    public sealed class MinorBonus
    {
        [SerializeField] private StatId _stat;
        [SerializeField] private StatOp _op = StatOp.Increased;
        [SerializeField] private float _min = 0.02f;
        [SerializeField] private float _max = 0.06f;

        public StatId Stat => _stat;

        public StatOp Op => _op;

        public float Min => _min;

        public float Max => _max;
    }
}
