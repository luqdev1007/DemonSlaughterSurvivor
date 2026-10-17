using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Content/Enemy Config")]
    public sealed class EnemyConfig : ContentConfig
    {
        [SerializeField] private GameObject _viewPrefab;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _turnSpeed = 360f;
        [SerializeField, Range(0f, 0.5f)] private float _moveSpeedSpread;

        [Header("Body")]
        [SerializeField] private float _bodyRadius = 0.4f;
        [SerializeField] private float _bodyHeight = 1.8f;

        [Header("Combat")]
        [SerializeField] private float _maxHealth = 20f;
        [SerializeField] private float _contactDamage = 10f;

        [Header("Drop")]
        [SerializeField] private GemConfig _gem;

        [Header("Ranged")]
        [SerializeField] private RangedAttackConfig _rangedAttack;

        [Header("Boss")]
        [SerializeField] private BossConfig _boss;
        [SerializeField] private ChargeAbilityConfig _charge;
        [SerializeField] private SpinAbilityConfig _spin;
        [SerializeField] private HeavyStrikeAbilityConfig _heavyStrike;

        [Header("Gore")]
        [SerializeField] private GameObject _dismemberedViewPrefab;
        [SerializeField, Range(0f, 1f)] private float _dismemberChance = 0.35f;

        [Header("Separation")]
        [SerializeField] private float _separationRadius = 1f;
        [SerializeField] private float _separationStrength = 0.5f;

        public GameObject ViewPrefab => _viewPrefab;

        public float MoveSpeed => _moveSpeed;

        public float TurnSpeed => _turnSpeed;

        public float MoveSpeedSpread => _moveSpeedSpread;

        public float BodyRadius => _bodyRadius;

        public float BodyHeight => _bodyHeight;

        public float MaxHealth => _maxHealth;

        public float ContactDamage => _contactDamage;

        public GemConfig Gem => _gem;

        public RangedAttackConfig RangedAttack => _rangedAttack;

        public BossConfig Boss => _boss;

        public ChargeAbilityConfig Charge => _charge;

        public SpinAbilityConfig Spin => _spin;

        public HeavyStrikeAbilityConfig HeavyStrike => _heavyStrike;

        public float PeakMoveSpeed => (_spin == null ? _moveSpeed : _moveSpeed * Mathf.Max(1f, _spin.MoveSpeedMultiplier)) * (1f + _moveSpeedSpread);

        public GameObject DismemberedViewPrefab => _dismemberedViewPrefab;

        public float DismemberChance => _dismemberChance;

        public float SeparationRadius => _separationRadius;

        public float SeparationStrength => _separationStrength;
    }
}
