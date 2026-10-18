using UnityEngine;

namespace Game.Core
{
    public interface IAimView
    {
        void SetAim(float twistDegrees, Vector2 localVelocity, int turnSide);
    }
}
