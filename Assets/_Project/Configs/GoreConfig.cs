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
        [SerializeField] private float _gibLifetimeMaxSeconds = 3f;
        [SerializeField] private float _gibSinkSeconds = 1f;
        [SerializeField] private float _gibSinkDepth = 0.6f;
        [SerializeField] private float _gibSpeed = 4f;
        [SerializeField] private float _gibUpSpeed = 3.5f;
        [SerializeField] private float _gibSpeedJitter = 0.3f;
        [SerializeField] private float _gibCorpseVelocityShare = 0.5f;
        [SerializeField] private float _gibSpinDegreesPerSecond = 540f;
        [SerializeField] private float _gibMass = 2f;
        [SerializeField] private float _gibColliderScale = 0.8f;

        [Header("Blood: prefabs")]
        [SerializeField] private GameObject _hitSpray;
        [SerializeField] private GameObject _hitSplat;
        [SerializeField] private GameObject _deathSpray;
        [SerializeField] private GameObject _deathSplat;
        [SerializeField] private GameObject _stumpFountain;
        [SerializeField] private GameObject _gibDrip;
        [SerializeField] private GameObject _gibImpactSplat;

        [Header("Blood: limits")]
        [SerializeField] private int _maxSprays = 24;
        [SerializeField] private int _maxSplats = 40;
        [SerializeField] private int _maxFountains = 8;
        [SerializeField] private int _maxDrips = 12;

        [Header("Blood: placement")]
        [SerializeField] private float _hitHeight = 1f;
        [SerializeField] private float _sprayLift = 0.3f;
        [SerializeField] private float _splatHeight = 0.02f;
        [SerializeField] private float _splatScale = 1.5f;
        [SerializeField] private float _fountainSeconds = 1f;
        [SerializeField] private float _gibImpactMinSpeed = 1.5f;

        [Header("Blood: lasting decals")]
        [SerializeField] private Material _decalMaterial;
        [SerializeField, Min(0)] private int _maxDecals = 140;
        [SerializeField] private Vector2 _poolSize = new Vector2(1.9f, 2.9f);
        [SerializeField] private float _poolDelaySeconds = 0.45f;
        [SerializeField] private Vector2 _poolGrowSeconds = new Vector2(3f, 4.5f);
        [SerializeField] private float _poolLifeSeconds = 26f;
        [SerializeField] private Vector2Int _deathSplatters = new Vector2Int(2, 4);
        [SerializeField] private Vector2 _deathSplatterDistance = new Vector2(0.4f, 2.2f);
        [SerializeField] private Vector2 _deathSplatterSize = new Vector2(0.7f, 1.6f);
        [SerializeField, Range(0f, 1f)] private float _hitSplatterChance = 0.45f;
        [SerializeField] private Vector2 _hitSplatterSize = new Vector2(0.35f, 0.8f);
        [SerializeField] private float _splatterLifeSeconds = 20f;
        [SerializeField] private float _decalFadeSeconds = 4f;
        [SerializeField] private float _decalDrySeconds = 18f;

        public float BerserkChanceMultiplier => _berserkChanceMultiplier;

        public float HeadWeight => _headWeight;

        public float NearArmWeight => _nearArmWeight;

        public float FarArmWeight => _farArmWeight;

        public float FrontalBlowThreshold => _frontalBlowThreshold;

        public int MaxActiveGibs => _maxActiveGibs;

        public float GibLifetimeSeconds => _gibLifetimeSeconds;

        public float GibLifetimeMaxSeconds => Mathf.Max(_gibLifetimeSeconds, _gibLifetimeMaxSeconds);

        public float GibSinkSeconds => _gibSinkSeconds;

        public float GibSinkDepth => _gibSinkDepth;

        public float GibSpeed => _gibSpeed;

        public float GibUpSpeed => _gibUpSpeed;

        public float GibSpeedJitter => _gibSpeedJitter;

        public float GibCorpseVelocityShare => _gibCorpseVelocityShare;

        public float GibSpinDegreesPerSecond => _gibSpinDegreesPerSecond;

        public float GibMass => _gibMass;

        public float GibColliderScale => _gibColliderScale;

        public GameObject HitSpray => _hitSpray;

        public GameObject HitSplat => _hitSplat;

        public GameObject DeathSpray => _deathSpray;

        public GameObject DeathSplat => _deathSplat;

        public GameObject StumpFountain => _stumpFountain;

        public GameObject GibDrip => _gibDrip;

        public GameObject GibImpactSplat => _gibImpactSplat;

        public int MaxSprays => _maxSprays;

        public int MaxSplats => _maxSplats;

        public int MaxFountains => _maxFountains;

        public int MaxDrips => _maxDrips;

        public float HitHeight => _hitHeight;

        public float SprayLift => _sprayLift;

        public float SplatHeight => _splatHeight;

        public float SplatScale => _splatScale;

        public float FountainSeconds => _fountainSeconds;

        public float GibImpactMinSpeed => _gibImpactMinSpeed;

        public Material DecalMaterial => _decalMaterial;

        public int MaxDecals => _maxDecals;

        public Vector2 PoolSize => _poolSize;

        public float PoolDelaySeconds => _poolDelaySeconds;

        public Vector2 PoolGrowSeconds => _poolGrowSeconds;

        public float PoolLifeSeconds => _poolLifeSeconds;

        public Vector2Int DeathSplatters => _deathSplatters;

        public Vector2 DeathSplatterDistance => _deathSplatterDistance;

        public Vector2 DeathSplatterSize => _deathSplatterSize;

        public float HitSplatterChance => _hitSplatterChance;

        public Vector2 HitSplatterSize => _hitSplatterSize;

        public float SplatterLifeSeconds => _splatterLifeSeconds;

        public float DecalFadeSeconds => _decalFadeSeconds;

        public float DecalDrySeconds => _decalDrySeconds;
    }
}
