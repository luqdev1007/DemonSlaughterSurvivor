using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Game/Content/Character Config")]
    public sealed class CharacterConfig : ContentConfig
    {
        [SerializeField] private float _moveSpeed;
        [SerializeField] private GameObject _viewPrefab;

        public float MoveSpeed => _moveSpeed;
        public GameObject ViewPrefab => _viewPrefab;
    }
}
