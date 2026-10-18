using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "Rune", menuName = "Game/Content/Rune Config")]
    public sealed class RuneConfig : ContentConfig
    {
        [SerializeField] private string _displayName;
        [SerializeField] private GameObject _viewPrefab;
        [SerializeField] private Color _color = Color.white;
        [SerializeField, Min(0.1f)] private float _durationSeconds = 10f;
        [SerializeField] private StatModifierEntry[] _modifiers = Array.Empty<StatModifierEntry>();

        [Header("Regeneration")]
        [SerializeField, Range(0f, 1f)] private float _regenFraction;
        [SerializeField, Min(0.05f)] private float _regenIntervalSeconds = 0.5f;

        public string DisplayName => _displayName;

        public GameObject ViewPrefab => _viewPrefab;

        public Color Color => _color;

        public float DurationSeconds => _durationSeconds;

        public IReadOnlyList<StatModifierEntry> Modifiers => _modifiers ?? Array.Empty<StatModifierEntry>();

        public float RegenFraction => _regenFraction;

        public float RegenIntervalSeconds => _regenIntervalSeconds;

        public string ModifierSource => "rune." + Id;

        public void Validate(string owner)
        {
            string label = $"{owner} rune '{Id}'";

            if (string.IsNullOrWhiteSpace(Id))
                throw new InvalidOperationException($"{owner} has a rune without an id.");

            if (_viewPrefab == null)
                throw new InvalidOperationException($"{label} has no view prefab.");

            if (Modifiers.Count == 0 && _regenFraction <= 0f)
                throw new InvalidOperationException($"{label} has neither stat modifiers nor regeneration; picking it up would do nothing.");

            for (int index = 0; index < Modifiers.Count; index++)
            {
                if (Modifiers[index] == null)
                    throw new InvalidOperationException($"{label} has an empty modifier at index {index}.");
            }
        }
    }
}
