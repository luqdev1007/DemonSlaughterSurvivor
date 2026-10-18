using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Game/Content/Character Config")]
    public sealed class CharacterConfig : ContentConfig
    {
        [Header("Roster")]
        [SerializeField] private string _displayName;
        [SerializeField] private int _menuOrder;

        [SerializeField] private GameObject _viewPrefab;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _turnSpeed = 720f;

        [Header("Aim")]
        [SerializeField, Min(0f)] private float _aimDeadZone = 1.2f;
        [SerializeField, Min(0f)] private float _aimTurnSpeed = 1080f;
        [SerializeField] private TorsoAimConfig _torsoAim;

        [Header("Body")]
        [SerializeField] private float _bodyRadius = 0.4f;

        [Header("Combat")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _hitInvulnerabilitySeconds = 0.5f;

        [Header("Offense")]
        [SerializeField, Range(0f, 1f)] private float _critChance = 0.05f;
        [SerializeField, Min(1f)] private float _critMultiplier = 2f;
        [SerializeField, Min(0f)] private float _attackPower;
        [SerializeField, Min(0f)] private float _spellPower;

        [Header("Defense")]
        [SerializeField, Min(0f)] private float _armor;
        [SerializeField, Min(0f)] private float _magicResist;
        [SerializeField, Min(1f)] private float _defenseScale = 100f;
        [SerializeField, Range(0f, 1f)] private float _evasion;
        [SerializeField, Range(0f, 1f)] private float _maxEvasion = 0.6f;

        [Header("Fortune")]
        [SerializeField, Min(0f)] private float _luck;

        [Header("Life Steal")]
        [SerializeField, Min(0f)] private float _lifeOnKill;
        [SerializeField, Min(0f)] private float _lifeOnHit;

        [Header("Low Health Fury")]
        [SerializeField] private LowHealthFuryConfig _lowHealthFury;

        [Header("Experience")]
        [SerializeField] private float _pickupRadius = 1.5f;
        [SerializeField] private ExperienceConfig _experience;

        [Header("Abilities")]
        [SerializeField] private DashAbilityConfig _dash;
        [SerializeField] private WeaponConfig _startingWeapon;
        [SerializeField] private BowConfig _bow;
        [SerializeField] private RageConfig _rage;
        [SerializeField] private BerserkAbilityConfig _ultimate;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? Id : _displayName;

        public int MenuOrder => _menuOrder;

        public GameObject ViewPrefab => _viewPrefab;

        public float MoveSpeed => _moveSpeed;

        public float TurnSpeed => _turnSpeed;

        public float AimDeadZone => _aimDeadZone;

        public float AimTurnSpeed => _aimTurnSpeed;

        public TorsoAimConfig TorsoAim => _torsoAim;

        public float BodyRadius => _bodyRadius;

        public float MaxHealth => _maxHealth;

        public float HitInvulnerabilitySeconds => _hitInvulnerabilitySeconds;

        public float CritChance => _critChance;

        public float CritMultiplier => _critMultiplier;

        public float AttackPower => _attackPower;

        public float SpellPower => _spellPower;

        public float Armor => _armor;

        public float MagicResist => _magicResist;

        public float DefenseScale => _defenseScale;

        public float Evasion => _evasion;

        public float MaxEvasion => _maxEvasion;

        public float Luck => _luck;

        public float LifeOnKill => _lifeOnKill;

        public float LifeOnHit => _lifeOnHit;

        public float PickupRadius => _pickupRadius;

        public ExperienceConfig Experience => _experience;

        public DashAbilityConfig Dash => _dash;

        public WeaponConfig StartingWeapon => _startingWeapon;

        public BowConfig Bow => _bow;

        public RageConfig Rage => _rage;

        public LowHealthFuryConfig LowHealthFury => _lowHealthFury;

        public BerserkAbilityConfig Ultimate => _ultimate;
    }
}
