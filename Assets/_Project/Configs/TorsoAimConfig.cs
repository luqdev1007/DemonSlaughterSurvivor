using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "TorsoAimConfig", menuName = "Game/Abilities/Torso Aim Config")]
    public sealed class TorsoAimConfig : ScriptableObject
    {
        [Header("Target")]
        [SerializeField, Min(0.1f)] private float _searchRadius = 14f;
        [SerializeField, Min(0f)] private float _keepMargin = 2f;
        [SerializeField, Min(0f)] private float _switchMargin = 1.5f;

        [Header("Torso")]
        [SerializeField, Min(1f)] private float _torsoTurnSpeed = 720f;
        [SerializeField, Range(10f, 180f)] private float _twistLimit = 110f;

        [Header("Legs")]
        [SerializeField] private bool _wholeBody;
        [SerializeField, Range(0f, 60f)] private float _backpedalHysteresis = 20f;
        [SerializeField, Min(1f)] private float _turnInPlaceSpeed = 240f;
        [SerializeField, Range(0f, 45f)] private float _turnInPlaceSettle = 8f;

        public float SearchRadius => _searchRadius;

        public float KeepRadius => _searchRadius + _keepMargin;

        public float SwitchMargin => _switchMargin;

        public float TorsoTurnSpeed => _torsoTurnSpeed;

        public float TwistLimit => _twistLimit;

        public bool WholeBody => _wholeBody;

        public float BackpedalHysteresis => _backpedalHysteresis;

        public float TurnInPlaceSpeed => _turnInPlaceSpeed;

        public float TurnInPlaceSettle => _turnInPlaceSettle;

        public void Validate(string owner)
        {
            if (_backpedalHysteresis >= _twistLimit)
                throw new InvalidOperationException(
                    $"{owner}: {nameof(TorsoAimConfig)} '{name}' has a backpedal hysteresis of {_backpedalHysteresis} degrees, " +
                    $"not below the twist limit {_twistLimit}; the legs could never switch back to running forward.");

            if (_turnInPlaceSettle >= _twistLimit)
                throw new InvalidOperationException(
                    $"{owner}: {nameof(TorsoAimConfig)} '{name}' settles a turn in place at {_turnInPlaceSettle} degrees, " +
                    $"not below the twist limit {_twistLimit}; the turn would start and stop on the same tick.");
        }
    }
}
