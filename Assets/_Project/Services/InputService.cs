using Game.Core;
using System;
using UnityEngine;
using Game.Services.Input;

namespace Game.Services
{
    public sealed class InputService : IInputService, IDisposable
    {
        private GameControls _input;
       

        public InputService()
        {
            _input = new GameControls();
            _input.Enable();
        }

        public Vector2 MoveAxis => _input.Gameplay.Move.ReadValue<Vector2>();

        public void Dispose()
        {
            _input?.Dispose();
        }
    }
}
