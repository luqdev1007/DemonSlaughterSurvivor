using Game.Configs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.View
{
    public sealed class SwordTrail : MonoBehaviour
    {
        [SerializeField] private SwordTrailConfig _config;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _base;
        [SerializeField] private Transform _tip;

        private GameObject _trailObject;
        private Mesh _mesh;

        private Vector3[] _bases;
        private Vector3[] _tips;
        private float[] _times;
        private bool[] _connected;
        private float[] _bloodTints;
        private int _head;
        private int _count;

        private Vector3[] _vertices;
        private Color[] _colors;
        private Vector2[] _uvs;
        private int[] _triangles;

        private int[] _attackStateHashes;
        private bool _wasEmitting;
        private bool _isBerserk;
        private float _reach = 1f;
        private bool _isSharp;
        private bool _hasPreviousTip;
        private Vector3 _previousTip;
        private bool _hasBlood;
        private float _bloodTime;

        public void SetBerserk(bool value)
        {
            _isBerserk = value;
        }

        public void SetStyle(float reach, bool sharp)
        {
            _reach = reach > 0f ? reach : 1f;
            _isSharp = sharp;
        }

        public void PlayBlood()
        {
            _hasBlood = true;
            _bloodTime = Time.time;
        }

        public void ResetTrail()
        {
            _hasBlood = false;
            _count = 0;
            _head = 0;
            _wasEmitting = false;
            _hasPreviousTip = false;
            _isSharp = false;

            if (_mesh != null)
                _mesh.Clear();
        }

        private void Awake()
        {
            if (_config == null || _base == null || _tip == null)
            {
                enabled = false;

                return;
            }

            int capacity = Mathf.Max(2, _config.MaxSamples);

            _bases = new Vector3[capacity];
            _tips = new Vector3[capacity];
            _times = new float[capacity];
            _connected = new bool[capacity];
            _bloodTints = new float[capacity];

            _vertices = new Vector3[capacity * 2];
            _colors = new Color[capacity * 2];
            _uvs = new Vector2[capacity * 2];
            _triangles = new int[(capacity - 1) * 6];

            string[] states = _config.AttackStates;
            _attackStateHashes = new int[states.Length];

            for (int index = 0; index < states.Length; index++)
                _attackStateHashes[index] = Animator.StringToHash(states[index]);

            _mesh = new Mesh { name = "Sword Trail" };
            _mesh.MarkDynamic();

            _trailObject = new GameObject(name + " Sword Trail");
            _trailObject.AddComponent<MeshFilter>().sharedMesh = _mesh;

            MeshRenderer renderer = _trailObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _config.Material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void OnEnable()
        {
            ResetTrail();

            if (_trailObject != null)
                _trailObject.SetActive(true);
        }

        private void OnDisable()
        {
            ResetTrail();

            if (_trailObject != null)
                _trailObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_trailObject != null)
                Destroy(_trailObject);

            if (_mesh != null)
                Destroy(_mesh);
        }

        private void LateUpdate()
        {
            float now = Time.time;
            Vector3 tip = ExtendFromBody(_tip.position);
            float deltaTime = Time.deltaTime;

            float tipSpeed = _hasPreviousTip && deltaTime > 0f ? (tip - _previousTip).magnitude / deltaTime : 0f;

            _previousTip = tip;
            _hasPreviousTip = true;

            bool emitting = IsAttacking() && tipSpeed >= _config.MinTipSpeed;

            if (emitting)
                AddSample(ExtendFromBody(_base.position), tip, now, _wasEmitting, ResolveBloodTint(now));

            _wasEmitting = emitting;

            DropExpired(now);
            Rebuild(now);
        }

        private Vector3 ExtendFromBody(Vector3 point)
        {
            float scale = _reach;

            if (_animator == null || Mathf.Approximately(scale, 1f))
                return point;

            Vector3 origin = _animator.transform.position;

            return new Vector3(origin.x + (point.x - origin.x) * scale, point.y, origin.z + (point.z - origin.z) * scale);
        }

        private bool IsAttacking()
        {
            if (_animator == null || _attackStateHashes.Length == 0)
                return false;

            int layers = _animator.layerCount;

            for (int layer = 0; layer < layers; layer++)
            {
                if (IsAttackState(_animator.GetCurrentAnimatorStateInfo(layer).shortNameHash))
                    return true;

                if (_animator.IsInTransition(layer) && IsAttackState(_animator.GetNextAnimatorStateInfo(layer).shortNameHash))
                    return true;
            }

            return false;
        }

        private bool IsAttackState(int hash)
        {
            for (int index = 0; index < _attackStateHashes.Length; index++)
            {
                if (_attackStateHashes[index] == hash)
                    return true;
            }

            return false;
        }

        private float ResolveBloodTint(float now)
        {
            if (_hasBlood == false || _config.BloodSeconds <= 0f)
                return 0f;

            return 1f - Mathf.Clamp01((now - _bloodTime) / _config.BloodSeconds);
        }

        private void AddSample(Vector3 basePosition, Vector3 tipPosition, float time, bool connected, float bloodTint)
        {
            int capacity = _bases.Length;
            int slot = (_head + _count) % capacity;

            if (_count == capacity)
            {
                _head = (_head + 1) % capacity;
                _count--;
            }

            _bases[slot] = basePosition;
            _tips[slot] = tipPosition;
            _times[slot] = time;
            _bloodTints[slot] = bloodTint;
            _connected[slot] = connected && _count > 0;
            _count++;
        }

        private void DropExpired(float now)
        {
            float fade = _config.FadeSeconds;

            while (_count > 0 && now - _times[_head] >= fade)
            {
                _head = (_head + 1) % _bases.Length;
                _count--;
            }
        }

        private void Rebuild(float now)
        {
            _mesh.Clear();

            if (_count < 2)
                return;

            Gradient gradient = _isSharp ? _config.SharpColor : _isBerserk ? _config.BerserkColor : _config.Color;
            Color baseColor = gradient.Evaluate(0f);
            Color tipColor = gradient.Evaluate(1f);
            Color bloodBase = _config.BloodColor.Evaluate(0f);
            Color bloodTip = _config.BloodColor.Evaluate(1f);
            float fade = Mathf.Max(_config.FadeSeconds, 1e-4f);
            int capacity = _bases.Length;
            int triangleCount = 0;

            for (int order = 0; order < _count; order++)
            {
                int slot = (_head + order) % capacity;
                float life = 1f - Mathf.Clamp01((now - _times[slot]) / fade);
                int vertex = order * 2;

                _vertices[vertex] = _bases[slot];
                _vertices[vertex + 1] = _tips[slot];

                float blood = _bloodTints[slot];
                Color fadedBase = Color.Lerp(baseColor, bloodBase, blood);
                fadedBase.a *= life;
                Color fadedTip = Color.Lerp(tipColor, bloodTip, blood);
                fadedTip.a *= life;

                _colors[vertex] = fadedBase;
                _colors[vertex + 1] = fadedTip;

                _uvs[vertex] = new Vector2(life, 0f);
                _uvs[vertex + 1] = new Vector2(life, 1f);

                if (order == 0 || _connected[slot] == false)
                    continue;

                int previous = vertex - 2;

                _triangles[triangleCount] = previous;
                _triangles[triangleCount + 1] = previous + 1;
                _triangles[triangleCount + 2] = vertex;
                _triangles[triangleCount + 3] = vertex;
                _triangles[triangleCount + 4] = previous + 1;
                _triangles[triangleCount + 5] = vertex + 1;
                triangleCount += 6;
            }

            if (triangleCount == 0)
                return;

            int vertexCount = _count * 2;

            _mesh.SetVertices(_vertices, 0, vertexCount);
            _mesh.SetColors(_colors, 0, vertexCount);
            _mesh.SetUVs(0, _uvs, 0, vertexCount);
            _mesh.SetTriangles(_triangles, 0, triangleCount, 0, true);
        }
    }
}
