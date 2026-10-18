using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "BowConfig", menuName = "Game/Content/Bow Config")]
    public sealed class BowConfig : ContentConfig
    {
        [Header("Shot")]
        [SerializeField, Min(0f)] private float _damage = 14f;
        [SerializeField, Min(0.01f)] private float _cooldownSeconds = 0.8f;
        [SerializeField, Min(0.01f)] private float _drawSeconds = 0.45f;
        [SerializeField, Min(0.1f)] private float _range = 14f;
        [SerializeField, Range(0f, 90f)] private float _aimToleranceDegrees = 12f;

        [Header("Arrow")]
        [SerializeField] private GameObject _arrowPrefab;
        [SerializeField, Min(0.1f)] private float _arrowSpeed = 30f;
        [SerializeField, Min(0.1f)] private float _arrowRange = 20f;
        [SerializeField, Min(0.01f)] private float _arrowRadius = 0.15f;
        [SerializeField, Min(0f)] private float _muzzleDistance = 0.6f;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float _drawClipSeconds = 1f;
        [SerializeField] private string _drawTrigger = "Draw";
        [SerializeField] private string _releaseTrigger = "Release";
        [SerializeField] private string _cancelTrigger = "BowCancel";

        public float Damage => _damage;

        public float CooldownSeconds => _cooldownSeconds;

        public float DrawSeconds => _drawSeconds;

        public float Range => _range;

        public float AimToleranceDegrees => _aimToleranceDegrees;

        public GameObject ArrowPrefab => _arrowPrefab;

        public float ArrowSpeed => _arrowSpeed;

        public float ArrowRange => _arrowRange;

        public float ArrowRadius => _arrowRadius;

        public float MuzzleDistance => _muzzleDistance;

        public float DrawClipSeconds => _drawClipSeconds;

        public string DrawTrigger => _drawTrigger;

        public string ReleaseTrigger => _releaseTrigger;

        public string CancelTrigger => _cancelTrigger;

        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
                throw new InvalidOperationException($"{nameof(BowConfig)} '{name}' has no id.");

            if (_arrowPrefab == null)
                throw new InvalidOperationException($"{nameof(BowConfig)} '{Id}' has no arrow prefab.");

            if (_drawSeconds > _cooldownSeconds)
                throw new InvalidOperationException(
                    $"{nameof(BowConfig)} '{Id}' draws for {_drawSeconds} s, longer than its {_cooldownSeconds} s cooldown; " +
                    "the shot period would silently become the draw time.");
        }
    }
}
