using Game.Core;
using Game.Services.Input;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Services
{
    public sealed class InputService : IInputService, IPauseInput, IPointerInput, IDashInput, IDisposable
    {
        private readonly GameControls _input;

        private bool _dashPressed;
        private bool _chargeReleased;
        private bool _ultimatePressed;
        private bool _pausePressed;

        public InputService()
        {
            _input = new GameControls();
            _input.Gameplay.Dash.performed += OnDashPerformed;
            _input.Gameplay.ChargeDash.canceled += OnChargeCanceled;
            _input.Gameplay.Ultimate.performed += OnUltimatePerformed;
            _input.Gameplay.Pause.performed += OnPausePerformed;
            _input.Enable();
        }

        public Vector2 MoveAxis => _input.Gameplay.Move.ReadValue<Vector2>();

        public Vector2 PointerPosition => _input.Gameplay.Point.ReadValue<Vector2>();

        public bool IsStandHeld => _input.Gameplay.StandStill.IsPressed();

        public bool IsChargeHeld => _input.Gameplay.ChargeDash.IsPressed();

        public bool ConsumeChargeReleased()
        {
            if (_chargeReleased == false)
                return false;

            _chargeReleased = false;

            return true;
        }

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
            _chargeReleased = false;
            _ultimatePressed = false;
            _pausePressed = false;
        }

        public void Dispose()
        {
            _input.Gameplay.Dash.performed -= OnDashPerformed;
            _input.Gameplay.ChargeDash.canceled -= OnChargeCanceled;
            _input.Gameplay.Ultimate.performed -= OnUltimatePerformed;
            _input.Gameplay.Pause.performed -= OnPausePerformed;
            _input.Disable();
            _input.Dispose();
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            _dashPressed = true;
        }

        private void OnChargeCanceled(InputAction.CallbackContext context)
        {
            _chargeReleased = true;
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
