using UnityEngine;

namespace Game.View
{
    public sealed class HoverRig : MonoBehaviour
    {
        [SerializeField] private Transform _body;
        [SerializeField] private float _pivotHeight = 1.1f;

        [Header("Lean")]
        [SerializeField, Min(0.1f)] private float _fullSpeed = 5f;
        [SerializeField, Range(0f, 30f)] private float _forwardLeanDegrees = 10f;
        [SerializeField, Range(0f, 30f)] private float _sideLeanDegrees = 7f;
        [SerializeField, Min(0f)] private float _leanSharpness = 6f;

        private Vector2 _target;
        private Vector2 _lean;

        public void SetVelocity(Vector2 localVelocity)
        {
            Vector2 scaled = localVelocity / _fullSpeed;
            _target = Vector2.ClampMagnitude(scaled, 1f);
        }

        public void ResetHover()
        {
            _target = Vector2.zero;
            _lean = Vector2.zero;
            Apply();
        }

        private void Update()
        {
            if (_body == null)
                return;

            float blend = 1f - Mathf.Exp(-_leanSharpness * Time.deltaTime);
            _lean = Vector2.Lerp(_lean, _target, blend);

            Apply();
        }

        private void Apply()
        {
            if (_body == null)
                return;

            Quaternion rotation = Quaternion.Euler(_lean.y * _forwardLeanDegrees, 0f, -_lean.x * _sideLeanDegrees);
            Vector3 pivot = Vector3.up * _pivotHeight;

            _body.localRotation = rotation;
            _body.localPosition = pivot - rotation * pivot;
        }
    }
}
