using DemonSlaughter.Gameplay.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace DemonSlaughter.Gameplay.ECS.Systems
{
    public sealed class AttackDetectionSystem : IEcsRunSystem
    {
        private EcsFilter _playerFilter;
        private EcsFilter _enemyFilter;

        private EcsPool<TransformRefComponent> _transformPool;
        private EcsPool<AttackDetectorComponent> _detectorPool;
        private EcsPool<AttackStateComponent> _attackStatePool;
        private EcsPool<AttackRequestComponent> _attackRequestPool;

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            if (_playerFilter == null)
            {
                _playerFilter = world
                    .Filter<PlayerTagComponent>()
                    .Inc<TransformRefComponent>()
                    .Inc<AttackDetectorComponent>()
                    .Inc<AttackStateComponent>()
                    .End();

                _enemyFilter = world
                    .Filter<EnemyTagComponent>()
                    .Inc<TransformRefComponent>()
                    .End();

                _transformPool = world.GetPool<TransformRefComponent>();
                _detectorPool = world.GetPool<AttackDetectorComponent>();
                _attackStatePool = world.GetPool<AttackStateComponent>();
                _attackRequestPool = world.GetPool<AttackRequestComponent>();
            }

            foreach (var playerEntity in _playerFilter)
            {
                ref var state = ref _attackStatePool.Get(playerEntity);

                if (state.Cooldown > 0f || state.IsAttacking) 
                    continue;

                ref var playerTransform = ref _transformPool.Get(playerEntity);
                ref var detector = ref _detectorPool.Get(playerEntity);

                if (!HasEnemyInRange(playerTransform.Value, detector)) 
                    continue;

                if (!_attackRequestPool.Has(playerEntity))
                    _attackRequestPool.Add(playerEntity);
            }
        }

        private bool HasEnemyInRange(
            Transform playerTransform,
            in AttackDetectorComponent detector)
        {
            foreach (var enemyEntity in _enemyFilter)
            {
                ref var enemyTransform = ref _transformPool.Get(enemyEntity);

                var toEnemy = enemyTransform.Value.position - playerTransform.position;
                var distance = toEnemy.magnitude;

                if (distance > detector.DetectionRadius) 
                    continue;

                var angle = Vector3.Angle(playerTransform.forward, toEnemy);

                if (angle <= detector.DetectionAngle * 0.5f)
                    return true;
            }

            return false;
        }
    }
}