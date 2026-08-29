using Game.Configs;
using Game.Core;
using Game.Simulation.Assets._Project.Simulation.Components;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation
{
    public sealed class ReadMoveInputSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, MoveIntent>> _filter = default;
        private readonly EcsCustomInject<IInputService> _input = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private float _yaw;

        public ReadMoveInputSystem()
        {
            _yaw = _level.Value.CameraYaw;
        }

        public void Run(IEcsSystems systems)
        {
            Vector2 axis = _input.Value.MoveAxis;

            foreach (var entity in _filter.Value)
            {
                Vector3 direction = Quaternion.Euler(0f, _yaw, 0f) * new Vector3(axis.x, 0f, axis.y);
                ref var moveIntent = ref direction;
            }
        }
    }
}
