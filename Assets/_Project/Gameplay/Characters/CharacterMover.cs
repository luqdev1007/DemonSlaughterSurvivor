using UnityEngine;

namespace DemonSlaughter.Gameplay.Characters
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMover : MonoBehaviour
    {
        private CharacterController _controller;
        private float _speed;

        public void Initialize(float speed)
        {
            _controller = GetComponent<CharacterController>();
            _speed = speed;
        }

        public void Move(Vector2 input)
        {
            if (input == Vector2.zero) return;

            var direction = new Vector3(input.x, 0f, input.y);
            _controller.SimpleMove(direction * _speed);

            transform.rotation = Quaternion.LookRotation(direction);

            Debug.Log(_controller.velocity);
        }
    }
}