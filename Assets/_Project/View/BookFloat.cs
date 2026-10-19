using UnityEngine;

namespace Game.View
{
    public sealed class BookFloat : MonoBehaviour
    {
        [SerializeField] private Transform _book;
        [SerializeField] private Transform _head;

        [Header("Float")]
        [SerializeField, Min(0f)] private float _bobHeight = 0.035f;
        [SerializeField, Min(0.1f)] private float _bobSeconds = 2.1f;
        [SerializeField, Range(0f, 20f)] private float _swayDegrees = 5f;
        [SerializeField, Min(0.1f)] private float _swaySeconds = 3.4f;
        [SerializeField, Range(0f, 10f)] private float _wobbleDegrees = 3f;

        [Header("Reading")]
        [SerializeField, Range(0f, 40f)] private float _headPitchDegrees = 14f;

        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private bool _hasBase;
        private float _time;

        private void Awake()
        {
            CaptureBase();
        }

        private void OnEnable()
        {
            _time = 0f;
        }

        private void Update()
        {
            if (_book == null)
                return;

            CaptureBase();

            _time += Time.deltaTime;

            float bob = Mathf.Sin(_time * Mathf.PI * 2f / _bobSeconds);
            float sway = Mathf.Sin(_time * Mathf.PI * 2f / _swaySeconds);
            float wobble = Mathf.Sin(_time * Mathf.PI * 2f / (_bobSeconds * 0.73f) + 1.3f);

            _book.localPosition = _basePosition + Vector3.up * (bob * _bobHeight);
            _book.localRotation = _baseRotation * Quaternion.Euler(wobble * _wobbleDegrees, sway * _swayDegrees, 0f);
        }

        private void LateUpdate()
        {
            if (_head == null || _headPitchDegrees <= 0f)
                return;

            _head.rotation = Quaternion.AngleAxis(_headPitchDegrees, transform.right) * _head.rotation;
        }

        private void CaptureBase()
        {
            if (_hasBase || _book == null)
                return;

            _basePosition = _book.localPosition;
            _baseRotation = _book.localRotation;
            _hasBase = true;
        }
    }
}
