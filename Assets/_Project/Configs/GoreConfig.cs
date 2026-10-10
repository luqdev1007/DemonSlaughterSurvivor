using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "GoreConfig", menuName = "Game/Gore Config")]
    public sealed class GoreConfig : ScriptableObject
    {
        [Header("Dismemberment")]
        [SerializeField] private float _berserkChanceMultiplier = 2f;
        [SerializeField] private float _headWeight = 0.4f;
        [SerializeField] private float _nearArmWeight = 0.45f;
        [SerializeField] private float _farArmWeight = 0.15f;
        [SerializeField] private float _frontalBlowThreshold = 0.3f;

        [Header("Gibs")]
        [SerializeField] private int _maxActiveGibs = 24;
        [SerializeField] private float _gibLifetimeSeconds = 3f;
        [SerializeField] private float _gibSinkSeconds = 1f;
        [SerializeField] private float _gibSinkDepth = 0.6f;
        [SerializeField] private float _gibSpeed = 4f;
        [SerializeField] private float _gibUpSpeed = 3.5f;
        [SerializeField] private float _gibSpeedJitter = 0.3f;
        [SerializeField] private float _gibCorpseVelocityShare = 0.5f;
        [SerializeField] private float _gibSpinDegreesPerSecond = 540f;
        [SerializeField] private float _gibMass = 2f;
        [SerializeField] private float _gibColliderScale = 0.8f;

        public float BerserkChanceMultiplier => _berserkChanceMultiplier;

        public float HeadWeight => _headWeight;

        public float NearArmWeight => _nearArmWeight;

        public float FarArmWeight => _farArmWeight;

        public float FrontalBlowThreshold => _frontalBlowThreshold;

        public int MaxActiveGibs => _maxActiveGibs;

        public float GibLifetimeSeconds => _gibLifetimeSeconds;

        public float GibSinkSeconds => _gibSinkSeconds;

        public float GibSinkDepth => _gibSinkDepth;

        public float GibSpeed => _gibSpeed;

        public float GibUpSpeed => _gibUpSpeed;

        public float GibSpeedJitter => _gibSpeedJitter;

        public float GibCorpseVelocityShare => _gibCorpseVelocityShare;

        public float GibSpinDegreesPerSecond => _gibSpinDegreesPerSecond;

        public float GibMass => _gibMass;

        public float GibColliderScale => _gibColliderScale;
    }
}
