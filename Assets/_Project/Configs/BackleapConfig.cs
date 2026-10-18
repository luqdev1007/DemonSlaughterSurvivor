using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "BackleapConfig", menuName = "Game/Content/Backleap Config")]
    public sealed class BackleapConfig : ContentConfig, IAbilityIcon
    {
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 2.5f;

        [Header("Leap")]
        [SerializeField, Min(0.1f)] private float _distance = 5f;
        [SerializeField, Min(0.02f)] private float _leapSeconds = 0.3f;
        [SerializeField, Min(0f)] private float _leapHeight = 1.6f;
        [SerializeField, Min(0f)] private float _invulnerabilitySeconds = 0.35f;

        [Header("Hover")]
        [SerializeField, Min(0f)] private float _maxHoverSeconds = 1.2f;
        [SerializeField, Min(0f)] private float _hoverDriftSpeed = 1f;

        [Header("Fall")]
        [SerializeField, Min(0.02f)] private float _fallSeconds = 0.25f;

        [Header("Dive")]
        [SerializeField, Min(0f)] private float _hopHeight = 2.2f;
        [SerializeField, Min(0.02f)] private float _hopSeconds = 0.35f;
        [SerializeField, Min(0.02f)] private float _diveSeconds = 0.15f;
        [SerializeField, Min(0.1f)] private float _impactRadius = 2.5f;
        [SerializeField, Min(0f)] private float _impactDamage = 30f;
        [SerializeField, Min(0f)] private float _impactPushSpeed = 6f;
        [SerializeField, Min(0f)] private float _impactPushSeconds = 0.4f;
        [SerializeField, Min(0f)] private float _impactPushArcHeight = 0.8f;

        [Header("Animation")]
        [SerializeField, Min(0.1f)] private float _leapAnimationSpeed = 1.6f;

        public Sprite Icon => _icon;

        public float CooldownSeconds => _cooldownSeconds;

        public float Distance => _distance;

        public float LeapSeconds => _leapSeconds;

        public float LeapHeight => _leapHeight;

        public float InvulnerabilitySeconds => _invulnerabilitySeconds;

        public float MaxHoverSeconds => _maxHoverSeconds;

        public float HoverDriftSpeed => _hoverDriftSpeed;

        public float FallSeconds => _fallSeconds;

        public float HopHeight => _hopHeight;

        public float HopSeconds => _hopSeconds;

        public float DiveSeconds => _diveSeconds;

        public float ImpactRadius => _impactRadius;

        public float ImpactDamage => _impactDamage;

        public float ImpactPushSpeed => _impactPushSpeed;

        public float ImpactPushSeconds => _impactPushSeconds;

        public float ImpactPushArcHeight => _impactPushArcHeight;

        public float LeapAnimationSpeed => _leapAnimationSpeed;

        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
                throw new InvalidOperationException($"{nameof(BackleapConfig)} '{name}' has no id.");
        }
    }
}
