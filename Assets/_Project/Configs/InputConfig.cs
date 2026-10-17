using Game.Core;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "InputConfig", menuName = "Game/Input Config")]
    public sealed class InputConfig : ScriptableObject
    {
        [SerializeField] private float _bufferSeconds = 0.12f;
        [SerializeField] private ControlScheme _defaultScheme = ControlScheme.Keyboard;

        [Header("Mouse hold: aim turn")]
        [SerializeField] private float _aimAlignDegrees = 15f;
        [SerializeField] private float _dashTurnBoost = 2f;
        [SerializeField, Range(0.01f, 1f)] private float _angularSmoothing = 0.3f;

        [Header("Mouse hold: predictive swing")]
        [SerializeField] private float _predictiveTurnSpeed = 200f;
        [SerializeField] private float _predictiveLeadSeconds = 0.15f;
        [SerializeField] private float _predictiveMaxLeadDegrees = 75f;
        [SerializeField] private float _predictiveReachPerTurnSpeed = 0.0005f;
        [SerializeField] private float _predictiveMaxReachBonus = 0.4f;
        [SerializeField] private string _turnLeftSwing = "AttackLeftSlash";
        [SerializeField] private string _turnRightSwing = "AttackRightSlash";

        [Header("Mouse hold: sharp side swing")]
        [SerializeField] private float _sharpSwingDamageMultiplier = 1.6f;
        [SerializeField] private float _sharpSwingSpeedMultiplier = 1.25f;
        [SerializeField] private float _sharpSwingPushSpeed = 7f;
        [SerializeField] private float _sharpSwingPushSeconds = 0.2f;

        public float BufferSeconds => _bufferSeconds;

        public ControlScheme DefaultScheme => _defaultScheme;

        public float AimAlignDegrees => _aimAlignDegrees;

        public float DashTurnBoost => _dashTurnBoost;

        public float AngularSmoothing => _angularSmoothing;

        public float PredictiveTurnSpeed => _predictiveTurnSpeed;

        public float PredictiveLeadSeconds => _predictiveLeadSeconds;

        public float PredictiveMaxLeadDegrees => _predictiveMaxLeadDegrees;

        public float PredictiveReachPerTurnSpeed => _predictiveReachPerTurnSpeed;

        public float PredictiveMaxReachBonus => _predictiveMaxReachBonus;

        public string TurnLeftSwing => _turnLeftSwing;

        public string TurnRightSwing => _turnRightSwing;

        public float SharpSwingDamageMultiplier => _sharpSwingDamageMultiplier;

        public float SharpSwingSpeedMultiplier => _sharpSwingSpeedMultiplier;

        public float SharpSwingPushSpeed => _sharpSwingPushSpeed;

        public float SharpSwingPushSeconds => _sharpSwingPushSeconds;
    }
}
