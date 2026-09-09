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

        public GameObject ViewPrefab => _viewPrefab;

        public float MoveSpeed => _moveSpeed;

        public float TurnSpeed => _turnSpeed;
    }
}
