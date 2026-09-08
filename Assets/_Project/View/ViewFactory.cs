using Game.Core;
using System;
using UnityEngine;

namespace Game.View
{
    public sealed class ViewFactory : IViewFactory
    {
        public IView Create(GameObject prefab, Vector3 position)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);

            if (instance.TryGetComponent(out ViewInterpolator view) == false)
                throw new InvalidOperationException(
                    $"Prefab '{prefab.name}' has no {nameof(ViewInterpolator)} on its root object.");

            return view;
        }

        public void Release(IView view)
        {
            if (view is not ViewInterpolator instance || instance == null)
                return;

            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}
