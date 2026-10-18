using UnityEngine;

namespace Game.View
{
    public sealed class RuneAuraFx : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int FillId = Shader.PropertyToID("_Fill");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int GlyphId = Shader.PropertyToID("_Glyph");

        [SerializeField] private MeshRenderer _ring;
        [SerializeField] private float _height = 0.05f;
        [SerializeField] private float _fadeSeconds = 0.25f;
        [SerializeField] private float _warningSeconds = 2f;
        [SerializeField] private float _warningHz = 4f;

        private Material _material;
        private bool _active;
        private float _alpha;
        private float _fill;
        private float _remaining;
        private Texture _glyph;

        public void Show(bool active, Color color, Texture glyph, float fraction, float remainingSeconds)
        {
            _active = active;

            if (active == false || _material == null)
                return;

            _fill = Mathf.Clamp01(fraction);
            _remaining = remainingSeconds;
            _material.SetColor(ColorId, color);

            if (glyph != _glyph)
            {
                _glyph = glyph;
                _material.SetTexture(GlyphId, glyph);
            }
        }

        private void Awake()
        {
            if (_ring == null)
                return;

            _material = _ring.material;
            _ring.enabled = false;
        }

        private void LateUpdate()
        {
            if (_material == null)
                return;

            float step = _fadeSeconds > 0f ? Time.deltaTime / _fadeSeconds : 1f;
            _alpha = Mathf.MoveTowards(_alpha, _active ? 1f : 0f, step);

            _ring.enabled = _alpha > 0.001f;

            if (_ring.enabled == false)
                return;

            float blink = _remaining < _warningSeconds ? 0.55f + 0.45f * Mathf.Cos(Time.time * _warningHz * Mathf.PI * 2f) : 1f;

            _material.SetFloat(FillId, _fill);
            _material.SetFloat(AlphaId, _alpha * blink);

            Transform ring = _ring.transform;
            Vector3 position = transform.position;
            ring.position = new Vector3(position.x, _height, position.z);
            ring.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
