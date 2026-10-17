using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "LowHealthFuryConfig", menuName = "Game/Content/Low Health Fury Config")]
    public sealed class LowHealthFuryConfig : ScriptableObject
    {
        [SerializeField, Range(0.01f, 1f)] private float _step = 0.1f;

        [Header("Bonus at zero health, as More")]
        [SerializeField, Min(0f)] private float _weaponDamage = 0.6f;
        [SerializeField, Min(0f)] private float _attackSpeed = 0.4f;
        [SerializeField, Min(0f)] private float _moveSpeed = 0.25f;
        [SerializeField, Min(0f)] private float _lifeSteal = 1f;

        public float Step => _step;

        public float WeaponDamage => _weaponDamage;

        public float AttackSpeed => _attackSpeed;

        public float MoveSpeed => _moveSpeed;

        public float LifeSteal => _lifeSteal;

        public void Validate(string owner)
        {
            string label = $"{owner} low health fury '{name}'";

            if (float.IsNaN(_step) || _step <= 0f || _step > 1f)
                throw new InvalidOperationException($"{label} has step {_step}; it must be in (0, 1].");

            RequireNonNegative(label, nameof(_weaponDamage), _weaponDamage);
            RequireNonNegative(label, nameof(_attackSpeed), _attackSpeed);
            RequireNonNegative(label, nameof(_moveSpeed), _moveSpeed);
            RequireNonNegative(label, nameof(_lifeSteal), _lifeSteal);
        }

        private static void RequireNonNegative(string label, string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new InvalidOperationException($"{label} has {field} {value}; it must be zero or positive.");
        }
    }
}
