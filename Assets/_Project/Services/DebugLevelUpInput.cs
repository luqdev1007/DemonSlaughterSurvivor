using Game.Core;
using System;
using UnityEngine.InputSystem;

namespace Game.Services
{
    public sealed class DebugLevelUpInput : IDebugLevelUpInput, IDisposable
    {
        private readonly InputAction _action;

        private bool _pressed;

        public DebugLevelUpInput()
        {
            _action = new InputAction("DebugLevelUp", InputActionType.Button, "<Keyboard>/l");
            _action.performed += OnPerformed;
            _action.Enable();
        }

        public bool ConsumePressed()
        {
            if (_pressed == false)
                return false;

            _pressed = false;

            return true;
        }

        public void Dispose()
        {
            _action.performed -= OnPerformed;
            _action.Disable();
            _action.Dispose();
        }

        private void OnPerformed(InputAction.CallbackContext context)
        {
            _pressed = true;
        }
    }
}
