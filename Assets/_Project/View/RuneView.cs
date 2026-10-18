using UnityEngine;

namespace Game.View
{
    public sealed class RuneView : MonoBehaviour
    {
        [SerializeField] private Transform _glyph;
        [SerializeField] private Transform _halo;
        [SerializeField] private SpriteRenderer _groundGlow;

        [Header("Levitation")]
        [SerializeField] private float _height = 1.2f;
        [SerializeField] private float _bobHeight = 0.22f;
        [SerializeField] private float _bobHz = 0.55f;
        [SerializeField] private float _spinDegreesPerSecond = 70f;
        [SerializeField] private float _tiltDegrees = 8f;

        [Header("Pulse")]
        [SerializeField] private float _pulseHz = 1.1f;
        [SerializeField] private float _pulseScale = 0.08f;
        [SerializeField] private float _haloSpinDegreesPerSecond = -35f;

        private Vector3 _glyphScale = Vector3.one;
        private Vector3 _haloScale = Vector3.one;
        private Color _glowColor = Color.white;
        private float _phase;

        private void Awake()
        {
            if (_glyph != null)
                _glyphScale = _glyph.localScale;

            if (_halo != null)
                _haloScale = _halo.localScale;

            if (_groundGlow != null)
                _glowColor = _groundGlow.color;

            _phase = Random.value * Mathf.PI * 2f;
        }

        private void LateUpdate()
        {
            float time = Time.time;
            float bob = Mathf.Sin(time * _bobHz * Mathf.PI * 2f + _phase);
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * _pulseHz * Mathf.PI * 2f + _phase);

            if (_glyph != null)
            {
                _glyph.localPosition = new Vector3(0f, _height + bob * _bobHeight, 0f);
                _glyph.localRotation = Quaternion.Euler(Mathf.Sin(time * 0.7f + _phase) * _tiltDegrees, time * _spinDegreesPerSecond, 0f);
                _glyph.localScale = _glyphScale * (1f + _pulseScale * pulse);
            }

            if (_halo != null)
            {
                _halo.localRotation = Quaternion.Euler(90f, time * _haloSpinDegreesPerSecond, 0f);
                _halo.localScale = _haloScale * (1f + 0.06f * pulse);
            }

            if (_groundGlow != null)
            {
                Color color = _glowColor;
                color.a = _glowColor.a * (0.6f + 0.4f * pulse);
                _groundGlow.color = color;
            }
        }
    }
}
