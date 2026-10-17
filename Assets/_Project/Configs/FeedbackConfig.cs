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
        [SerializeField] private float _victoryBannerSeconds = 3f;

        [Header("Dissolve")]
        [SerializeField] private float _dissolveSeconds = 0.8f;
        [SerializeField] private float _heroDissolveDelaySeconds;
        [SerializeField] private Material _dissolveMaterial;

        [Header("Death Knockback")]
        [SerializeField] private float _knockbackSpeed = 6f;
        [SerializeField] private float _knockbackUpSpeed = 3f;
        [SerializeField] private float _knockbackSpeedJitter = 0.3f;
        [SerializeField] private float _knockbackAngleJitterDegrees = 20f;
        [SerializeField] private float _knockbackGravity = 20f;
        [SerializeField] private float _knockbackFriction = 12f;

        [Header("Corpses")]
        [SerializeField] private float _corpseMinSeconds = 10f;
        [SerializeField] private float _corpseMaxSeconds = 15f;
        [SerializeField] private float _corpseDecaySeconds = 1.5f;
        [SerializeField] private float _corpseSinkDepth = 1.4f;
        [SerializeField] private int _maxCorpses = 60;
        [SerializeField] private float _offscreenMargin = 0.08f;

        [Header("Gib kicks")]
        [SerializeField] private float _kickRadius = 1f;
        [SerializeField] private float _kickSpeed = 3.5f;
        [SerializeField] private float _kickHeroSpeedShare = 0.6f;
        [SerializeField] private float _kickUpSpeed = 2f;
        [SerializeField] private float _strikeKickSpeed = 7f;
        [SerializeField] private float _strikeKickUpSpeed = 3.5f;

        [Header("Gore")]
        [SerializeField] private GoreConfig _gore;

        [Header("Audio")]
        [SerializeField] private SfxConfig _sfx;

        public float HitFlashSeconds => _hitFlashSeconds;

        public float InvulnerabilityBlinkSeconds => _invulnerabilityBlinkSeconds;

        public float EnemyDeathSeconds => _enemyDeathSeconds;

        public float PlayerDeathDelaySeconds => _playerDeathDelaySeconds;

        public float VictoryBannerSeconds => _victoryBannerSeconds;

        public float DissolveSeconds => _dissolveSeconds;

        public float HeroDissolveDelaySeconds => _heroDissolveDelaySeconds;

        public Material DissolveMaterial => _dissolveMaterial;

        public float KnockbackSpeed => _knockbackSpeed;

        public float KnockbackUpSpeed => _knockbackUpSpeed;

        public float KnockbackSpeedJitter => _knockbackSpeedJitter;

        public float KnockbackAngleJitterDegrees => _knockbackAngleJitterDegrees;

        public float KnockbackGravity => _knockbackGravity;

        public float KnockbackFriction => _knockbackFriction;

        public GoreConfig Gore => _gore;

        public SfxConfig Sfx => _sfx;

        public float CorpseMinSeconds => _corpseMinSeconds;

        public float CorpseMaxSeconds => _corpseMaxSeconds;

        public float CorpseDecaySeconds => _corpseDecaySeconds;

        public float CorpseSinkDepth => _corpseSinkDepth;

        public int MaxCorpses => _maxCorpses;

        public float OffscreenMargin => _offscreenMargin;

        public float KickRadius => _kickRadius;

        public float KickSpeed => _kickSpeed;

        public float KickHeroSpeedShare => _kickHeroSpeedShare;

        public float KickUpSpeed => _kickUpSpeed;

        public float StrikeKickSpeed => _strikeKickSpeed;

        public float StrikeKickUpSpeed => _strikeKickUpSpeed;
    }
}
