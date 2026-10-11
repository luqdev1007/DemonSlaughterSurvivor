using UnityEngine;
using UnityEngine.Rendering;

namespace Game.View
{
    public sealed class LeapImpactFx : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField, Min(1)] private int _boltCount = 10;
        [SerializeField, Min(3)] private int _pointsPerBolt = 10;
        [SerializeField, Min(0f)] private float _innerRadius = 0.3f;
        [SerializeField, Min(0.1f)] private float _radius = 4f;
        [SerializeField, Min(0f)] private float _height = 0.08f;
        [SerializeField, Min(0f)] private float _jitter = 0.35f;
        [SerializeField, Min(0f)] private float _width = 0.14f;
        [SerializeField] private Color _color = new Color(1f, 0.06f, 0.02f, 1f);
        [SerializeField, Min(0.01f)] private float _durationSeconds = 0.4f;
        [SerializeField, Range(0.01f, 1f)] private float _growShare = 0.3f;
        [SerializeField, Min(0.01f)] private float _refreshSeconds = 0.04f;

        private readonly System.Random _random = new System.Random();

        private LineRenderer[] _bolts;
        private float[] _angles;
        private Vector3[] _points;
        private float _elapsed;
        private float _refreshCountdown;

        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnEnable()
        {
            EnsureBuilt();

            _elapsed = 0f;
            _refreshCountdown = 0f;

            float offset = Range(0f, 360f);

            for (int index = 0; index < _angles.Length; index++)
                _angles[index] = offset + 360f * index / _angles.Length + Range(-12f, 12f);

            SetBoltsEnabled(true);
            Refresh(0f);
        }

        private void OnDisable()
        {
            if (_bolts != null)
                SetBoltsEnabled(false);
        }

        private void Update()
        {
            if (_elapsed >= _durationSeconds)
                return;

            _elapsed += Time.deltaTime;

            if (_elapsed >= _durationSeconds)
            {
                SetBoltsEnabled(false);

                return;
            }

            float progress = _elapsed / _durationSeconds;

            _refreshCountdown -= Time.deltaTime;

            if (_refreshCountdown <= 0f)
                Refresh(progress);

            ApplyFade(progress);
        }

        private void Refresh(float progress)
        {
            float grow = Mathf.Clamp01(progress / _growShare);
            float reach = Mathf.Lerp(_innerRadius, _radius, 1f - (1f - grow) * (1f - grow));
            int last = _points.Length - 1;

            for (int bolt = 0; bolt < _bolts.Length; bolt++)
            {
                float radians = _angles[bolt] * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                Vector3 side = new Vector3(direction.z, 0f, -direction.x);
                float length = reach * Range(0.75f, 1f);

                for (int point = 0; point <= last; point++)
                {
                    float t = (float)point / last;
                    float distance = Mathf.Lerp(_innerRadius, length, t);
                    float bend = point == 0 ? 0f : Range(-1f, 1f) * _jitter * Mathf.Lerp(0.4f, 1f, t);

                    _points[point] = direction * distance + side * bend + Vector3.up * _height;
                }

                _bolts[bolt].SetPositions(_points);
            }

            _refreshCountdown = _refreshSeconds;
        }

        private void ApplyFade(float progress)
        {
            float alpha = 1f - progress * progress;
            float flicker = Range(0.75f, 1f);

            Color color = _color;
            color.a *= alpha * flicker;

            Color tail = color;
            tail.a *= 0.35f;

            for (int index = 0; index < _bolts.Length; index++)
            {
                _bolts[index].startColor = color;
                _bolts[index].endColor = tail;
                _bolts[index].widthMultiplier = _width * Mathf.Lerp(1f, 0.4f, progress);
            }
        }

        private void EnsureBuilt()
        {
            if (_bolts != null)
                return;

            _bolts = new LineRenderer[_boltCount];
            _angles = new float[_boltCount];
            _points = new Vector3[_pointsPerBolt];

            AnimationCurve widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f));

            for (int index = 0; index < _boltCount; index++)
            {
                GameObject bolt = new GameObject("Bolt" + index);
                bolt.transform.SetParent(transform, false);

                LineRenderer line = bolt.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.alignment = LineAlignment.View;
                line.sharedMaterial = _material;
                line.positionCount = _pointsPerBolt;
                line.widthCurve = widthCurve;
                line.widthMultiplier = _width;
                line.numCapVertices = 0;
                line.numCornerVertices = 0;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.lightProbeUsage = LightProbeUsage.Off;
                line.reflectionProbeUsage = ReflectionProbeUsage.Off;
                line.enabled = false;

                _bolts[index] = line;
            }
        }

        private void SetBoltsEnabled(bool enabled)
        {
            for (int index = 0; index < _bolts.Length; index++)
                _bolts[index].enabled = enabled;
        }

        private float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }
    }
}
