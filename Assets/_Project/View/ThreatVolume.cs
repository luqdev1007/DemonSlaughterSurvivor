using UnityEngine;
using UnityEngine.Rendering;

namespace Game.View
{
    public sealed class ThreatVolume : MonoBehaviour
    {
        [SerializeField] private Volume _volume;
        [SerializeField] private AnimationCurve _weightByWaveProgress = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.45f, 0.05f), new Keyframe(0.5f, 0.4f), new Keyframe(1f, 0.6f));
        [SerializeField, Range(0f, 1f)] private float _bossWeight = 0.9f;
        [SerializeField] private float _blendSeconds = 3f;
        [SerializeField] private float _dangerFogDensity = 0.026f;
        [SerializeField] private Color _dangerFogColor = new Color(0.17f, 0.07f, 0.09f, 1f);

        private float _target;
        private float _weight;
        private float _baseFogDensity;
        private Color _baseFogColor;
        private bool _hasBase;

        public float Weight => _weight;

        public void SetTarget(float waveProgress, bool bossAlive)
        {
            float target = _weightByWaveProgress.Evaluate(Mathf.Clamp01(waveProgress));

            if (bossAlive)
                target = Mathf.Max(target, _bossWeight);

            _target = Mathf.Clamp01(target);
        }

        public void SetImmediate(float weight)
        {
            _target = Mathf.Clamp01(weight);
            _weight = _target;

            Apply();
        }

        private void Awake()
        {
            _baseFogDensity = RenderSettings.fogDensity;
            _baseFogColor = RenderSettings.fogColor;
            _hasBase = true;

            Apply();
        }

        private void Update()
        {
            float step = _blendSeconds > 0f ? Time.unscaledDeltaTime / _blendSeconds : 1f;

            _weight = Mathf.MoveTowards(_weight, _target, step);

            Apply();
        }

        private void OnDestroy()
        {
            if (_hasBase == false)
                return;

            RenderSettings.fogDensity = _baseFogDensity;
            RenderSettings.fogColor = _baseFogColor;
        }

        private void Apply()
        {
            if (_volume != null)
                _volume.weight = _weight;

            if (_hasBase == false)
                return;

            RenderSettings.fogDensity = Mathf.Lerp(_baseFogDensity, _dangerFogDensity, _weight);
            RenderSettings.fogColor = Color.Lerp(_baseFogColor, _dangerFogColor, _weight);
        }
    }
}
