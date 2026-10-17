using UnityEngine;

namespace Game.Core
{
    public interface IPointerInput
    {
        Vector2 PointerPosition { get; }

        bool IsStandHeld { get; }
    }
}
