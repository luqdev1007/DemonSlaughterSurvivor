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

        [Header("Body anchors")]
        [SerializeField] private Transform[] _anchors = System.Array.Empty<Transform>();
        [SerializeField, Range(0f, 1f)] private float _anchorShare = 0.75f;
        [SerializeField, Min(0f)] private float _anchorSpread = 0.08f;

        [Header("Core")]
        [SerializeField] private Color _coreColor = new Color(1f, 0.85f, 0.6f, 1f);
        [SerializeField, Range(0f, 1f)] private float _coreWidthShare = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _flicker = 0.45f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem[] _loops = System.Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem _burst;

        private readonly System.Random _random = new System.Random();

        private LineRenderer[] _arcs;
        private LineRenderer[] _cores;
        private float[] _arcAlphas;
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

            SetParticles(value);

            if (value && _burst != null)
            {
                _burst.Clear(true);
                _burst.Play(true);
            }
        }

        public void ResetImmediate()
        {
            _isShowing = false;
            _alpha = 0f;

            SetParticles(false);

            for (int index = 0; index < _loops.Length; index++)
            {
                if (_loops[index] != null)
                    _loops[index].Clear(true);
            }

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
                ApplyColor(index);
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

                if (_cores != null && _cores[index] != null)
                {
                    _cores[index].sharedMaterial = _material;
                    _cores[index].widthMultiplier = _width * _coreWidthShare;
                }
            }

            ApplyColor();
        }

        private void EnsureBuilt()
        {
            if (_arcs != null)
                return;

            _arcs = new LineRenderer[_arcCount];
            _cores = new LineRenderer[_arcCount];
            _arcAlphas = new float[_arcCount];
            _refreshCountdowns = new float[_arcCount];
            _points = new Vector3[_pointsPerArc];
            _widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

            for (int index = 0; index < _arcCount; index++)
            {
                _arcs[index] = CreateLine("Arc" + index, _width);
                _cores[index] = CreateLine("ArcCore" + index, _width * _coreWidthShare);
                _arcAlphas[index] = 1f;
                _refreshCountdowns[index] = Range(0f, _maxRefreshSeconds);
            }

            ApplyColor();
        }

        private LineRenderer CreateLine(string name, float width)
        {
            GameObject arc = new GameObject(name);
            arc.transform.SetParent(transform, false);

            LineRenderer line = arc.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = _material;
            line.positionCount = _pointsPerArc;
            line.widthCurve = _widthCurve;
            line.widthMultiplier = width;
            line.numCapVertices = 0;
            line.numCornerVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.enabled = false;

            return line;
        }

        private Vector3 PickPoint()
        {
            if (_anchors.Length < 2 || Range(0f, 1f) > _anchorShare)
                return PointOnEllipsoid();

            Transform anchor = _anchors[_random.Next(_anchors.Length)];

            if (anchor == null)
                return PointOnEllipsoid();

            return transform.InverseTransformPoint(anchor.position) + RandomInCube() * _anchorSpread;
        }

        private void RebuildArc(int index)
        {
            Vector3 start = PickPoint();
            Vector3 end = PickPoint();
            int last = _points.Length - 1;

            for (int point = 0; point <= last; point++)
            {
                float t = (float)point / last;
                float bend = Mathf.Sin(t * Mathf.PI) * _jitter;

                _points[point] = Vector3.Lerp(start, end, t) + RandomInCube() * bend;
            }

            _arcs[index].SetPositions(_points);
            _cores[index].SetPositions(_points);
            _arcAlphas[index] = 1f - Range(0f, _flicker);
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

            for (int index = 0; index < _arcs.Length; index++)
                ApplyColor(index);
        }

        private void ApplyColor(int index)
        {
            if (_arcs[index] == null)
                return;

            float alpha = _alpha * _arcAlphas[index];

            Color color = _color;
            color.a *= alpha;

            _arcs[index].startColor = color;
            _arcs[index].endColor = color;

            Color core = _coreColor;
            core.a *= alpha;

            _cores[index].startColor = core;
            _cores[index].endColor = core;
        }

        private void SetArcsEnabled(bool enabled)
        {
            _arcsEnabled = enabled;

            for (int index = 0; index < _arcs.Length; index++)
            {
                _arcs[index].enabled = enabled;
                _cores[index].enabled = enabled;
            }
        }

        private void SetParticles(bool playing)
        {
            for (int index = 0; index < _loops.Length; index++)
            {
                ParticleSystem loop = _loops[index];

                if (loop == null)
                    continue;

                if (playing)
                {
                    if (loop.isPlaying == false)
                        loop.Play(true);

                    continue;
                }

                loop.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
