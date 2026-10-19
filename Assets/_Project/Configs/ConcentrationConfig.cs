using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ConcentrationConfig", menuName = "Game/Content/Concentration Config")]
    public sealed class ConcentrationConfig : ContentConfig
    {
        [SerializeField, Min(1f)] private float _max = 100f;
        [SerializeField, Min(0f)] private float _start = 100f;

        [Header("Restore")]
        [SerializeField, Min(0f)] private float _regenPerSecond = 4f;
        [SerializeField, Min(0f)] private float _perEliteKill = 25f;

        [Header("Energy shield")]
        [SerializeField, Range(0f, 1f)] private float _shieldAbsorb = 0.5f;
        [SerializeField, Min(0.01f)] private float _shieldCostPerDamage = 1f;

        [Header("Bonuses at full concentration")]
        [SerializeField, Min(0f)] private float _damageBonus = 0.3f;
        [SerializeField, Min(0f)] private float _areaBonus = 0.25f;
        [SerializeField, Min(0f)] private float _castSpeedBonus = 0.4f;

        public float Max => _max;

        public float Start => _start;

        public float RegenPerSecond => _regenPerSecond;

        public float PerEliteKill => _perEliteKill;

        public float ShieldAbsorb => _shieldAbsorb;

        public float ShieldCostPerDamage => _shieldCostPerDamage;

        public float DamageBonus => _damageBonus;

        public float AreaBonus => _areaBonus;

        public float CastSpeedBonus => _castSpeedBonus;

        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
                throw new InvalidOperationException($"{nameof(ConcentrationConfig)} '{name}' has no id.");

            if (_start > _max)
                throw new InvalidOperationException($"{nameof(ConcentrationConfig)} '{Id}' starts at {_start}, above its maximum {_max}.");
        }
    }
}
