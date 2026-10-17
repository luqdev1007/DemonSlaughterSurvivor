using Game.Core;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "DashAbilityConfig", menuName = "Game/Content/Dash Ability Config")]
    public sealed class DashAbilityConfig : ContentConfig
    {
        [SerializeField] private float _distance;
        [SerializeField] private float _duration;
        [SerializeField] private float _cooldown;
        [SerializeField] private DashDirection _direction;
        [SerializeField] private float _accelerationPower = 3f;

        [Header("Charge")]
        [SerializeField, Min(0f)] private float _tapSeconds = 0.18f;
        [SerializeField] private bool _chargeEnabled;
        [SerializeField, Min(0.01f)] private float _maxChargeSeconds = 1f;
        [SerializeField] private AnimationCurve _chargeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField, Min(1f)] private float _distanceAtFullCharge = 1.8f;
        [SerializeField, Min(1f)] private float _speedAtFullCharge = 1.4f;
        [SerializeField, Min(1f)] private float _damageAtFullCharge = 2f;
        [SerializeField, Range(-0.95f, 0f)] private float _chargeMoveSpeedMore = -0.4f;
        [SerializeField] private bool _chargeRunAnimation;

        [Header("Invulnerability")]
        [SerializeField] private float _invulnerabilitySeconds = 0.3f;

        [Header("Push")]
        [SerializeField] private float _pushSpeed = 5f;
        [SerializeField] private float _pushSeconds = 0.6f;
        [SerializeField] private float _pushArcHeight = 1.2f;
        [SerializeField] private float _pushReach = 1f;
        [SerializeField] private float _pushEdgeScale = 0.5f;
        [SerializeField] private float _pushForwardTiltDegrees = 30f;
        [SerializeField] private float _pushSpreadDegrees = 15f;

        public float TapSeconds => _tapSeconds;

        public bool ChargeEnabled => _chargeEnabled;

        public float MaxChargeSeconds => _maxChargeSeconds;

        public AnimationCurve ChargeCurve => _chargeCurve;

        public float DistanceAtFullCharge => _distanceAtFullCharge;

        public float SpeedAtFullCharge => _speedAtFullCharge;

        public float DamageAtFullCharge => _damageAtFullCharge;

        public float ChargeMoveSpeedMore => _chargeMoveSpeedMore;

        public bool ChargeRunAnimation => _chargeRunAnimation;

        public float Distance => _distance;
        public float Duration => _duration;
        public float Cooldown => _cooldown;
        public DashDirection Direction => _direction;
        public float AccelerationPower => _accelerationPower;

        public float InvulnerabilitySeconds => _invulnerabilitySeconds;

        public float PushSpeed => _pushSpeed;

        public float PushSeconds => _pushSeconds;

        public float PushArcHeight => _pushArcHeight;

        public float PushReach => _pushReach;

        public float PushEdgeScale => _pushEdgeScale;

        public float PushForwardTiltDegrees => _pushForwardTiltDegrees;

        public float PushSpreadDegrees => _pushSpreadDegrees;
    }
}
