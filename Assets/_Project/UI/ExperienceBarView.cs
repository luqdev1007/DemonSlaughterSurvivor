using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class ExperienceBarView : MonoBehaviour
    {
        private static readonly int ClockId = Shader.PropertyToID("_Clock");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");

        [SerializeField] private Image _fill;
        [SerializeField] private Material _fillMaterial;
        [SerializeField] private RectTransform _badge;
        [SerializeField] private float _flashSeconds = 0.6f;
        [SerializeField] private float _badgePopScale = 0.35f;

        private Material _material;
        private float _flash;
        private float _time;
        private int _level = -1;

        public void ShowLevel(int level)
        {
            if (_level >= 0 && level > _level)
                _flash = 1f;

            _level = level;
        }

        private void Awake()
        {
            if (_fill == null || _fillMaterial == null)
                return;

            _material = new Material(_fillMaterial);
            _fill.material = _material;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;

            _time += delta;
            _flash = Mathf.MoveTowards(_flash, 0f, _flashSeconds > 0f ? delta / _flashSeconds : 1f);

            if (_material != null)
            {
                _material.SetFloat(ClockId, _time);
                _material.SetFloat(FlashId, _flash * _flash);
            }

            if (_badge != null)
            {
                float scale = 1f + _badgePopScale * _flash * _flash;
                _badge.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
