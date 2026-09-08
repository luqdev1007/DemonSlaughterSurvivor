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

        [Header("Abilities")]
        [SerializeField] private DashAbilityConfig _dash;

        public GameObject ViewPrefab => _viewPrefab;

        public float MoveSpeed => _moveSpeed;

        public float TurnSpeed => _turnSpeed;

        public DashAbilityConfig Dash => _dash;
    }
}
