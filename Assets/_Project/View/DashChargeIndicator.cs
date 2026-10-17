using UnityEngine;

namespace Game.View
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class DashChargeIndicator : MonoBehaviour
    {
        private const int Segments = 48;

        [SerializeField] private LineRenderer _line;
        [SerializeField] private float _minRadius = 0.6f;
        [SerializeField] private float _maxRadius = 1.3f;
        [SerializeField] private float _minWidth = 0.03f;
        [SerializeField] private float _maxWidth = 0.09f;
        [SerializeField] private float _height = 0.05f;
        [SerializeField] private Color _emptyColor = new Color(1f, 0.55f, 0.15f, 0.35f);
        [SerializeField] private Color _fullColor = new Color(1f, 0.25f, 0.05f, 1f);
        [SerializeField] private float _fullPulseHertz = 6f;

        private bool _visible;
        private float _strength;

        public void Show(float strength)
        {
            _strength = Mathf.Clamp01(strength);

            if (_visible == false)
            {
                _visible = true;
                _line.enabled = true;
            }

            Redraw();
        }

        public void Hide()
        {
            if (_visible == false)
                return;

            _visible = false;
            _line.enabled = false;
        }

        private void Awake()
        {
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.positionCount = Segments;
            _line.enabled = false;
        }

        private void Redraw()
        {
            float radius = Mathf.Lerp(_minRadius, _maxRadius, _strength);
            float width = Mathf.Lerp(_minWidth, _maxWidth, _strength);
            Color color = Color.Lerp(_emptyColor, _fullColor, _strength);

            if (_strength >= 1f)
                color.a *= 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * _fullPulseHertz * Mathf.PI));

            for (int index = 0; index < Segments; index++)
            {
                float angle = index * Mathf.PI * 2f / Segments;
                _line.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, _height, Mathf.Sin(angle) * radius));
            }

            _line.startWidth = width;
            _line.endWidth = width;
            _line.startColor = color;
            _line.endColor = color;
        }
    }
}
