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

        public float HitFlashSeconds => _hitFlashSeconds;

        public float InvulnerabilityBlinkSeconds => _invulnerabilityBlinkSeconds;

        public float EnemyDeathSeconds => _enemyDeathSeconds;

        public float PlayerDeathDelaySeconds => _playerDeathDelaySeconds;
    }
}
