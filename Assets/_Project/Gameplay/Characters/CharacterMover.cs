using UnityEngine;

namespace DemonSlaughter.Gameplay.Characters
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMover : MonoBehaviour
    {
        private CharacterController _controller;
        private float _speed;
        private Vector2 _currentInput;

        public void Initialize(float speed)
        {
            _controller = GetComponent<CharacterController>();
            _speed = speed;
        }

        public void SetInput(Vector2 input)
        {
            _currentInput = input;
        }

        private void Update()
        {
            if (_currentInput == Vector2.zero) 
                return;

            var direction = new Vector3(_currentInput.x, 0f, _currentInput.y);
            _controller.SimpleMove(direction * _speed);
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}