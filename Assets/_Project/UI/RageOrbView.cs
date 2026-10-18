using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class RageOrbView : MonoBehaviour
    {
        private const int GlowSize = 128;

        private static readonly int FullId = Shader.PropertyToID("_Full");
        private static readonly int LevelId = Shader.PropertyToID("_Level");
        private static readonly int ClockId = Shader.PropertyToID("_Clock");

        [SerializeField] private Image _fill;
        [SerializeField] private Material _fillMaterial;
        [SerializeField] private RectTransform _pulseRoot;
        [SerializeField] private Image _glow;
        [SerializeField] private Image _halo;
        [SerializeField] private Color _glowColor = new Color(1f, 0.32f, 0.05f, 0.9f);
        [SerializeField] private Color _haloColor = new Color(1f, 0.75f, 0.25f, 0.7f);

        [Header("Full")]
        [SerializeField] private float _fadeSeconds = 0.25f;
        [SerializeField] private float _pulseHz = 1.6f;
        [SerializeField] private float _pulseScale = 0.05f;
        [SerializeField] private float _popScale = 0.28f;
        [SerializeField] private float _popSeconds = 0.45f;
        [SerializeField] private float _haloSpinDegrees = 70f;

        private Material _material;
        private Texture2D _glowTexture;
        private Sprite _glowSprite;
        private float _full;
        private float _target;
        private float _pop;
        private float _time;

        public void Show(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);

            if (_material != null)
                _material.SetFloat(LevelId, fraction);

            float target = fraction >= 0.999f ? 1f : 0f;

            if (target > _target)
                _pop = 1f;

            _target = target;
        }

        private void Awake()
        {
            if (_fill != null && _fillMaterial != null)
            {
                _material = new Material(_fillMaterial);
                _fill.material = _material;
            }

            _glowTexture = BuildGlowTexture();
            _glowSprite = Sprite.Create(_glowTexture, new Rect(0f, 0f, GlowSize, GlowSize), new Vector2(0.5f, 0.5f));

            Prepare(_glow);
            Prepare(_halo);

            Apply();
        }

        private void Prepare(Image image)
        {
            if (image == null)
                return;

            image.sprite = _glowSprite;
            image.raycastTarget = false;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;

            _time += delta;
            _full = Mathf.MoveTowards(_full, _target, _fadeSeconds > 0f ? delta / _fadeSeconds : 1f);
            _pop = Mathf.MoveTowards(_pop, 0f, _popSeconds > 0f ? delta / _popSeconds : 1f);

            Apply();
        }

        private void Apply()
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(_time * _pulseHz * Mathf.PI * 2f);
            float pop = _pop * _pop;

            if (_material != null)
            {
                _material.SetFloat(FullId, _full);
                _material.SetFloat(ClockId, _time);
            }

            if (_pulseRoot != null)
            {
                float scale = 1f + _full * _pulseScale * wave + pop * _popScale;
                _pulseRoot.localScale = new Vector3(scale, scale, 1f);
            }

            if (_glow != null)
            {
                Color color = _glowColor.linear;
                color.a = Mathf.Clamp01(_glowColor.a * _full * (0.55f + 0.45f * wave) + pop * 0.6f);
                _glow.color = color;
                _glow.enabled = color.a > 0.001f;
                _glow.rectTransform.localScale = Vector3.one * (1f + 0.12f * wave * _full + pop * 0.5f);
            }

            if (_halo != null)
            {
                Color color = _haloColor.linear;
                color.a = _haloColor.a * _full * (0.35f + 0.65f * (1f - wave));
                _halo.color = color;
                _halo.enabled = color.a > 0.001f;
                _halo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -_time * _haloSpinDegrees);
                _halo.rectTransform.localScale = new Vector3(1.05f + 0.08f * wave, 0.85f + 0.08f * (1f - wave), 1f);
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);

            if (_glowSprite != null)
                Destroy(_glowSprite);

            if (_glowTexture != null)
                Destroy(_glowTexture);
        }

        private static Texture2D BuildGlowTexture()
        {
            Texture2D texture = new Texture2D(GlowSize, GlowSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = "RageOrbGlow";

            Color32[] pixels = new Color32[GlowSize * GlowSize];
            float half = (GlowSize - 1) * 0.5f;

            for (int y = 0; y < GlowSize; y++)
            {
                for (int x = 0; x < GlowSize; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Exp(-Mathf.Pow((distance - 0.62f) / 0.22f, 2f));
                    float core = Mathf.Clamp01(1f - distance) * 0.35f;
                    float alpha = Mathf.Clamp01(ring + core) * Mathf.Clamp01((1f - distance) * 4f);

                    pixels[y * GlowSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return texture;
        }
    }
}
