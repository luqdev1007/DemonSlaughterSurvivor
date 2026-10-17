using Game.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "EliteAffix", menuName = "Game/Content/Elite Affix Config")]
    public sealed class EliteAffixConfig : ContentConfig
    {
        [SerializeField] private string _displayName;
        [SerializeField] private StatModifierEntry[] _modifiers = Array.Empty<StatModifierEntry>();
        [SerializeField] private float _scale = 1f;

        [Header("View")]
        [SerializeField] private GameObject _vfxPrefab;
        [SerializeField] private Vector3 _vfxOffset;

        public string DisplayName => _displayName;

        public IReadOnlyList<StatModifierEntry> Modifiers => _modifiers;

        public float Scale => _scale;

        public GameObject VfxPrefab => _vfxPrefab;

        public Vector3 VfxOffset => _vfxOffset;

        public float MoveSpeedFactor
        {
            get
            {
                float increased = 0f;
                float more = 1f;

                for (int index = 0; index < _modifiers.Length; index++)
                {
                    StatModifierEntry entry = _modifiers[index];

                    if (entry == null || entry.Stat != StatId.MoveSpeed)
                        continue;

                    if (entry.Op == StatOp.Increased)
                        increased += entry.Value;
                    else if (entry.Op == StatOp.More)
                        more *= 1f + entry.Value;
                }

                return Mathf.Max(1f, (1f + increased) * more);
            }
        }

        public void Validate(string owner)
        {
            string label = $"{owner} elite affix '{Id}'";

            if (string.IsNullOrWhiteSpace(Id))
                throw new InvalidOperationException($"{owner} has an elite affix without an id.");

            if (float.IsNaN(_scale) || _scale <= 0f)
                throw new InvalidOperationException($"{label} has scale {_scale}; it must be positive.");

            for (int index = 0; index < _modifiers.Length; index++)
            {
                StatModifierEntry entry = _modifiers[index];

                if (entry == null)
                    throw new InvalidOperationException($"{label} has an empty modifier at index {index}.");

                if (entry.Stat == StatId.MoveSpeed && entry.Op == StatOp.Flat)
                    throw new InvalidOperationException(
                        $"{label} adds a flat {nameof(StatId.MoveSpeed)}; use Increased or More, " +
                        "the enemy speed ceiling is derived from multipliers only.");
            }
        }
    }
}
