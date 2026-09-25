using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "FeedbackConfig", menuName = "Game/Content/Feedback Config")]
    public sealed class FeedbackConfig : ContentConfig
    {
        [SerializeField] private float _hitFlashSeconds = 0.1f;
        [SerializeField] private float _invulnerabilityBlinkSeconds = 0.1f;
        [SerializeField] private float _enemyDeathSeconds = 1f;
        [SerializeField] private float _playerDeathDelaySeconds = 1.5f;

        [Header("Dissolve")]
        [SerializeField] private float _dissolveSeconds = 0.8f;
        [SerializeField] private Material _dissolveMaterial;

        [Header("Death Knockback")]
        [SerializeField] private float _knockbackSpeed = 6f;
        [SerializeField] private float _knockbackUpSpeed = 3f;
        [SerializeField] private float _knockbackSpeedJitter = 0.3f;
        [SerializeField] private float _knockbackAngleJitterDegrees = 20f;
        [SerializeField] private float _knockbackGravity = 20f;
        [SerializeField] private float _knockbackFriction = 12f;

        public float HitFlashSeconds => _hitFlashSeconds;

        public float InvulnerabilityBlinkSeconds => _invulnerabilityBlinkSeconds;

        public float EnemyDeathSeconds => _enemyDeathSeconds;

        public float PlayerDeathDelaySeconds => _playerDeathDelaySeconds;

        public float DissolveSeconds => _dissolveSeconds;

        public Material DissolveMaterial => _dissolveMaterial;

        public float KnockbackSpeed => _knockbackSpeed;

        public float KnockbackUpSpeed => _knockbackUpSpeed;

        public float KnockbackSpeedJitter => _knockbackSpeedJitter;

        public float KnockbackAngleJitterDegrees => _knockbackAngleJitterDegrees;

        public float KnockbackGravity => _knockbackGravity;

        public float KnockbackFriction => _knockbackFriction;
    }
}
