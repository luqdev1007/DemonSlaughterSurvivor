using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "BlinkConfig", menuName = "Game/Content/Blink Config")]
    public sealed class BlinkConfig : ContentConfig, IAbilityIcon
    {
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 3f;
        [SerializeField, Min(0.5f)] private float _maxDistance = 7f;
        [SerializeField, Min(0.01f)] private float _probeStep = 0.1f;

        [Header("View")]
        [SerializeField] private GameObject _departFxPrefab;
        [SerializeField] private GameObject _arriveFxPrefab;
        [SerializeField, Min(0.05f)] private float _fxSeconds = 1f;

        public Sprite Icon => _icon;

        public float CooldownSeconds => _cooldownSeconds;

        public float MaxDistance => _maxDistance;

        public float ProbeStep => _probeStep;

        public GameObject DepartFxPrefab => _departFxPrefab;

        public GameObject ArriveFxPrefab => _arriveFxPrefab;

        public float FxSeconds => _fxSeconds;

        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
                throw new InvalidOperationException($"{nameof(BlinkConfig)} '{name}' has no id.");

            if (_probeStep >= _maxDistance)
                throw new InvalidOperationException(
                    $"{nameof(BlinkConfig)} '{Id}' probes in {_probeStep} m steps over a {_maxDistance} m blink; a blocked target could only fall back to the start.");
        }
    }
}
