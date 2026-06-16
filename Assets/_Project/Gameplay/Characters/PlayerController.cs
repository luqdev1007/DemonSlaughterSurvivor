using R3;
using System;
using VContainer.Unity;

namespace DemonSlaughter.Gameplay.Characters
{
    public sealed class PlayerController : IStartable, IDisposable
    {
        private readonly PlayerInputHandler _inputHandler;
        private readonly CharacterMover _mover;
        private readonly CompositeDisposable _disposable = new();

        public PlayerController(PlayerInputHandler inputHandler, CharacterMover mover)
        {
            _inputHandler = inputHandler;
            _mover = mover;
        }

        public void Start()
        {
            _inputHandler.MoveInput
                .Subscribe(input => _mover.Move(input))
                .AddTo(_disposable);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}