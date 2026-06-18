using Leopotam.EcsLite;
using DemonSlaughter.Gameplay.ECS.Components;
using DemonSlaughter.Gameplay.Combat;
using UnityEngine;

namespace DemonSlaughter.Gameplay.Combat
{
    public sealed class AttackAnimationEventReceiver : MonoBehaviour
    {
        private EcsWorld _world;
        private int _entity;
        private float _attackCooldown;
        private SwordHitbox _swordHitbox;

        public void Initialize(
            EcsWorld world,
            int entity,
            float cooldown,
            SwordHitbox swordHitbox)
        {
            _world = world;
            _entity = entity;
            _attackCooldown = cooldown;
            _swordHitbox = swordHitbox;
        }

        // Animation Event — начало активной фазы удара
        public void EnableHitbox()
        {
            _swordHitbox.EnableHitbox();
        }

        // Animation Event — конец активной фазы удара  
        public void DisableHitbox()
        {
            _swordHitbox.DisableHitbox();
        }

        // Animation Event — конец всей анимации атаки
        public void OnAttackFinished()
        {
            _swordHitbox.DisableHitbox();

            var pool = _world.GetPool<AttackStateComponent>();

            if (!pool.Has(_entity)) 
                return;

            ref var state = ref pool.Get(_entity);
            state.IsAttacking = false;
            state.Cooldown = _attackCooldown;
        }
    }
}