using DemonSlaughter.Gameplay.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace DemonSlaughter.Gameplay.ECS.Systems
{
    public sealed class AttackSystem : IEcsRunSystem
    {
        private static readonly int AttackIndex = Animator.StringToHash("AttackIndex");
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");

        private readonly string[] _attackTriggers;

        private EcsFilter _requestFilter;
        private EcsPool<AttackRequestComponent> _requestPool;
        private EcsPool<AttackStateComponent> _attackStatePool;
        private EcsPool<AnimatorRefComponent> _animatorPool;

        public AttackSystem(string[] attackTriggers)
        {
            _attackTriggers = attackTriggers;
        }

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            if (_requestFilter == null)
            {
                _requestFilter = world
                    .Filter<PlayerTagComponent>()
                    .Inc<AttackRequestComponent>()
                    .Inc<AttackStateComponent>()
                    .Inc<AnimatorRefComponent>()
                    .End();

                _requestPool = world.GetPool<AttackRequestComponent>();
                _attackStatePool = world.GetPool<AttackStateComponent>();
                _animatorPool = world.GetPool<AnimatorRefComponent>();
            }

            foreach (var entity in _requestFilter)
            {
                ref var state = ref _attackStatePool.Get(entity);
                ref var animatorRef = ref _animatorPool.Get(entity);

                var triggerName = _attackTriggers[state.ComboIndex % _attackTriggers.Length];
                animatorRef.Value.SetTrigger(triggerName);

                state.IsAttacking = true;
                state.ComboIndex++;

                _requestPool.Del(entity);
            }
        }
    }
}