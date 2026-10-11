using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "HealOrbConfig", menuName = "Game/Content/Heal Orb Config")]
    public sealed class HealOrbConfig : ContentConfig
    {
        [SerializeField] private GameObject _viewPrefab;
        [SerializeField, Range(0f, 1f)] private float _healFraction = 0.3f;

        public GameObject ViewPrefab => _viewPrefab;

        public float HealFraction => _healFraction;
    }
}
