using DemonSlaughter.Gameplay.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace DemonSlaughter.Gameplay.ECS.Systems
{
    public sealed class PlayerInputSystem : IEcsRunSystem
    {
        private readonly PlayerInputActions _inputActions;

        private EcsFilter _filter;
        private EcsPool<MoveInputComponent> _moveInputPool;

        public PlayerInputSystem(PlayerInputActions inputActions)
        {
            _inputActions = inputActions;
        }

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            if (_filter == null)
            {
                _filter = world
                    .Filter<PlayerTagComponent>()
                    .Inc<MoveInputComponent>()
                    .End();

                _moveInputPool = world.GetPool<MoveInputComponent>();
            }

            foreach (var entity in _filter)
            {
                ref var input = ref _moveInputPool.Get(entity);
                input.Value = _inputActions.Player.Move.ReadValue<Vector2>();
            }
        }
    }
}