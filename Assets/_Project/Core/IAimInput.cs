using UnityEngine;

namespace Game.Core
{
    public interface IAimInput
    {
        bool TryGetAimPoint(out Vector3 point);
    }
}
