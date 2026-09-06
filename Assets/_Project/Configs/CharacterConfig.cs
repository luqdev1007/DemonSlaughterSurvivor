using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Game/Content/Character Config")]
    public sealed class CharacterConfig : ContentConfig
    {
        [SerializeField] private GameObject _viewPrefab;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed;

        [Header("Dash")]
        [SerializeField] private float _dashDistance;
        [SerializeField] private float _dashDuration;
        [SerializeField] private float _dashCooldown;
        [Tooltip("+1 вперёд, -1 назад")][SerializeField] private float _dashDirectionSign; 
        [SerializeField] private float _inputBufferSeconds;

        public GameObject ViewPrefab => _viewPrefab;

        public float MoveSpeed => _moveSpeed;

        public float DashDistance => _dashDistance;
        public float DashDuration => _dashDuration;
        public float DashCooldown => _dashCooldown;
        public float DashDirectionSign => _dashDirectionSign;
        public float InputBufferSeconds => _inputBufferSeconds;
    }
}
