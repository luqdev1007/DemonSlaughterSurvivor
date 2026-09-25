using UnityEngine;

namespace Game.Core
{
    public interface IView
    {
        Transform Transform { get; }

        void SetPosition(Vector3 position);

        void SetRotation(Quaternion rotation);

        void PlayHit();

        void SetInvulnerable(bool value);

        void SetDashing(bool value);

        void PlayDeath();

        void Dissolve();
    }
}
