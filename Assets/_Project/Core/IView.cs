using UnityEngine;

namespace Game.Core
{
    public interface IView
    {
        Transform Transform { get; }

        void SetPosition(Vector3 position);

        void SetRotation(Quaternion rotation);
    }
}
