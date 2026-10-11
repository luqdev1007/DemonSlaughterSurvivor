using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class FaceEnemyChargeTargetSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<EnemyAction, EnemyCharge, ChaseTarget, Position, Facing>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<EnemyCharge> _charges = default;
        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _filter.Value)
            {
                if (_actions.Value.Get(entity).Phase != EnemyActionPhase.ChargeWindup)
                    continue;

                if (RangedTarget.TryFindLiving(world, _chaseTargets.Value.Get(entity).Value, _positions.Value, _dead.Value, out Vector3 target) == false)
                    continue;

                Vector3 direction = target - _positions.Value.Get(entity).Value;
                direction.y = 0f;

                if (direction.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                ref Facing facing = ref _facings.Value.Get(entity);

                float maxRadians = _charges.Value.Get(entity).Config.WindupTurnSpeedDegrees * Mathf.Deg2Rad * _clock.Value.Delta;

                facing.Value = Vector3.RotateTowards(facing.Value, direction.normalized, maxRadians, 0f);
            }
        }
    }
}
