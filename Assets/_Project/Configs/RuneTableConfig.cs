using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class WeightedRune
    {
        [SerializeField] private RuneConfig _rune;
        [SerializeField] private float _weight = 1f;

        public RuneConfig Rune => _rune;

        public float Weight => _weight;
    }

    [CreateAssetMenu(fileName = "RuneTable", menuName = "Game/Content/Rune Table Config")]
    public sealed class RuneTableConfig : ContentConfig
    {
        [SerializeField] private WeightedRune[] _runes = Array.Empty<WeightedRune>();

        [Header("Spawn timing")]
        [SerializeField, Min(0f)] private float _firstDelaySeconds = 60f;
        [SerializeField, Min(0f)] private float _minIntervalSeconds = 25f;
        [SerializeField, Min(0f)] private float _maxIntervalSeconds = 45f;

        [Header("Placement")]
        [SerializeField, Min(0f)] private float _minDistanceFromHero = 8f;
        [SerializeField, Min(0f)] private float _maxDistanceFromHero = 20f;
        [SerializeField, Min(0f)] private float _arenaEdgeMargin = 4f;

        [Header("Pickup")]
        [SerializeField, Min(0.1f)] private float _pickupRadius = 1.3f;

        [Header("Announcement: {0} is the rune name")]
        [SerializeField] private string _spawnAnnouncement = "Где-то появилась {0}";

        public IReadOnlyList<WeightedRune> Runes => _runes;

        public float FirstDelaySeconds => _firstDelaySeconds;

        public float MinIntervalSeconds => _minIntervalSeconds;

        public float MaxIntervalSeconds => Mathf.Max(_minIntervalSeconds, _maxIntervalSeconds);

        public float MinDistanceFromHero => _minDistanceFromHero;

        public float MaxDistanceFromHero => Mathf.Max(_minDistanceFromHero, _maxDistanceFromHero);

        public float ArenaEdgeMargin => _arenaEdgeMargin;

        public float PickupRadius => _pickupRadius;

        public string SpawnAnnouncement => _spawnAnnouncement;

        public void Validate(string owner)
        {
            string label = $"{owner} rune table '{Id}'";

            if (_runes == null || _runes.Length == 0)
                throw new InvalidOperationException($"{label} has no runes.");

            for (int index = 0; index < _runes.Length; index++)
            {
                if (_runes[index] == null || _runes[index].Rune == null)
                    throw new InvalidOperationException($"{label} has an empty rune at index {index}.");

                if (_runes[index].Weight <= 0f)
                    throw new InvalidOperationException($"{label} rune '{_runes[index].Rune.Id}' has a non-positive weight.");

                _runes[index].Rune.Validate(label);
            }
        }
    }
}
