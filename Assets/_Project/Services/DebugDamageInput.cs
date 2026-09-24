using Game.Core;
using System;
using UnityEngine.InputSystem;

namespace Game.Services
{
    public sealed class DebugDamageInput : IDebugDamageInput, IDisposable
    {
        private readonly InputAction _action;

        private bool _pressed;

        public DebugDamageInput()
        {
            _action = new InputAction("DebugDamage", InputActionType.Button, "<Keyboard>/k");
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
