using UnityEngine;

namespace Game.Core
{
    public interface IViewFactory
    {
        Transform Create(GameObject prefab, Vector3 position);
        void Release(Transform view);
    }
}
