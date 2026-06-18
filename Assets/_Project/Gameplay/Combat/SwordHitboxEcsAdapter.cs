using DemonSlaughter.Gameplay.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace DemonSlaughter.Gameplay.Combat
{
    public sealed class SwordHitboxEcsAdapter : MonoBehaviour
    {
        private SwordHitbox _hitbox;
        private EcsWorld _world;
        private int _ownerEntity;
        private EcsPool<DamageReceivedComponent> _damagePool;
        private float _damage;

        public void Initialize(EcsWorld world, int ownerEntity, float damage)
        {
            _world = world;
            _ownerEntity = ownerEntity;
            _damage = damage;

            _hitbox = GetComponent<SwordHitbox>();
            _hitbox.OnHit += OnHit;

            _damagePool = world.GetPool<DamageReceivedComponent>();
        }

        private void OnHit(Collider other)
        {
            var entityLink = other.GetComponent<EcsEntityLink>();

            if (entityLink == null) 
                return;

            var enemyEntity = entityLink.Entity;

            if (_damagePool.Has(enemyEntity)) 
                return;

            ref var damage = ref _damagePool.Add(enemyEntity);
            damage.Value = _damage;
            damage.AttackerEntity = _ownerEntity;
        }

        private void OnDestroy()
        {
            if (_hitbox != null)
                _hitbox.OnHit -= OnHit;
        }
    }
}