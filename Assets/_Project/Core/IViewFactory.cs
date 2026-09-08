using UnityEngine;

namespace Game.Core
{
    public interface IViewFactory
    {
        IView Create(GameObject prefab, Vector3 position);

        void Release(IView view);
    }
}
