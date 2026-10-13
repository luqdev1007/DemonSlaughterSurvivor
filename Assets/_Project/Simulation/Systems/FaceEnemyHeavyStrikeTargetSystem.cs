using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class FaceEnemyHeavyStrikeTargetSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<EnemyAction, EnemyHeavyStrike, ChaseTarget, Position, Facing>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<EnemyHeavyStrike> _strikes = default;
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
                ref EnemyHeavyStrike strike = ref _strikes.Value.Get(entity);

                strike.TurnDegreesPerSecond = 0f;

                if (_actions.Value.Get(entity).Phase != EnemyActionPhase.StrikeWindup)
                    continue;

                if (RangedTarget.TryFindLiving(world, _chaseTargets.Value.Get(entity).Value, _positions.Value, _dead.Value, out Vector3 target) == false)
                    continue;

                Vector3 direction = target - _positions.Value.Get(entity).Value;
                direction.y = 0f;

                if (direction.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                ref Facing facing = ref _facings.Value.Get(entity);

                float delta = _clock.Value.Delta;
                float maxRadians = strike.Config.WindupTurnSpeedDegrees * Mathf.Deg2Rad * delta;

                Vector3 before = facing.Value;

                facing.Value = Vector3.RotateTowards(before, direction.normalized, maxRadians, 0f);

                strike.TurnDegreesPerSecond = Vector3.SignedAngle(before, facing.Value, Vector3.up) / delta;
            }
        }
    }
}
