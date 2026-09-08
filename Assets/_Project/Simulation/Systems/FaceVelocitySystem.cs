using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class FaceVelocitySystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Velocity, Facing, TurnSpeed>> _filter = default;

        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<TurnSpeed> _turnSpeeds = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var velocity = ref _velocities.Value.Get(entity);
                ref var facing = ref _facings.Value.Get(entity);
                ref var turnSpeed = ref _turnSpeeds.Value.Get(entity);

                if (velocity.Value.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                float maxRadians = turnSpeed.Value * Mathf.Deg2Rad * _clock.Value.Delta;

                facing.Value = Vector3.RotateTowards(facing.Value, velocity.Value, maxRadians, 0f);
            }
        }
    }
}
