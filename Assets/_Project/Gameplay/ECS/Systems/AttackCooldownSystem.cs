using DemonSlaughter.Gameplay.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace DemonSlaughter.Gameplay.ECS.Systems
{
    public sealed class AttackCooldownSystem : IEcsRunSystem
    {
        private EcsFilter _filter;
        private EcsPool<AttackStateComponent> _attackStatePool;

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            if (_filter == null)
            {
                _filter = world
                    .Filter<PlayerTagComponent>()
                    .Inc<AttackStateComponent>()
                    .End();

                _attackStatePool = world.GetPool<AttackStateComponent>();
            }

            foreach (var entity in _filter)
            {
                ref var state = ref _attackStatePool.Get(entity);

                if (state.Cooldown > 0f)
                    state.Cooldown -= Time.deltaTime;
            }
        }
    }
}