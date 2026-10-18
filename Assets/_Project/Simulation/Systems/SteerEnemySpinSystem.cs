using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SteerEnemySpinSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<EnemySpin, EnemyAction, Velocity, Position>, Exc<Dead, Pushed>> _filter = default;

        private readonly EcsPoolInject<EnemySpin> _spins = default;
        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<BossRandom> _randoms = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                if (_actions.Value.Get(entity).Phase != EnemyActionPhase.Spinning)
                    continue;

                ref EnemySpin spin = ref _spins.Value.Get(entity);
                ref Velocity velocity = ref _velocities.Value.Get(entity);

                if (spin.DashDelayTicks > 0)
                {
                    spin.DashDelayTicks--;

                    if (spin.DashDelayTicks == 0)
                        StartDash(entity, ref spin, delta);
                }

                if (spin.DashTicks > 0)
                {
                    spin.DashTicks--;
                    velocity.Value = spin.DashVelocity;

                    continue;
                }

                Wander(entity, ref spin, delta);

                velocity.Value = Quaternion.Euler(0f, spin.WanderAngle, 0f) * velocity.Value;
            }
        }

        private void Wander(int entity, ref EnemySpin spin, float delta)
        {
            SpinAbilityConfig config = spin.Config;

            if (config.WanderDegrees <= 0f || _randoms.Value.Has(entity) == false)
                return;

            if (spin.WanderTicks <= 0)
            {
                spin.WanderTarget = (SimulationRandom.NextUnit(ref _randoms.Value.Get(entity).State) * 2f - 1f) * config.WanderDegrees;
                spin.WanderTicks = EnemyActionPhases.ToTicks(config.WanderRetargetSeconds, delta);
            }

            spin.WanderTicks--;
            spin.WanderAngle = Mathf.MoveTowards(spin.WanderAngle, spin.WanderTarget, config.WanderTurnSpeedDegrees * delta);
        }

        private void StartDash(int entity, ref EnemySpin spin, float delta)
        {
            SpinAbilityConfig config = spin.Config;

            if (config.DashRepeatSeconds > 0f)
                spin.DashDelayTicks = EnemyActionPhases.ToTicks(config.DashSeconds + config.DashRepeatSeconds, delta);

            if (_chaseTargets.Value.Has(entity) == false)
                return;

            if (RangedTarget.TryFindLiving(_world.Value, _chaseTargets.Value.Get(entity).Value, _positions.Value, _dead.Value, out Vector3 target) == false)
                return;

            Vector3 toTarget = target - _positions.Value.Get(entity).Value;
            toTarget.y = 0f;

            float distance = Mathf.Min(toTarget.magnitude, config.DashMaxDistance);

            if (distance <= Mathf.Epsilon)
                return;

            spin.DashTicks = EnemyActionPhases.ToTicks(config.DashSeconds, delta);
            spin.DashVelocity = toTarget.normalized * (distance / (spin.DashTicks * delta));
        }
    }
}
