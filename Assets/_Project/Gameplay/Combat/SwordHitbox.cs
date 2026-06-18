using System;
using UnityEngine;

namespace DemonSlaughter.Gameplay.Combat
{
    public sealed class SwordHitbox : MonoBehaviour
    {
        public event Action<Collider> OnHit;

        private bool _isActive;

        // Animation Event
        public void EnableHitbox() => _isActive = true;

        // Animation Event
        public void DisableHitbox() => _isActive = false;

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) 
                return;

            OnHit?.Invoke(other);
        }
    }
}