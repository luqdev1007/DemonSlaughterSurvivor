using Game.Core;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Services
{
    public sealed class DebugGoreInput : IDebugGoreInput, IDisposable
    {
        private readonly InputAction _action;

        public DebugGoreInput()
        {
            _action = new InputAction("DebugForceDismember", InputActionType.Button, "<Keyboard>/g");
            _action.performed += OnPerformed;
            _action.Enable();
        }

        public bool ForceDismember { get; private set; }

        public void Dispose()
        {
            _action.performed -= OnPerformed;
            _action.Disable();
            _action.Dispose();
        }

        private void OnPerformed(InputAction.CallbackContext context)
        {
            ForceDismember = ForceDismember == false;

            Debug.Log($"Debug: dismember chance forced to 100% is {(ForceDismember ? "ON" : "OFF")} (G).");
        }
    }
}
