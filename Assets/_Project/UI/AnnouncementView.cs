using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class AnnouncementView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _inSeconds = 0.18f;
        [SerializeField] private float _holdSeconds = 1.3f;
        [SerializeField] private float _outSeconds = 0.45f;
        [SerializeField] private float _popScale = 1.6f;
        [SerializeField] private float _shakePixels = 6f;
        [SerializeField] private float _riseOutPixels = 30f;

        private RectTransform _rect;
        private Vector2 _basePosition;
        private float _age = -1f;

        public bool IsWired => _text != null;

        public void Show(string text)
        {
            _text.SetText(text);
            _text.gameObject.SetActive(true);
            _age = 0f;
            Apply();
        }

        private void Awake()
        {
            if (_text == null)
                return;

            _rect = _text.rectTransform;
            _basePosition = _rect.anchoredPosition;
            _text.raycastTarget = false;
            _text.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_age < 0f)
                return;

            _age += Time.unscaledDeltaTime;

            if (_age >= _inSeconds + _holdSeconds + _outSeconds)
            {
                _age = -1f;
                _text.gameObject.SetActive(false);

                return;
            }

            Apply();
        }

        private void Apply()
        {
            float scale;
            float alpha;
            Vector2 offset = Vector2.zero;

            if (_age < _inSeconds)
            {
                float t = _age / _inSeconds;
                scale = Mathf.Lerp(_popScale, 1f, 1f - (1f - t) * (1f - t));
                alpha = t;
                offset = new Vector2(Mathf.Sin(_age * 90f), Mathf.Cos(_age * 77f)) * _shakePixels * (1f - t);
            }
            else if (_age < _inSeconds + _holdSeconds)
            {
                float t = (_age - _inSeconds) / _holdSeconds;
                scale = 1f + 0.03f * Mathf.Sin(t * Mathf.PI * 4f);
                alpha = 1f;
            }
            else
            {
                float t = (_age - _inSeconds - _holdSeconds) / _outSeconds;
                scale = 1f + 0.1f * t;
                alpha = 1f - t;
                offset = new Vector2(0f, _riseOutPixels * t);
            }

            _rect.localScale = new Vector3(scale, scale, 1f);
            _rect.anchoredPosition = _basePosition + offset;
            _text.alpha = alpha;
        }
    }
}
