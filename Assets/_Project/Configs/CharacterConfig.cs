using System;
using System.Collections.Generic;
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

        [Header("Menu")]
        [SerializeField] private AnimationClip _menuIdleClip;
        [SerializeField] private AnimationClip _menuSelectClip;
        [SerializeField] private AvatarMask _menuSelectMask;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _turnSpeed = 720f;

        [Header("Aim")]
        [SerializeField, Min(0f)] private float _aimDeadZone = 1.2f;
        [SerializeField, Min(0f)] private float _aimTurnSpeed = 1080f;
        [SerializeField] private TorsoAimConfig _torsoAim;

        [Header("Body")]
        [SerializeField] private float _bodyRadius = 0.4f;
        [SerializeField, Min(0f)] private float _airborneClearance = 1f;

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

        [Header("Level Growth")]
        [SerializeField] private StatModifierEntry[] _levelGrowth = Array.Empty<StatModifierEntry>();

        [Header("Abilities")]
        [SerializeField] private DashAbilityConfig _dash;
        [SerializeField] private WeaponConfig _startingWeapon;
        [SerializeField] private BowConfig _bow;
        [SerializeField] private StaffConfig _staff;
        [SerializeField] private BackleapConfig _backleap;
        [SerializeField] private BlinkConfig _blink;
        [SerializeField] private RageConfig _rage;
        [SerializeField] private DualityConfig _duality;
        [SerializeField] private ConcentrationConfig _concentration;
        [SerializeField] private BerserkAbilityConfig _ultimate;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? Id : _displayName;

        public int MenuOrder => _menuOrder;

        public GameObject ViewPrefab => _viewPrefab;

        public AnimationClip MenuIdleClip => _menuIdleClip;

        public AnimationClip MenuSelectClip => _menuSelectClip;

        public AvatarMask MenuSelectMask => _menuSelectMask;

        public float MoveSpeed => _moveSpeed;

        public float TurnSpeed => _turnSpeed;

        public float AimDeadZone => _aimDeadZone;

        public float AimTurnSpeed => _aimTurnSpeed;

        public TorsoAimConfig TorsoAim => _torsoAim;

        public float BodyRadius => _bodyRadius;

        public float AirborneClearance => _airborneClearance;

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

        public IReadOnlyList<StatModifierEntry> LevelGrowth => _levelGrowth ?? Array.Empty<StatModifierEntry>();

        public DashAbilityConfig Dash => _dash;

        public WeaponConfig StartingWeapon => _startingWeapon;

        public BowConfig Bow => _bow;

        public StaffConfig Staff => _staff;

        public BackleapConfig Backleap => _backleap;

        public BlinkConfig Blink => _blink;

        public RageConfig Rage => _rage;

        public DualityConfig Duality => _duality;

        public ConcentrationConfig Concentration => _concentration;

        public LowHealthFuryConfig LowHealthFury => _lowHealthFury;

        public BerserkAbilityConfig Ultimate => _ultimate;
    }
}
