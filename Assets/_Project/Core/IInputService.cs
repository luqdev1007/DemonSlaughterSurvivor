using UnityEngine;

namespace Game.Core
{
    public interface IInputService
    {
        Vector2 MoveAxis { get; }

        bool ConsumeDashPressed();

        void ResetLatches();
    }
}
