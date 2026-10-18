using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "GemConfig", menuName = "Game/Content/Gem Config")]
    public sealed class GemConfig : ContentConfig
    {
        [SerializeField] private GameObject _viewPrefab;
        [SerializeField] private int _experience = 1;

        [Header("Scatter out of the dying enemy")]
        [SerializeField, Min(0f)] private float _scatterMinDistance = 0.7f;
        [SerializeField, Min(0f)] private float _scatterMaxDistance = 2f;
        [SerializeField, Min(0f)] private float _scatterMinHeight = 0.8f;
        [SerializeField, Min(0f)] private float _scatterMaxHeight = 1.6f;
        [SerializeField, Min(0f)] private float _scatterMinSeconds = 0.4f;
        [SerializeField, Min(0f)] private float _scatterMaxSeconds = 0.6f;

        public GameObject ViewPrefab => _viewPrefab;

        public float ScatterMinDistance => _scatterMinDistance;

        public float ScatterMaxDistance => Mathf.Max(_scatterMinDistance, _scatterMaxDistance);

        public float ScatterMinHeight => _scatterMinHeight;

        public float ScatterMaxHeight => Mathf.Max(_scatterMinHeight, _scatterMaxHeight);

        public float ScatterMinSeconds => _scatterMinSeconds;

        public float ScatterMaxSeconds => Mathf.Max(_scatterMinSeconds, _scatterMaxSeconds);

        public int Experience => _experience;
    }
}
