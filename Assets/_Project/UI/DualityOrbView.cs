using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class DualityOrbView : MonoBehaviour
    {
        private static readonly int DemonicId = Shader.PropertyToID("_Demonic");
        private static readonly int HumanId = Shader.PropertyToID("_Human");
        private static readonly int DemonFormId = Shader.PropertyToID("_DemonForm");
        private static readonly int HumanFormId = Shader.PropertyToID("_HumanForm");
        private static readonly int ThresholdId = Shader.PropertyToID("_Threshold");
        private static readonly int ClockId = Shader.PropertyToID("_Clock");

        [SerializeField] private Image _fill;
        [SerializeField] private Material _fillMaterial;
        [SerializeField] private RectTransform _pulseRoot;

        [Header("Motion")]
        [SerializeField] private float _levelSharpness = 8f;
        [SerializeField] private float _formFadeSeconds = 0.3f;
        [SerializeField] private float _popScale = 0.12f;
        [SerializeField] private float _popSeconds = 0.4f;

        private Material _material;
        private Vector3 _pulseBaseScale = Vector3.one;
        private float _threshold = 1f;
        private float _target = 0.5f;
        private float _level = 0.5f;
        private float _demonForm;
        private float _humanForm;
        private float _pop;
        private float _time;

        public void Show(float demonic, float formThreshold)
        {
            demonic = Mathf.Clamp01(demonic);
            _threshold = formThreshold;

            bool wasDemon = _target > _threshold;
            bool wasHuman = 1f - _target > _threshold;

            _target = demonic;

            if ((wasDemon == false && _target > _threshold) || (wasHuman == false && 1f - _target > _threshold))
                _pop = 1f;
        }

        private void Awake()
        {
            if (_pulseRoot != null)
                _pulseBaseScale = _pulseRoot.localScale;

            if (_fill == null || _fillMaterial == null)
                return;

            _material = new Material(_fillMaterial);
            _fill.material = _material;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            float fade = _formFadeSeconds > 0f ? delta / _formFadeSeconds : 1f;

            _time += delta;
            _level = Mathf.Lerp(_level, _target, 1f - Mathf.Exp(-_levelSharpness * delta));
            _demonForm = Mathf.MoveTowards(_demonForm, _target > _threshold ? 1f : 0f, fade);
            _humanForm = Mathf.MoveTowards(_humanForm, 1f - _target > _threshold ? 1f : 0f, fade);
            _pop = Mathf.MoveTowards(_pop, 0f, _popSeconds > 0f ? delta / _popSeconds : 1f);

            if (_pulseRoot != null)
                _pulseRoot.localScale = _pulseBaseScale * (1f + _pop * _pop * _popScale);

            if (_material == null)
                return;

            _material.SetFloat(DemonicId, _level);
            _material.SetFloat(HumanId, 1f - _level);
            _material.SetFloat(DemonFormId, _demonForm);
            _material.SetFloat(HumanFormId, _humanForm);
            _material.SetFloat(ThresholdId, _threshold);
            _material.SetFloat(ClockId, _time);
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
