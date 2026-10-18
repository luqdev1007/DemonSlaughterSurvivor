using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class HealthOrbView : MonoBehaviour
    {
        private static readonly int LevelId = Shader.PropertyToID("_Level");
        private static readonly int TrailId = Shader.PropertyToID("_Trail");
        private static readonly int SloshId = Shader.PropertyToID("_Slosh");
        private static readonly int LowId = Shader.PropertyToID("_Low");
        private static readonly int ClockId = Shader.PropertyToID("_Clock");

        [SerializeField] private Image _fill;
        [SerializeField] private Material _fillMaterial;

        [Header("Motion")]
        [SerializeField] private float _levelSharpness = 10f;
        [SerializeField] private float _trailDelaySeconds = 0.45f;
        [SerializeField] private float _trailSpeed = 0.35f;
        [SerializeField] private float _sloshPerChange = 0.6f;
        [SerializeField] private float _sloshDamping = 3.5f;
        [SerializeField] private float _sloshHz = 1.7f;
        [SerializeField, Range(0f, 1f)] private float _lowThreshold = 0.3f;

        private Material _material;
        private float _target = 1f;
        private float _level = 1f;
        private float _trail = 1f;
        private float _trailWait;
        private float _sloshEnergy;
        private float _time;

        public void Show(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);

            if (Mathf.Approximately(fraction, _target))
                return;

            if (fraction < _target)
                _trailWait = _trailDelaySeconds;

            _sloshEnergy = Mathf.Min(1f, _sloshEnergy + Mathf.Abs(fraction - _target) * _sloshPerChange * 10f);
            _target = fraction;
        }

        private void Awake()
        {
            if (_fill == null || _fillMaterial == null)
                return;

            _material = new Material(_fillMaterial);
            _fill.material = _material;
            _fill.fillAmount = 1f;
        }

        private void Update()
        {
            if (_material == null)
                return;

            float delta = Time.unscaledDeltaTime;

            _time += delta;
            _level = Mathf.Lerp(_level, _target, 1f - Mathf.Exp(-_levelSharpness * delta));

            if (_trail < _level)
                _trail = _level;

            if (_trailWait > 0f)
                _trailWait -= delta;
            else
                _trail = Mathf.MoveTowards(_trail, _level, _trailSpeed * delta);

            _sloshEnergy = Mathf.MoveTowards(_sloshEnergy, 0f, _sloshDamping * delta * Mathf.Max(0.05f, _sloshEnergy));

            float slosh = Mathf.Sin(_time * _sloshHz * Mathf.PI * 2f) * _sloshEnergy * 0.12f;
            float low = _target <= 0f ? 0f : Mathf.Clamp01(1f - _target / _lowThreshold);

            _material.SetFloat(LevelId, _level);
            _material.SetFloat(TrailId, _trail);
            _material.SetFloat(SloshId, slosh);
            _material.SetFloat(LowId, low);
            _material.SetFloat(ClockId, _time);
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
