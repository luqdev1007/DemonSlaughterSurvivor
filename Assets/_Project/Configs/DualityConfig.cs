using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "DualityConfig", menuName = "Game/Content/Duality Config")]
    public sealed class DualityConfig : ContentConfig
    {
        [SerializeField, Range(0f, 1f)] private float _startDemonic = 0.5f;
        [SerializeField, Range(0.5f, 1f)] private float _formThreshold = 0.6f;

        [Header("Demonic gain")]
        [SerializeField, Min(0f)] private float _damageTakenScale = 0.6f;
        [SerializeField, Min(0f)] private float _perCrit = 0.015f;
        [SerializeField, Min(0f)] private float _maxCritGainPerTick = 0.05f;
        [SerializeField, Min(0f)] private float _perBackleap = 0.08f;

        [Header("Human gain")]
        [SerializeField, Min(0f)] private float _aimStillPerSecond = 0.06f;
        [SerializeField, Min(0f)] private float _calmDelaySeconds = 3f;
        [SerializeField, Min(0f)] private float _calmPerSecond = 0.015f;
        [SerializeField, Min(0f)] private float _healScale = 0.6f;

        public float StartDemonic => _startDemonic;

        public float FormThreshold => _formThreshold;

        public float DamageTakenScale => _damageTakenScale;

        public float PerCrit => _perCrit;

        public float MaxCritGainPerTick => _maxCritGainPerTick;

        public float PerBackleap => _perBackleap;

        public float AimStillPerSecond => _aimStillPerSecond;

        public float CalmDelaySeconds => _calmDelaySeconds;

        public float CalmPerSecond => _calmPerSecond;

        public float HealScale => _healScale;

        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
                throw new InvalidOperationException($"{nameof(DualityConfig)} '{name}' has no id.");
        }
    }
}
