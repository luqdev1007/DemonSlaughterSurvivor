using UnityEngine;

namespace Game.Core
{
    public interface IEliteView
    {
        void SetElite(float scale, GameObject vfxPrefab, Vector3 vfxOffset);
    }
}
