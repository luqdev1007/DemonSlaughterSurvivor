using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ReadMoveInputSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, MoveIntent>> _filter = default;
        private readonly EcsCustomInject<IInputService> _input = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private readonly EcsPoolInject<MoveIntent> _intents = default;

        public void Run(IEcsSystems systems)
        {
            Vector2 axis = _input.Value.MoveAxis;
            Vector3 direction = Quaternion.Euler(0f, _level.Value.CameraYaw, 0f) * new Vector3(axis.x, 0f, axis.y).normalized;

            foreach (var entity in _filter.Value)
            {
                ref var intent = ref _intents.Value.Get(entity);
                intent.Value = direction;
            }
        }
    }
}
