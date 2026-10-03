using Game.Core;
using System;
using UnityEngine.InputSystem;

namespace Game.Services
{
    public sealed class UpgradeChoiceInput : IUpgradeChoiceInput, IUpgradeChoiceSubmit, IDisposable
    {
        private const int NoChoice = -1;

        private readonly InputAction[] _actions;

        private int _pending = NoChoice;

        public UpgradeChoiceInput()
        {
            _actions = new[]
            {
                CreateAction("UpgradeChoice1", "<Keyboard>/1", "<Keyboard>/numpad1"),
                CreateAction("UpgradeChoice2", "<Keyboard>/2", "<Keyboard>/numpad2"),
                CreateAction("UpgradeChoice3", "<Keyboard>/3", "<Keyboard>/numpad3"),
            };

            for (int index = 0; index < _actions.Length; index++)
            {
                _actions[index].performed += OnPerformed;
                _actions[index].Enable();
            }
        }

        public bool TryConsume(out int index)
        {
            index = _pending;

            if (_pending == NoChoice)
                return false;

            _pending = NoChoice;

            return true;
        }

        public void Clear()
        {
            _pending = NoChoice;
        }

        public void Submit(int index)
        {
            _pending = index;
        }

        public void Dispose()
        {
            for (int index = 0; index < _actions.Length; index++)
            {
                _actions[index].performed -= OnPerformed;
                _actions[index].Disable();
                _actions[index].Dispose();
            }
        }

        private void OnPerformed(InputAction.CallbackContext context)
        {
            _pending = Array.IndexOf(_actions, context.action);
        }

        private static InputAction CreateAction(string name, string binding, string alternative)
        {
            InputAction action = new InputAction(name, InputActionType.Button, binding);
            action.AddBinding(alternative);

            return action;
        }
    }
}
