using Game.Core;
using Game.Services.Input;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Services
{
    public sealed class InputService : IInputService, IPauseInput, IDisposable
    {
        private readonly GameControls _input;

        private bool _dashPressed;
        private bool _ultimatePressed;
        private bool _pausePressed;

        public InputService()
        {
            _input = new GameControls();
            _input.Gameplay.Dash.performed += OnDashPerformed;
            _input.Gameplay.Ultimate.performed += OnUltimatePerformed;
            _input.Gameplay.Pause.performed += OnPausePerformed;
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

        public bool ConsumeUltimatePressed()
        {
            if (_ultimatePressed == false)
                return false;

            _ultimatePressed = false;

            return true;
        }

        public bool ConsumePausePressed()
        {
            if (_pausePressed == false)
                return false;

            _pausePressed = false;

            return true;
        }

        public void ResetLatches()
        {
            _dashPressed = false;
            _ultimatePressed = false;
            _pausePressed = false;
        }

        public void Dispose()
        {
            _input.Gameplay.Dash.performed -= OnDashPerformed;
            _input.Gameplay.Ultimate.performed -= OnUltimatePerformed;
            _input.Gameplay.Pause.performed -= OnPausePerformed;
            _input.Dispose();
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            _dashPressed = true;
        }

        private void OnUltimatePerformed(InputAction.CallbackContext context)
        {
            _ultimatePressed = true;
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            _pausePressed = true;
        }
    }
}
