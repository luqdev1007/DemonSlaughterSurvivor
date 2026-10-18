using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class RuneBeaconView : MonoBehaviour
    {
        [SerializeField] private RectTransform _layer;
        [SerializeField] private Image _glow;
        [SerializeField] private float _intervalSeconds = 9f;
        [SerializeField] private float _firstDelaySeconds = 3f;
        [SerializeField] private float _inSeconds = 0.6f;
        [SerializeField] private float _holdSeconds = 0.4f;
        [SerializeField] private float _outSeconds = 1.4f;
        [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.35f;
        [SerializeField] private Vector2 _edge = new Vector2(0.46f, 0.43f);
        [SerializeField] private float _onScreenMargin = 0.06f;

        private bool _present;
        private float _timer;
        private float _pulse = -1f;

        public bool IsWired => _layer != null && _glow != null;

        public void Show(bool present, bool hasViewport, Vector3 viewport, Color color)
        {
            if (present != _present)
            {
                _present = present;
                _timer = _firstDelaySeconds;
                _pulse = -1f;
            }

            if (present == false || hasViewport == false)
            {
                _glow.enabled = false;

                return;
            }

            Vector2 direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);

            if (viewport.z < 0f)
                direction = -direction;

            bool onScreen = viewport.z > 0f
                && viewport.x > _onScreenMargin && viewport.x < 1f - _onScreenMargin
                && viewport.y > _onScreenMargin && viewport.y < 1f - _onScreenMargin;

            float delta = Time.unscaledDeltaTime;

            if (_pulse < 0f)
            {
                _timer -= delta;

                if (_timer <= 0f && onScreen == false)
                {
                    _pulse = 0f;
                    _timer = _intervalSeconds;
                }
            }

            if (_pulse < 0f)
            {
                _glow.enabled = false;

                return;
            }

            _pulse += delta;

            float total = _inSeconds + _holdSeconds + _outSeconds;

            if (_pulse >= total)
            {
                _pulse = -1f;
                _glow.enabled = false;

                return;
            }

            float alpha;

            if (_pulse < _inSeconds)
                alpha = _pulse / _inSeconds;
            else if (_pulse < _inSeconds + _holdSeconds)
                alpha = 1f;
            else
                alpha = 1f - (_pulse - _inSeconds - _holdSeconds) / _outSeconds;

            if (direction.sqrMagnitude < 1e-6f)
                direction = Vector2.up;

            float scale = Mathf.Max(Mathf.Abs(direction.x) / _edge.x, Mathf.Abs(direction.y) / _edge.y);
            Vector2 onEdge = direction / Mathf.Max(scale, 1e-4f);

            Rect bounds = _layer.rect;
            RectTransform rect = _glow.rectTransform;
            rect.anchoredPosition = new Vector2(onEdge.x * bounds.width, onEdge.y * bounds.height);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            Color tint = color.linear;
            tint.a = _maxAlpha * alpha * alpha;

            _glow.color = tint;
            _glow.enabled = true;
        }
    }
}
