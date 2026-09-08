using Game.Core;
using System;
using UnityEngine;

namespace Game.View
{
    public sealed class ViewInterpolator : MonoBehaviour, IView
    {
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
            _secondsSinceUpdate = 0f;
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
            throw new NotImplementedException();
        }
    }
}
