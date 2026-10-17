using Game.Core;
using System;
using UnityEngine;

namespace Game.Configs
{
    public enum ItemSlot
    {
        Weapon = 0,
        Armor = 1,
        Ring = 2,
        Boots = 3,
        Amulet = 4
    }

    [CreateAssetMenu(fileName = "Item", menuName = "Game/Content/Item Config")]
    public sealed class ItemConfig : ContentConfig
    {
        public const int RankCount = 5;

        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private ItemSlot _slot;

        [Header("Main stat by rank 1..5")]
        [SerializeField] private StatId _stat;
        [SerializeField] private StatOp _op = StatOp.Increased;
        [SerializeField] private float[] _rankValues = { 0.08f, 0.12f, 0.16f, 0.2f, 0.25f };

        public string DisplayName => _displayName;

        public Sprite Icon => _icon;

        public ItemSlot Slot => _slot;

        public StatId Stat => _stat;

        public StatOp Op => _op;

        public float Value(int rank)
        {
            return _rankValues[Mathf.Clamp(rank, 1, RankCount) - 1];
        }

        public void Validate(string owner)
        {
            string label = $"{owner} item '{Id}'";

            if (string.IsNullOrWhiteSpace(Id))
                throw new InvalidOperationException($"{owner} has an item without an id.");

            if (_rankValues == null || _rankValues.Length != RankCount)
                throw new InvalidOperationException($"{label} must have exactly {RankCount} rank values.");

            for (int index = 0; index < _rankValues.Length; index++)
            {
                if (float.IsNaN(_rankValues[index]) || float.IsInfinity(_rankValues[index]))
                    throw new InvalidOperationException($"{label} has an invalid value at rank {index + 1}.");
            }
        }
    }
}
