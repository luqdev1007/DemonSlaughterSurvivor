using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ConcentrationConfig", menuName = "Game/Content/Concentration Config")]
    public sealed class ConcentrationConfig : ContentConfig
    {
        [SerializeField, Min(1f)] private float _max = 100f;
        [SerializeField, Min(0f)] private float _start = 100f;

        public float Max => _max;

        public float Start => _start;

        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
                throw new InvalidOperationException($"{nameof(ConcentrationConfig)} '{name}' has no id.");

            if (_start > _max)
                throw new InvalidOperationException($"{nameof(ConcentrationConfig)} '{Id}' starts at {_start}, above its maximum {_max}.");
        }
    }
}
