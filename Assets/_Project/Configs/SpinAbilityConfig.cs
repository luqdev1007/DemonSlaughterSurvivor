using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "SpinAbilityConfig", menuName = "Game/Content/Spin Ability Config")]
    public sealed class SpinAbilityConfig : ContentConfig
    {
        [Header("Trigger")]
        [SerializeField] private float _triggerRange = 5f;
        [SerializeField] private float _cooldownSeconds = 10f;

        [Header("Phases")]
        [SerializeField] private float _raiseSeconds = 0.8f;
        [SerializeField] private float _spinSeconds = 4f;
        [SerializeField] private float _recoverSeconds = 0.8f;
        [SerializeField] private float _moveSpeedMultiplier = 0.6f;

        [Header("Damage")]
        [SerializeField] private float _radius = 4f;
        [SerializeField] private float _damageAtCenter = 15f;
        [SerializeField] private float _damageAtEdge = 4f;
        [SerializeField] private float _tickSeconds = 0.5f;

        [Header("View")]
        [SerializeField] private float _raiseClipSeconds = 0.8f;
        [SerializeField] private float _spinAngularSpeedDegrees = 540f;
        [SerializeField] private float _armWobbleDegrees = 12f;

        public float TriggerRange => _triggerRange;

        public float CooldownSeconds => _cooldownSeconds;

        public float RaiseSeconds => _raiseSeconds;

        public float SpinSeconds => _spinSeconds;

        public float RecoverSeconds => _recoverSeconds;

        public float MoveSpeedMultiplier => _moveSpeedMultiplier;

        public float Radius => _radius;

        public float DamageAtCenter => _damageAtCenter;

        public float DamageAtEdge => _damageAtEdge;

        public float TickSeconds => _tickSeconds;

        public float RaiseClipSeconds => _raiseClipSeconds;

        public float SpinAngularSpeedDegrees => _spinAngularSpeedDegrees;

        public float ArmWobbleDegrees => _armWobbleDegrees;

        public float DamageAt(float distance)
        {
            return Mathf.Lerp(_damageAtCenter, _damageAtEdge, Mathf.Clamp01(distance / _radius));
        }

        public void Validate(EnemyConfig owner)
        {
            string label = $"{nameof(EnemyConfig)} '{(owner == null ? "<none>" : owner.Id)}' spin '{Id}'";

            RequirePositive(label, nameof(_triggerRange), _triggerRange);
            RequireNonNegative(label, nameof(_cooldownSeconds), _cooldownSeconds);
            RequirePositive(label, nameof(_raiseSeconds), _raiseSeconds);
            RequirePositive(label, nameof(_spinSeconds), _spinSeconds);
            RequirePositive(label, nameof(_recoverSeconds), _recoverSeconds);
            RequirePositive(label, nameof(_moveSpeedMultiplier), _moveSpeedMultiplier);
            RequirePositive(label, nameof(_radius), _radius);
            RequireNonNegative(label, nameof(_damageAtCenter), _damageAtCenter);
            RequireNonNegative(label, nameof(_damageAtEdge), _damageAtEdge);
            RequirePositive(label, nameof(_tickSeconds), _tickSeconds);
            RequirePositive(label, nameof(_raiseClipSeconds), _raiseClipSeconds);
            RequireNonNegative(label, nameof(_spinAngularSpeedDegrees), _spinAngularSpeedDegrees);
            RequireNonNegative(label, nameof(_armWobbleDegrees), _armWobbleDegrees);
        }

        private static void RequirePositive(string label, string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new InvalidOperationException($"{label} has {field} {value}; it must be positive.");
        }

        private static void RequireNonNegative(string label, string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new InvalidOperationException($"{label} has {field} {value}; it must be zero or positive.");
        }
    }
}
