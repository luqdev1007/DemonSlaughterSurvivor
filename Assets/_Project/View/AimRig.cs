using UnityEngine;

namespace Game.View
{
    public sealed class AimRig : MonoBehaviour
    {
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveZId = Animator.StringToHash("MoveZ");
        private static readonly int LocomotionSpeedId = Animator.StringToHash("LocomotionSpeed");
        private static readonly int TurnId = Animator.StringToHash("Turn");

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform[] _spine;
        [SerializeField] private float[] _spineWeights;

        [Header("Locomotion")]
        [SerializeField, Min(0.1f)] private float _runSpeed = 3.4f;
        [SerializeField, Min(0f)] private float _moveDamping = 0.08f;
        [SerializeField, Min(0.1f)] private float _maxPlaybackSpeed = 2f;

        [Header("Twist")]
        [SerializeField, Min(0f)] private float _twistSharpness = 18f;

        private float _targetTwist;
        private float _twist;
        private Vector2 _localVelocity;
        private int _turnSide;
        private bool _hasState;

        public void SetAim(float twistDegrees, Vector2 localVelocity, int turnSide)
        {
            _targetTwist = twistDegrees;
            _localVelocity = localVelocity;
            _turnSide = turnSide;

            if (_hasState)
                return;

            _twist = twistDegrees;
            _hasState = true;
        }

        public void ResetAim()
        {
            _targetTwist = 0f;
            _twist = 0f;
            _localVelocity = Vector2.zero;
            _turnSide = 0;
            _hasState = false;
        }

        private void Update()
        {
            if (_animator == null || _animator.isActiveAndEnabled == false)
                return;

            Vector2 parameter = _localVelocity;
            float speed = parameter.magnitude;
            float playback = 1f;

            if (speed > _runSpeed)
            {
                playback = Mathf.Min(_maxPlaybackSpeed, speed / _runSpeed);
                parameter *= _runSpeed / speed;
            }

            float deltaTime = Time.deltaTime;

            _animator.SetFloat(MoveXId, parameter.x, _moveDamping, deltaTime);
            _animator.SetFloat(MoveZId, parameter.y, _moveDamping, deltaTime);
            _animator.SetFloat(LocomotionSpeedId, playback);
            _animator.SetInteger(TurnId, _turnSide);
        }

        private void LateUpdate()
        {
            if (_spine == null || _spine.Length == 0)
                return;

            float blend = 1f - Mathf.Exp(-_twistSharpness * Time.deltaTime);
            _twist = Mathf.LerpAngle(_twist, _targetTwist, blend);

            if (Mathf.Abs(_twist) < 0.01f)
                return;

            for (int index = 0; index < _spine.Length; index++)
            {
                Transform bone = _spine[index];

                if (bone == null)
                    continue;

                float weight = _spineWeights != null && index < _spineWeights.Length ? _spineWeights[index] : 1f / _spine.Length;
                bone.rotation = Quaternion.AngleAxis(_twist * weight, Vector3.up) * bone.rotation;
            }
        }
    }
}
