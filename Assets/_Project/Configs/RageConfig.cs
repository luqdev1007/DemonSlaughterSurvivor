using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "RageConfig", menuName = "Game/Content/Rage Config")]
    public sealed class RageConfig : ContentConfig
    {
        [SerializeField] private float _max = 100f;

        [Header("Gain")]
        [SerializeField] private float _dealtPerHit = 0.2f;
        [SerializeField] private float _receivedScale = 100f;

        [Header("Decay")]
        [SerializeField] private float _decayDelaySeconds = 3f;
        [SerializeField] private float _decayPerSecond = 4f;

        public float Max => _max;

        public float DealtPerHit => _dealtPerHit;

        public float ReceivedScale => _receivedScale;

        public float DecayDelaySeconds => _decayDelaySeconds;

        public float DecayPerSecond => _decayPerSecond;
    }
}
