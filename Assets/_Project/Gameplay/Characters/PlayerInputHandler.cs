using R3;
using UnityEngine;

namespace DemonSlaughter.Gameplay.Characters
{
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        private PlayerInputSystem _inputActions;

        public ReadOnlyReactiveProperty<Vector2> MoveInput => _moveInput;
        private ReactiveProperty<Vector2> _moveInput = new(Vector2.zero);

        public void Initialize(PlayerInputSystem inputActions)
        {
            _inputActions = inputActions;
            _inputActions.Player.Enable();
        }

        private void Update()
        {
            _moveInput.Value = _inputActions.Player.Move.ReadValue<Vector2>();
        }

        private void OnDestroy()
        {
            _moveInput.Dispose();
            _inputActions?.Player.Disable();
        }
    }
}