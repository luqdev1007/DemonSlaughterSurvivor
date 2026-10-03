using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "GemConfig", menuName = "Game/Content/Gem Config")]
    public sealed class GemConfig : ContentConfig
    {
        [SerializeField] private GameObject _viewPrefab;
        [SerializeField] private int _experience = 1;

        public GameObject ViewPrefab => _viewPrefab;

        public int Experience => _experience;
    }
}
