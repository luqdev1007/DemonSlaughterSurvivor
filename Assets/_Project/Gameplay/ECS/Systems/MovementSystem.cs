using DemonSlaughter.Gameplay.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace DemonSlaughter.Gameplay.ECS.Systems
{
    public sealed class MovementSystem : IEcsRunSystem
    {
        private EcsFilter _filter;
        private EcsPool<MoveInputComponent> _moveInputPool;
        private EcsPool<MovementComponent> _movementPool;
        private EcsPool<TransformRefComponent> _transformPool;

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            if (_filter == null)
            {
                _filter = world
                    .Filter<PlayerTagComponent>()
                    .Inc<MoveInputComponent>()
                    .Inc<MovementComponent>()
                    .Inc<TransformRefComponent>()
                    .End();

                _moveInputPool = world.GetPool<MoveInputComponent>();
                _movementPool = world.GetPool<MovementComponent>();
                _transformPool = world.GetPool<TransformRefComponent>();
            }

            foreach (var entity in _filter)
            {
                ref var input = ref _moveInputPool.Get(entity);
                ref var movement = ref _movementPool.Get(entity);
                ref var transformRef = ref _transformPool.Get(entity);

                if (input.Value == Vector2.zero)
                    continue;

                var direction = new Vector3(input.Value.x, 0f, input.Value.y);
                var position = transformRef.Value.position;

                position += direction * (movement.Speed * Time.deltaTime);
                transformRef.Value.position = position;

                transformRef.Value.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}