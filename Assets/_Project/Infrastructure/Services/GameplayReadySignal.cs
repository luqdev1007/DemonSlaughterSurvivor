using System;
using DemonSlaughter.Core.Services;

namespace DemonSlaughter.Infrastructure.Services
{
    public sealed class GameplayReadySignal : IGameplayReadySignal
    {
        public event Action OnReady;

        public void Fire() => OnReady?.Invoke();
    }
}