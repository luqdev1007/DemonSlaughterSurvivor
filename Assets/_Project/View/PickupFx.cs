using UnityEngine;

namespace Game.View
{
    public sealed class PickupFx : MonoBehaviour
    {
        [SerializeField] private Transform _body;
        [SerializeField] private SpriteRenderer _glow;
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private Color _color = new Color(0.35f, 0.65f, 1f, 1f);

        [Header("Ground glow")]
        [SerializeField] private float _glowSize = 0.9f;
        [SerializeField] private float _glowAlpha = 0.55f;
        [SerializeField] private float _glowPulse = 0.25f;
        [SerializeField] private float _glowPulseHz = 1.1f;
        [SerializeField] private float _glowHeight = 0.03f;

        [Header("Flight")]
        [SerializeField] private float _stretch = 0.7f;
        [SerializeField] private float _endScale = 0.3f;
        [SerializeField] private float _trailWidth = 0.18f;

        private Vector3 _bodyScale = Vector3.one;
        private float _flight;
        private float _phase;
        private bool _flying;

        public void SetFlight(float progress)
        {
            _flight = Mathf.Clamp01(progress);

            bool flying = _flight > 0f;

            if (flying == _flying)
                return;

            _flying = flying;

            if (_trail == null)
                return;

            _trail.Clear();
            _trail.emitting = flying;
        }

        private void Awake()
        {
            if (_body != null)
                _bodyScale = _body.localScale;

            _phase = Random.value * Mathf.PI * 2f;

            if (_glow != null)
            {
                _glow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                _glow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _glow.receiveShadows = false;
            }

            if (_trail != null)
            {
                _trail.emitting = false;
                _trail.widthMultiplier = _trailWidth;
                _trail.startColor = _color;
                _trail.endColor = new Color(_color.r, _color.g, _color.b, 0f);
            }
        }

        private void OnEnable()
        {
            _flying = false;
            _flight = 0f;

            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = false;
            }
        }

        private void LateUpdate()
        {
            ApplyGlow();
            ApplyBody();
        }

        private void ApplyGlow()
        {
            if (_glow == null)
                return;

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * _glowPulseHz * Mathf.PI * 2f + _phase);
            float fade = 1f - Mathf.Clamp01(_flight * 4f);
            float size = _glowSize * (1f + _glowPulse * wave);

            Transform glow = _glow.transform;
            Vector3 position = transform.position;
            glow.position = new Vector3(position.x, _glowHeight, position.z);
            glow.rotation = Quaternion.Euler(90f, 0f, 0f);
            glow.localScale = new Vector3(size, size, size);

            Color color = _color;
            color.a = _glowAlpha * (0.7f + 0.3f * wave) * fade;
            _glow.color = color;
            _glow.enabled = color.a > 0.001f;
        }

        private void ApplyBody()
        {
            if (_body == null)
                return;

            if (_flying == false)
            {
                _body.localScale = _bodyScale;

                return;
            }

            float grow = Mathf.Sin(Mathf.Min(_flight, 0.35f) / 0.35f * Mathf.PI * 0.5f) * _stretch;
            float shrink = Mathf.Lerp(1f, _endScale, _flight * _flight);
            float scale = (1f + grow * (1f - _flight)) * shrink;

            _body.localScale = _bodyScale * scale;
        }
    }
}
