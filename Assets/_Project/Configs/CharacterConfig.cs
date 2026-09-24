using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Game/Content/Character Config")]
    public sealed class CharacterConfig : ContentConfig
    {
        [SerializeField] private GameObject _viewPrefab;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _turnSpeed = 720f;

        [Header("Body")]
        [SerializeField] private float _bodyRadius = 0.4f;

        [Header("Combat")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _hitInvulnerabilitySeconds = 0.5f;

        [Header("Abilities")]
        [SerializeField] private DashAbilityConfig _dash;

        public GameObject ViewPrefab => _viewPrefab;

        public float MoveSpeed => _moveSpeed;

        public float TurnSpeed => _turnSpeed;

        public float BodyRadius => _bodyRadius;

        public float MaxHealth => _maxHealth;

        public float HitInvulnerabilitySeconds => _hitInvulnerabilitySeconds;

        public DashAbilityConfig Dash => _dash;
    }
}
