using UnityEngine;
using UnityEngine.Rendering;

namespace Game.View
{
    public sealed class LightningBoltFx : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField, Min(1)] private int _strands = 3;
        [SerializeField, Min(3)] private int _points = 14;
        [SerializeField, Min(0f)] private float _jitter = 0.35f;
        [SerializeField, Min(0f)] private float _width = 0.14f;
        [SerializeField, Range(0f, 1f)] private float _coreWidthShare = 0.35f;
        [SerializeField] private Color _color = new Color(0.35f, 0.75f, 1f, 1f);
        [SerializeField] private Color _coreColor = new Color(0.9f, 0.97f, 1f, 1f);
        [SerializeField, Min(0.02f)] private float _seconds = 0.4f;
        [SerializeField, Min(0.01f)] private float _refreshSeconds = 0.035f;

        [Header("Hits")]
        [SerializeField] private ParticleSystem _fromHit;
        [SerializeField] private ParticleSystem _toHit;

        private readonly System.Random _random = new System.Random();

        private LineRenderer[] _lines;
        private LineRenderer[] _cores;
        private Vector3[] _buffer;
        private Vector3 _from;
        private Vector3 _to;
        private float _age;
        private float _refresh;
        private bool _active;

        public void Strike(Vector3 from, Vector3 to)
        {
            EnsureBuilt();

            _from = from;
            _to = to;
            _age = 0f;
            _refresh = 0f;
            _active = (to - from).sqrMagnitude > 0.01f;

            SetLinesEnabled(_active);

            if (_active)
                Rebuild();

            Play(_fromHit, from, _active);
            Play(_toHit, to, true);
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnDisable()
        {
            _active = false;

            if (_lines != null)
                SetLinesEnabled(false);
        }

        private void Update()
        {
            if (_active == false)
                return;

            float delta = Time.deltaTime;

            _age += delta;
            _refresh -= delta;

            if (_age >= _seconds)
            {
                _active = false;
                SetLinesEnabled(false);

                return;
            }

            if (_refresh <= 0f)
                Rebuild();

            ApplyColor(1f - _age / _seconds);
        }

        private void EnsureBuilt()
        {
            if (_lines != null)
                return;

            _lines = new LineRenderer[_strands];
            _cores = new LineRenderer[_strands];
            _buffer = new Vector3[_points];

            for (int index = 0; index < _strands; index++)
            {
                float share = index == 0 ? 1f : 0.55f;

                _lines[index] = CreateLine("Bolt" + index, _width * share);
                _cores[index] = CreateLine("BoltCore" + index, _width * share * _coreWidthShare);
            }
        }

        private LineRenderer CreateLine(string name, float width)
        {
            GameObject strand = new GameObject(name);
            strand.transform.SetParent(transform, false);

            LineRenderer line = strand.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = _material;
            line.positionCount = _points;
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

        private void Rebuild()
        {
            _refresh = _refreshSeconds;

            Vector3 along = _to - _from;
            float length = along.magnitude;
            Vector3 side = Vector3.Cross(along, Vector3.up);

            if (side.sqrMagnitude < 0.0001f)
                side = Vector3.right;

            side.Normalize();

            float jitter = _jitter * Mathf.Clamp(length / 3f, 0.4f, 1.5f);
            int last = _points - 1;

            for (int strand = 0; strand < _lines.Length; strand++)
            {
                float strength = strand == 0 ? 1f : 1.6f;

                for (int point = 0; point <= last; point++)
                {
                    float t = (float)point / last;
                    float bend = Mathf.Sin(t * Mathf.PI) * jitter * strength;
                    Vector3 offset = side * Range(-1f, 1f) * bend + Vector3.up * Range(-1f, 1f) * bend * 0.6f;

                    _buffer[point] = Vector3.Lerp(_from, _to, t) + offset;
                }

                _lines[strand].SetPositions(_buffer);
                _cores[strand].SetPositions(_buffer);
            }

            ApplyColor(1f - _age / _seconds);
        }

        private void ApplyColor(float life)
        {
            float flicker = 0.75f + Range(0f, 0.25f);

            for (int strand = 0; strand < _lines.Length; strand++)
            {
                float share = strand == 0 ? 1f : 0.6f;

                Color color = _color;
                color.a *= life * flicker * share;
                _lines[strand].startColor = color;
                _lines[strand].endColor = color;

                Color core = _coreColor;
                core.a *= life * flicker * share;
                _cores[strand].startColor = core;
                _cores[strand].endColor = core;
            }
        }

        private void SetLinesEnabled(bool enabled)
        {
            for (int strand = 0; strand < _lines.Length; strand++)
            {
                _lines[strand].enabled = enabled;
                _cores[strand].enabled = enabled;
            }
        }

        private static void Play(ParticleSystem hit, Vector3 at, bool play)
        {
            if (hit == null)
                return;

            hit.transform.position = at;
            hit.Clear(true);

            if (play)
                hit.Play(true);
        }

        private float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }
    }
}
