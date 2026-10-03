using UnityEngine;
using UnityEngine.Rendering;

namespace Game.View
{
    public sealed class BerserkLightningFx : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField, Min(1)] private int _arcCount = 7;
        [SerializeField, Min(3)] private int _pointsPerArc = 8;
        [SerializeField] private Vector3 _radii = new Vector3(0.75f, 0.9f, 0.75f);
        [SerializeField, Min(0f)] private float _jitter = 0.15f;
        [SerializeField, Min(0.01f)] private float _minRefreshSeconds = 0.05f;
        [SerializeField, Min(0.01f)] private float _maxRefreshSeconds = 0.08f;
        [SerializeField, Min(0f)] private float _width = 0.04f;
        [SerializeField] private Color _color = new Color(1f, 0.06f, 0.02f, 1f);
        [SerializeField, Min(0f)] private float _fadeSeconds = 0.15f;

        private readonly System.Random _random = new System.Random();

        private LineRenderer[] _arcs;
        private float[] _refreshCountdowns;
        private Vector3[] _points;
        private AnimationCurve _widthCurve;
        private bool _isShowing;
        private bool _arcsEnabled;
        private float _alpha;

        public bool IsShowing => _isShowing;

        public float Alpha => _alpha;

        public void SetShowing(bool value)
        {
            if (_isShowing == value)
                return;

            _isShowing = value;

            EnsureBuilt();

            if (value && _arcsEnabled == false)
            {
                for (int index = 0; index < _arcs.Length; index++)
                    RebuildArc(index);

                ApplyColor();
                SetArcsEnabled(true);
            }
        }

        public void ResetImmediate()
        {
            _isShowing = false;
            _alpha = 0f;

            if (_arcs == null)
                return;

            ApplyColor();
            SetArcsEnabled(false);
        }

        public void Tick(float deltaTime)
        {
            if (_arcsEnabled == false)
                return;

            float target = _isShowing ? 1f : 0f;

            if (_alpha != target)
            {
                float step = _fadeSeconds > 0f ? deltaTime / _fadeSeconds : 1f;

                _alpha = Mathf.MoveTowards(_alpha, target, step);

                ApplyColor();

                if (_alpha <= 0f)
                {
                    SetArcsEnabled(false);

                    return;
                }
            }

            for (int index = 0; index < _arcs.Length; index++)
            {
                _refreshCountdowns[index] -= deltaTime;

                if (_refreshCountdowns[index] > 0f)
                    continue;

                RebuildArc(index);
            }
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnValidate()
        {
            if (_maxRefreshSeconds < _minRefreshSeconds)
                _maxRefreshSeconds = _minRefreshSeconds;

            if (_arcs == null)
                return;

            for (int index = 0; index < _arcs.Length; index++)
            {
                if (_arcs[index] == null)
                    continue;

                _arcs[index].sharedMaterial = _material;
                _arcs[index].widthMultiplier = _width;
            }

            ApplyColor();
        }

        private void EnsureBuilt()
        {
            if (_arcs != null)
                return;

            _arcs = new LineRenderer[_arcCount];
            _refreshCountdowns = new float[_arcCount];
            _points = new Vector3[_pointsPerArc];
            _widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

            for (int index = 0; index < _arcCount; index++)
            {
                GameObject arc = new GameObject("Arc" + index);
                arc.transform.SetParent(transform, false);

                LineRenderer line = arc.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.alignment = LineAlignment.View;
                line.sharedMaterial = _material;
                line.positionCount = _pointsPerArc;
                line.widthCurve = _widthCurve;
                line.widthMultiplier = _width;
                line.numCapVertices = 0;
                line.numCornerVertices = 0;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.lightProbeUsage = LightProbeUsage.Off;
                line.reflectionProbeUsage = ReflectionProbeUsage.Off;
                line.enabled = false;

                _arcs[index] = line;
                _refreshCountdowns[index] = Range(0f, _maxRefreshSeconds);
            }

            ApplyColor();
        }

        private void RebuildArc(int index)
        {
            Vector3 start = PointOnEllipsoid();
            Vector3 end = PointOnEllipsoid();
            int last = _points.Length - 1;

            for (int point = 0; point <= last; point++)
            {
                float t = (float)point / last;
                float bend = Mathf.Sin(t * Mathf.PI) * _jitter;

                _points[point] = Vector3.Lerp(start, end, t) + RandomInCube() * bend;
            }

            _arcs[index].SetPositions(_points);
            _refreshCountdowns[index] = Range(_minRefreshSeconds, _maxRefreshSeconds);
        }

        private Vector3 PointOnEllipsoid()
        {
            Vector3 direction = RandomInCube();

            while (direction.sqrMagnitude < 0.0001f)
                direction = RandomInCube();

            direction.Normalize();

            return Vector3.Scale(direction, _radii);
        }

        private Vector3 RandomInCube()
        {
            return new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f));
        }

        private float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        private void ApplyColor()
        {
            if (_arcs == null)
                return;

            Color color = _color;
            color.a *= _alpha;

            for (int index = 0; index < _arcs.Length; index++)
            {
                if (_arcs[index] == null)
                    continue;

                _arcs[index].startColor = color;
                _arcs[index].endColor = color;
            }
        }

        private void SetArcsEnabled(bool enabled)
        {
            _arcsEnabled = enabled;

            for (int index = 0; index < _arcs.Length; index++)
                _arcs[index].enabled = enabled;
        }
    }
}
