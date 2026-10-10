using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class FaceRangedTargetSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<RangedAttack, ChaseTarget, Position, Facing, TurnSpeed>, Exc<Dead, Pushed>> _filter = default;

        private readonly EcsPoolInject<RangedAttack> _attacks = default;
        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<TurnSpeed> _turnSpeeds = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _filter.Value)
            {
                if (_attacks.Value.Get(entity).IsHolding == false)
                    continue;

                if (RangedTarget.TryFindLiving(world, _chaseTargets.Value.Get(entity).Value, _positions.Value, _dead.Value, out Vector3 target) == false)
                    continue;

                Vector3 direction = target - _positions.Value.Get(entity).Value;
                direction.y = 0f;

                if (direction.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                ref Facing facing = ref _facings.Value.Get(entity);

                float maxRadians = _turnSpeeds.Value.Get(entity).Value * Mathf.Deg2Rad * _clock.Value.Delta;

                facing.Value = Vector3.RotateTowards(facing.Value, direction.normalized, maxRadians, 0f);
            }
        }
    }
}
