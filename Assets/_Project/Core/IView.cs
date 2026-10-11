using UnityEngine;

namespace Game.Core
{
    public interface IView
    {
        Transform Transform { get; }

        void SetPosition(Vector3 position);

        void SetRotation(Quaternion rotation);

        void PlayHit(Vector3 direction);

        void PlayWeaponHit();

        void SetInvulnerable(bool value);

        void SetBerserk(bool value);

        void SetPowerUp(bool active, float progress);

        void SetDashing(bool value);

        void SetRunning(bool value);

        void SetSpecialAttack(bool value);

        void SetLift(float height);

        void PlayAttack(string trigger, float speed);

        void PlayDeath();

        void Dissolve();
    }
}
