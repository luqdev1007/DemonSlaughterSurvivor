using DemonSlaughter.Gameplay.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace DemonSlaughter.Gameplay.ECS.Systems
{
    public sealed class AnimationSystem : IEcsRunSystem
    {
        private static readonly int IsRunning = Animator.StringToHash("IsRunning");

        private EcsFilter _filter;
        private EcsPool<MoveInputComponent> _moveInputPool;
        private EcsPool<AnimatorRefComponent> _animatorPool;

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            if (_filter == null)
            {
                _filter = world
                    .Filter<PlayerTagComponent>()
                    .Inc<MoveInputComponent>()
                    .Inc<AnimatorRefComponent>()
                    .End();

                _moveInputPool = world.GetPool<MoveInputComponent>();
                _animatorPool = world.GetPool<AnimatorRefComponent>();
            }

            foreach (var entity in _filter)
            {
                ref var input = ref _moveInputPool.Get(entity);
                ref var animatorRef = ref _animatorPool.Get(entity);

                var isMoving = input.Value != Vector2.zero;
                animatorRef.Value.SetBool(IsRunning, isMoving);
            }
        }
    }
}