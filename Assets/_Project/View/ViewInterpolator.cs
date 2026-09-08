using Game.Core;
using UnityEngine;

namespace Game.View
{
    public sealed class ViewInterpolator : MonoBehaviour, IView
    {
        private const float TickSeconds = 1f / 60f;

        private Vector3 _previousPosition;
        private Vector3 _currentPosition;
        private float _secondsSinceUpdate;
        private bool _hasPosition;

        public Transform Transform => transform;

        public void SetPosition(Vector3 position)
        {
            if (_hasPosition == false)
            {
                _previousPosition = position;
                _currentPosition = position;
                _secondsSinceUpdate = 0f;
                _hasPosition = true;

                transform.position = position;

                return;
            }

            _previousPosition = _currentPosition;
            _currentPosition = position;
            _secondsSinceUpdate -= TickSeconds;
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        private void Update()
        {
            if (_hasPosition == false)
                return;

            _secondsSinceUpdate += Time.deltaTime;

            transform.position = ResolvePosition();
        }

        private Vector3 ResolvePosition()
        {
            float phase = Mathf.Clamp01(_secondsSinceUpdate / TickSeconds);

            return Vector3.Lerp(_previousPosition, _currentPosition, phase);
        }
    }
}