using UnityEngine;

namespace Game.View
{
    public sealed class DeathRayFx : MonoBehaviour
    {
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField] private LineRenderer _core;
        [SerializeField] private LineRenderer _glow;
        [SerializeField] private Transform _orb;
        [SerializeField] private ParticleSystem _sparks;
        [SerializeField] private ParticleSystem _flare;

        [Header("Shape")]
        [SerializeField, Min(1f)] private float _length = 16f;
        [SerializeField, Min(0f)] private float _coreWidth = 0.28f;
        [SerializeField, Min(0f)] private float _glowWidth = 1.1f;
        [SerializeField, Min(0f)] private float _orbScale = 0.9f;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float _pulseSpeed = 22f;
        [SerializeField, Range(0f, 1f)] private float _pulseDepth = 0.25f;
        [SerializeField] private float _scrollSpeed = -6f;
        [SerializeField, Min(0f)] private float _growSharpness = 18f;

        private Material _coreMaterial;
        private Material _glowMaterial;
        private float _charge;
        private bool _firing;
        private float _width;
        private float _time;

        public void Show(float charge, bool firing)
        {
            _charge = Mathf.Clamp01(charge);

            if (_firing == firing)
                return;

            _firing = firing;

            if (_sparks != null)
            {
                if (firing)
                    _sparks.Play(true);
                else
                    _sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (_flare != null)
            {
                if (firing)
                    _flare.Play(true);
                else
                    _flare.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void Awake()
        {
            if (_core != null)
                _coreMaterial = _core.material;

            if (_glow != null)
                _glowMaterial = _glow.material;

            Layout();
        }

        private void OnEnable()
        {
            _charge = 0f;
            _firing = false;
            _width = 0f;
            _time = 0f;

            if (_sparks != null)
                _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (_flare != null)
                _flare.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Apply();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _time += deltaTime;

            float target = _firing ? 1f : 0f;
            _width = Mathf.Lerp(_width, target, 1f - Mathf.Exp(-_growSharpness * deltaTime));

            Apply();
        }

        private void Apply()
        {
            float pulse = 1f - _pulseDepth + _pulseDepth * (0.5f + 0.5f * Mathf.Sin(_time * _pulseSpeed));
            float flicker = 0.9f + 0.1f * Mathf.Sin(_time * 61f);

            if (_core != null)
                _core.widthMultiplier = _coreWidth * _width * pulse * flicker;

            if (_glow != null)
                _glow.widthMultiplier = _glowWidth * _width * pulse;

            Vector2 offset = new Vector2(_time * _scrollSpeed, 0f);

            if (_coreMaterial != null && _coreMaterial.HasProperty(MainTexId))
                _coreMaterial.SetTextureOffset(MainTexId, offset);

            if (_glowMaterial != null && _glowMaterial.HasProperty(MainTexId))
                _glowMaterial.SetTextureOffset(MainTexId, offset * 0.5f);

            if (_orb != null)
            {
                float orb = _firing ? 0.8f + 0.2f * pulse : _charge * (0.85f + 0.15f * Mathf.Sin(_time * 30f));
                _orb.localScale = Vector3.one * (_orbScale * orb);
            }
        }

        private void Layout()
        {
            foreach (LineRenderer line in new[] { _core, _glow })
            {
                if (line == null)
                    continue;

                line.useWorldSpace = false;
                line.positionCount = 2;
                line.SetPosition(0, Vector3.zero);
                line.SetPosition(1, Vector3.forward * _length);
            }

            if (_flare != null)
                _flare.transform.localPosition = Vector3.forward * _length;
        }

        private void OnDestroy()
        {
            if (_coreMaterial != null)
                Destroy(_coreMaterial);

            if (_glowMaterial != null)
                Destroy(_glowMaterial);
        }
    }
}
