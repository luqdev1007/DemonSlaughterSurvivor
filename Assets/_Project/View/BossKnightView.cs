using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.View
{
    public sealed class BossKnightView : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int ShapeId = Shader.PropertyToID("_Shape");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
        private static readonly int PulseRingId = Shader.PropertyToID("_PulseRing");

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _halberd;
        [SerializeField] private Material _markerMaterial;

        [Header("Markers")]
        [SerializeField, Min(0f)] private float _markerHeight = 0.04f;
        [SerializeField] private Color _chargeAimColor = new Color(1f, 0.25f, 0.1f, 0.35f);
        [SerializeField] private Color _chargeLockedColor = new Color(1f, 0.1f, 0.05f, 0.75f);
        [SerializeField] private Color _spinWarningColor = new Color(1f, 0.45f, 0.1f, 0.25f);
        [SerializeField] private Color _spinActiveColor = new Color(1f, 0.15f, 0.05f, 0.6f);

        [Header("Spin")]
        [SerializeField, Min(0.01f)] private float _halberdReturnSeconds = 0.2f;

        private ActionCue _cue;
        private MaterialPropertyBlock _block;
        private Mesh _stripMesh;
        private Mesh _discMesh;
        private MeshRenderer _chargeMarker;
        private MeshRenderer _spinMarker;

        private Transform _leftHand;
        private Transform _rightHand;
        private Transform _leftArm;
        private Transform _rightArm;
        private Quaternion _leftArmBase;
        private Quaternion _rightArmBase;
        private Quaternion _leftArmWritten;
        private Quaternion _rightArmWritten;

        private Vector3 _halberdRestPosition;
        private Quaternion _halberdRestRotation;
        private Vector3 _halberdOffset;
        private Quaternion _halberdRotation;
        private bool _isSpinning;
        private float _spinAngle;

        private bool _isReturning;
        private float _returnElapsed;
        private Vector3 _returnFromPosition;
        private Quaternion _returnFromRotation;

        public float SpinAngle => _spinAngle;

        public bool IsSpinning => _isSpinning;

        public Transform Halberd => _halberd;

        public void Apply(in ActionCue cue)
        {
            EnsureBuilt();

            _cue = cue;

            ApplyMarkers();
        }

        public void ResetImmediate()
        {
            _cue = default;

            if (_isSpinning || _isReturning)
                RestoreHalberd();

            _isSpinning = false;
            _isReturning = false;
            _spinAngle = 0f;

            if (_chargeMarker != null)
                _chargeMarker.enabled = false;

            if (_spinMarker != null)
                _spinMarker.enabled = false;
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnDestroy()
        {
            if (_stripMesh != null)
                Destroy(_stripMesh);

            if (_discMesh != null)
                Destroy(_discMesh);
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;

            if (_cue.Pose == ActionPose.Spinning)
            {
                if (_isSpinning == false)
                    BeginSpin();

                AdvanceSpin(deltaTime);
            }
            else if (_isSpinning)
            {
                EndSpin();
            }

            if (_isReturning)
                AdvanceReturn(deltaTime);
        }

        private void EnsureBuilt()
        {
            if (_block != null)
                return;

            _block = new MaterialPropertyBlock();

            if (_animator != null)
            {
                _leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
                _rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
                _leftArm = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                _rightArm = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            }

            if (_halberd != null)
            {
                _halberdRestPosition = _halberd.localPosition;
                _halberdRestRotation = _halberd.localRotation;
            }

            _stripMesh = BuildQuad("ChargeMarkerQuad", 0f, 1f);
            _discMesh = BuildQuad("SpinMarkerQuad", -0.5f, 0.5f);

            _chargeMarker = BuildMarker("ChargeMarker", _stripMesh);
            _spinMarker = BuildMarker("SpinMarker", _discMesh);
        }

        private MeshRenderer BuildMarker(string markerName, Mesh mesh)
        {
            GameObject marker = new GameObject(markerName);
            marker.transform.SetParent(transform, false);
            marker.AddComponent<NotBody>();
            marker.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer renderer = marker.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _markerMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;

            return renderer;
        }

        private static Mesh BuildQuad(string meshName, float nearZ, float farZ)
        {
            Mesh mesh = new Mesh { name = meshName };

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, nearZ),
                new Vector3(0.5f, 0f, nearZ),
                new Vector3(-0.5f, 0f, farZ),
                new Vector3(0.5f, 0f, farZ)
            };

            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };

            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();

            return mesh;
        }

        private void ApplyMarkers()
        {
            bool isCharge = _cue.Marker == GroundMarker.ChargeAim || _cue.Marker == GroundMarker.ChargeLocked;
            bool isSpin = _cue.Marker == GroundMarker.SpinWarning || _cue.Marker == GroundMarker.SpinActive;

            _chargeMarker.enabled = isCharge && _markerMaterial != null;
            _spinMarker.enabled = isSpin && _markerMaterial != null;

            if (isCharge)
            {
                Transform marker = _chargeMarker.transform;
                marker.localPosition = new Vector3(0f, _markerHeight, 0f);
                marker.localRotation = Quaternion.identity;
                marker.localScale = new Vector3(_cue.MarkerWidth, 1f, _cue.MarkerLength);

                SetBlock(_chargeMarker, _cue.Marker == GroundMarker.ChargeLocked ? _chargeLockedColor : _chargeAimColor, 0f, 0f, 0f);
            }

            if (isSpin)
            {
                Transform marker = _spinMarker.transform;
                float diameter = _cue.MarkerRadius * 2f;
                marker.localPosition = new Vector3(0f, _markerHeight, 0f);
                marker.localRotation = Quaternion.identity;
                marker.localScale = new Vector3(diameter, 1f, diameter);

                bool active = _cue.Marker == GroundMarker.SpinActive;
                float sincePulse = Mathf.Clamp01(_cue.Pulse);
                float pulse = active ? (1f - sincePulse) * (1f - sincePulse) : 0f;

                SetBlock(_spinMarker, active ? _spinActiveColor : _spinWarningColor, 1f, pulse, sincePulse);
            }
        }

        private void SetBlock(Renderer target, Color color, float shape, float pulse, float pulseRing)
        {
            _block.Clear();
            _block.SetColor(ColorId, color);
            _block.SetFloat(ShapeId, shape);
            _block.SetFloat(PulseId, pulse);
            _block.SetFloat(PulseRingId, pulseRing);

            target.SetPropertyBlock(_block);
        }

        private void BeginSpin()
        {
            if (_halberd == null || _leftHand == null || _rightHand == null)
                return;

            if (_isReturning)
                RestoreHalberd();

            _isSpinning = true;
            _isReturning = false;
            _spinAngle = 0f;

            Quaternion inverseYaw = Quaternion.Inverse(transform.rotation);
            Vector3 pivot = (_leftHand.position + _rightHand.position) * 0.5f;

            _halberdOffset = inverseYaw * (_halberd.position - pivot);
            _halberdRotation = inverseYaw * _halberd.rotation;

            if (_leftArm != null)
            {
                _leftArmBase = _leftArm.localRotation;
                _leftArmWritten = _leftArmBase;
            }

            if (_rightArm != null)
            {
                _rightArmBase = _rightArm.localRotation;
                _rightArmWritten = _rightArmBase;
            }
        }

        private void AdvanceSpin(float deltaTime)
        {
            if (_isSpinning == false)
                return;

            _spinAngle = Mathf.Repeat(_spinAngle + _cue.SpinDegreesPerSecond * deltaTime, 360f);

            float radians = _spinAngle * Mathf.Deg2Rad;
            Quaternion wobble = Quaternion.Euler(Mathf.Sin(radians) * _cue.ArmWobbleDegrees, 0f, Mathf.Cos(radians) * _cue.ArmWobbleDegrees);

            Wobble(_leftArm, wobble, ref _leftArmBase, ref _leftArmWritten);
            Wobble(_rightArm, wobble, ref _rightArmBase, ref _rightArmWritten);

            Vector3 pivot = (_leftHand.position + _rightHand.position) * 0.5f;
            Quaternion spin = transform.rotation * Quaternion.AngleAxis(_spinAngle, Vector3.up);

            _halberd.SetPositionAndRotation(pivot + spin * _halberdOffset, spin * _halberdRotation);
        }

        private static void Wobble(Transform arm, Quaternion wobble, ref Quaternion baseRotation, ref Quaternion written)
        {
            if (arm == null)
                return;

            Quaternion current = arm.localRotation;

            if (current != written)
                baseRotation = current;

            written = baseRotation * wobble;
            arm.localRotation = written;
        }

        private void EndSpin()
        {
            _isSpinning = false;

            if (_halberd == null)
                return;

            _isReturning = true;
            _returnElapsed = 0f;
            _returnFromPosition = _halberd.position;
            _returnFromRotation = _halberd.rotation;
        }

        private void AdvanceReturn(float deltaTime)
        {
            _returnElapsed += deltaTime;

            float progress = Mathf.Clamp01(_returnElapsed / _halberdReturnSeconds);

            Transform parent = _halberd.parent;
            Vector3 restPosition = parent.TransformPoint(_halberdRestPosition);
            Quaternion restRotation = parent.rotation * _halberdRestRotation;

            if (progress >= 1f)
            {
                RestoreHalberd();

                _isReturning = false;

                return;
            }

            _halberd.SetPositionAndRotation(
                Vector3.Lerp(_returnFromPosition, restPosition, progress),
                Quaternion.Slerp(_returnFromRotation, restRotation, progress));
        }

        private void RestoreHalberd()
        {
            if (_halberd == null)
                return;

            _halberd.localPosition = _halberdRestPosition;
            _halberd.localRotation = _halberdRestRotation;
        }
    }
}
