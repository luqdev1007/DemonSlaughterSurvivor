using UnityEngine;
using Game.Core;

namespace Game.View
{
    public sealed class ViewFactory : IViewFactory
    {
        public Transform Create(GameObject prefab, Vector3 position)
        {
            return Object.Instantiate(prefab, position, Quaternion.identity).transform;
        }

        public void Release(Transform view)
        {
            Object.Destroy(view.gameObject);
        }
    }
}
