using UnityEngine;

namespace Game.View
{
    public sealed class BookFloat : MonoBehaviour
    {
        [SerializeField] private Transform _book;
        [SerializeField] private Transform _head;

        [Header("Float")]
        [SerializeField, Min(0f)] private float _bobHeight = 0.05f;
        [SerializeField, Min(0.1f)] private float _bobSeconds = 2.6f;
        [SerializeField, Range(0f, 20f)] private float _swayDegrees = 4f;
        [SerializeField, Min(0.1f)] private float _swaySeconds = 3.7f;
        [SerializeField, Range(0f, 10f)] private float _wobbleDegrees = 2.5f;

        [Header("Follow")]
        [SerializeField, Min(0.1f)] private float _followSharpness = 7f;
        [SerializeField, Min(0.1f)] private float _turnSharpness = 9f;

        [Header("Reading")]
        [SerializeField, Range(0f, 40f)] private float _headPitchDegrees = 12f;

        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;
        private bool _hasBase;
        private bool _snap = true;
        private float _time;

        private void Awake()
        {
            CaptureBase();
        }

        private void OnEnable()
        {
            _time = 0f;
            _snap = true;
        }

        private void LateUpdate()
        {
            if (_book != null)
                Float(Time.deltaTime);

            if (_head != null && _headPitchDegrees > 0f)
                _head.rotation = Quaternion.AngleAxis(_headPitchDegrees, transform.right) * _head.rotation;
        }

        private void Float(float deltaTime)
        {
            CaptureBase();

            _time += deltaTime;

            float bob = Mathf.Sin(_time * Mathf.PI * 2f / _bobSeconds);
            float sway = Mathf.Sin(_time * Mathf.PI * 2f / _swaySeconds);
            float wobble = Mathf.Sin(_time * Mathf.PI * 2f / (_bobSeconds * 0.73f) + 1.3f);

            Vector3 targetPosition = transform.TransformPoint(_baseLocalPosition) + Vector3.up * (bob * _bobHeight);
            Quaternion targetRotation = transform.rotation * _baseLocalRotation * Quaternion.Euler(wobble * _wobbleDegrees, sway * _swayDegrees, 0f);

            if (_snap)
            {
                _book.SetPositionAndRotation(targetPosition, targetRotation);
                _snap = false;

                return;
            }

            float follow = 1f - Mathf.Exp(-_followSharpness * deltaTime);
            float turn = 1f - Mathf.Exp(-_turnSharpness * deltaTime);

            _book.SetPositionAndRotation(Vector3.Lerp(_book.position, targetPosition, follow), Quaternion.Slerp(_book.rotation, targetRotation, turn));
        }

        private void CaptureBase()
        {
            if (_hasBase || _book == null)
                return;

            _baseLocalPosition = transform.InverseTransformPoint(_book.position);
            _baseLocalRotation = Quaternion.Inverse(transform.rotation) * _book.rotation;
            _hasBase = true;
        }
    }
}
