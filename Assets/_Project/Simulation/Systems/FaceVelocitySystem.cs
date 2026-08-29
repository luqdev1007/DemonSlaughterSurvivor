using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class FaceVelocitySystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Velocity, Facing>> _filter = default;

        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Facing> _facings = default;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var velocity = ref _velocities.Value.Get(entity);
                ref var facing = ref _facings.Value.Get(entity);

                if (velocity.Value.sqrMagnitude > Mathf.Epsilon)
                {
                    facing.Value = velocity.Value;
                }
            }
        }
    }
}
