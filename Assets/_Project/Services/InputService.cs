using Game.Core;
using Game.Services.Input;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Services
{
    public sealed class InputService : IInputService, IDisposable
    {
        private readonly GameControls _input;

        private bool _dashPressed;

        public InputService()
        {
            _input = new GameControls();
            _input.Gameplay.Dash.performed += OnDashPerformed;
            _input.Enable();
        }

        public Vector2 MoveAxis => _input.Gameplay.Move.ReadValue<Vector2>();

        public bool ConsumeDashPressed()
        {
            if (_dashPressed == false)
                return false;

            _dashPressed = false;

            return true;
        }

        public void ResetLatches()
        {
            _dashPressed = false;
        }

        public void Dispose()
        {
            _input.Gameplay.Dash.performed -= OnDashPerformed;
            _input.Dispose();
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            _dashPressed = true;
        }
    }
}
