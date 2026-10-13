using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class FaceExecutionTargetSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Execution>> _executions = default;

        private readonly EcsPoolInject<Execution> _executionPool = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _executions.Value)
            {
                ref Execution execution = ref _executionPool.Value.Get(entity);

                if (execution.Owner.Unpack(world, out int owner) == false || _facings.Value.Has(owner) == false)
                    continue;

                if (execution.Target.Unpack(world, out int target) == false || _dead.Value.Has(target))
                    continue;

                Vector3 direction = _positions.Value.Get(target).Value - _positions.Value.Get(owner).Value;
                direction.y = 0f;

                if (direction.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                ref Facing facing = ref _facings.Value.Get(owner);

                float maxRadians = execution.Config.TurnSpeedDegrees * Mathf.Deg2Rad * _clock.Value.Delta;

                facing.Value = Vector3.RotateTowards(facing.Value, direction.normalized, maxRadians, 0f);
            }
        }
    }
}
