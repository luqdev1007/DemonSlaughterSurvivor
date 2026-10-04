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

        void SetBerserk(bool value);

        void SetDashing(bool value);

        void SetRunning(bool value);

        void SetSpecialAttack(bool value);

        void PlayAttack(string trigger, float speed);

        void PlayDeath();

        void Dissolve();
    }
}
