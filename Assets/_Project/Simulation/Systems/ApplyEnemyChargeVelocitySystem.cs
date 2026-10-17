using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ApplyEnemyChargeVelocitySystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<EnemyCharge, Rushing, Velocity>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyCharge> _charges = default;
        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                ref EnemyCharge charge = ref _charges.Value.Get(entity);

                Home(entity, ref charge, delta);

                _velocities.Value.Get(entity).Value = charge.Direction * charge.Config.RushSpeed;
            }
        }

        private void Home(int entity, ref EnemyCharge charge, float delta)
        {
            ChargeAbilityConfig config = charge.Config;

            if (config.HomingTurnSpeedDegrees <= 0f || _actions.Value.Has(entity) == false || _chaseTargets.Value.Has(entity) == false)
                return;

            int rushTicks = EnemyActionPhases.ToTicks(config.RushSeconds, delta);
            float elapsed = 1f - (float)_actions.Value.Get(entity).PhaseTicks / rushTicks;

            if (elapsed < config.HomingStartFraction)
                return;

            if (RangedTarget.TryFindLiving(_world.Value, _chaseTargets.Value.Get(entity).Value, _positions.Value, _dead.Value, out Vector3 target) == false)
                return;

            Vector3 toTarget = target - _positions.Value.Get(entity).Value;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= Mathf.Epsilon)
                return;

            Vector3 desired = Quaternion.Euler(0f, charge.HomingBias, 0f) * toTarget.normalized;
            float offset = Vector3.SignedAngle(charge.LockedDirection, desired, Vector3.up);
            float clamped = Mathf.Clamp(offset, -config.HomingMaxDegrees, config.HomingMaxDegrees);

            desired = Quaternion.Euler(0f, clamped, 0f) * charge.LockedDirection;

            charge.Direction = Vector3.RotateTowards(charge.Direction, desired, config.HomingTurnSpeedDegrees * Mathf.Deg2Rad * delta, 0f);
        }
    }
}
