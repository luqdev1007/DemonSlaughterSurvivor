using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class HudView : MonoBehaviour
    {
        private const int VignetteSize = 256;

        [SerializeField] private Image _healthFill;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private Image _ultimateChargeFill;

        [Header("Berserk vignette")]
        [SerializeField] private Image _vignette;
        [SerializeField] private Color _vignetteColor = new Color(0.55f, 0f, 0f, 0.85f);
        [SerializeField] private float _vignetteInner = 0.55f;
        [SerializeField] private float _vignetteOuter = 1.25f;
        [SerializeField] private float _vignetteFadeSeconds = 0.35f;

        private Texture2D _vignetteTexture;
        private Sprite _vignetteSprite;
        private float _vignetteAlpha;
        private float _vignetteTarget;

        public bool IsWired => _healthFill != null && _healthText != null && _ultimateChargeFill != null && _vignette != null;

        public float VignetteAlpha => _vignetteAlpha;

        public void ShowHealth(float health, float maxHealth)
        {
            float fraction = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;

            _healthFill.fillAmount = fraction;
            _healthText.SetText("{0} / {1}", Mathf.Ceil(health), Mathf.Ceil(maxHealth));
        }

        public void ShowUltimateCharge(float fraction)
        {
            _ultimateChargeFill.fillAmount = Mathf.Clamp01(fraction);
        }

        public void ShowBerserk(bool active)
        {
            _vignetteTarget = active ? 1f : 0f;
        }

        private void Awake()
        {
            if (_vignette == null)
                return;

            _vignetteTexture = BuildVignetteTexture(_vignetteInner, _vignetteOuter);
            _vignetteSprite = Sprite.Create(_vignetteTexture, new Rect(0f, 0f, VignetteSize, VignetteSize), new Vector2(0.5f, 0.5f));

            _vignette.sprite = _vignetteSprite;
            _vignette.raycastTarget = false;

            ApplyVignette();
        }

        private void Update()
        {
            if (_vignette == null || _vignetteAlpha == _vignetteTarget)
                return;

            float step = _vignetteFadeSeconds > 0f ? Time.unscaledDeltaTime / _vignetteFadeSeconds : 1f;

            _vignetteAlpha = Mathf.MoveTowards(_vignetteAlpha, _vignetteTarget, step);

            ApplyVignette();
        }

        private void OnDestroy()
        {
            if (_vignetteSprite != null)
                Destroy(_vignetteSprite);

            if (_vignetteTexture != null)
                Destroy(_vignetteTexture);
        }

        private void ApplyVignette()
        {
            Color color = _vignetteColor;
            color.a *= _vignetteAlpha;

            _vignette.color = color;
            _vignette.enabled = _vignetteAlpha > 0f;
        }

        private static Texture2D BuildVignetteTexture(float inner, float outer)
        {
            Texture2D texture = new Texture2D(VignetteSize, VignetteSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = "BerserkVignette";

            Color32[] pixels = new Color32[VignetteSize * VignetteSize];
            float half = (VignetteSize - 1) * 0.5f;

            for (int y = 0; y < VignetteSize; y++)
            {
                for (int x = 0; x < VignetteSize; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, outer, distance));

                    pixels[y * VignetteSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return texture;
        }
    }
}
