using UnityEngine;

namespace Game.Core
{
    public interface ICameraService
    {
        void SetFollowTarget(Transform target);

        bool TryWorldToViewport(Vector3 world, out Vector3 viewport);
    }
}
