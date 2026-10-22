using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceEnemyJumpSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<EnemyAction, EnemyJump, Position, Facing>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<EnemyJump> _jumps = default;
        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                ref EnemyJump jump = ref _jumps.Value.Get(entity);

                if (jump.CooldownTicks > 0)
                    jump.CooldownTicks--;

                ref EnemyAction action = ref _actions.Value.Get(entity);

                if (EnemyActionPhases.IsJump(action.Phase) == false)
                {
                    jump.Lift = 0f;

                    continue;
                }

                JumpAbilityConfig config = jump.Config;

                if (action.Phase == EnemyActionPhase.JumpAir)
                {
                    float progress = action.PhaseTotalTicks > 0 ? 1f - (float)(action.PhaseTicks - 1) / action.PhaseTotalTicks : 1f;

                    jump.Lift = config.Lift(progress);
                    TurnToTarget(entity, config, delta);
                }
                else
                {
                    jump.Lift = 0f;
                }

                action.PhaseTicks--;

                if (action.PhaseTicks > 0)
                    continue;

                if (action.Phase == EnemyActionPhase.JumpAir)
                {
                    jump.Lift = 0f;
                    jump.ImpactPending = true;
                    EnemyActionPhases.Enter(ref action, EnemyActionPhase.JumpRecover, config.RecoverSeconds, delta);
                }
                else
                {
                    EnemyActionPhases.Finish(ref action, ResolveGlobalCooldown(entity), delta);
                }
            }
        }

        private void TurnToTarget(int entity, JumpAbilityConfig config, float delta)
        {
            if (_chaseTargets.Value.Has(entity) == false)
                return;

            if (RangedTarget.TryFindLiving(_world.Value, _chaseTargets.Value.Get(entity).Value, _positions.Value, _dead.Value, out Vector3 target) == false)
                return;

            Vector3 direction = target - _positions.Value.Get(entity).Value;
            direction.y = 0f;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            ref Facing facing = ref _facings.Value.Get(entity);
            facing.Value = Vector3.RotateTowards(facing.Value, direction.normalized, config.TurnSpeedDegrees * Mathf.Deg2Rad * delta, 0f);
        }

        private float ResolveGlobalCooldown(int entity)
        {
            return _bosses.Value.Has(entity) ? _bosses.Value.Get(entity).Config.GlobalAbilityCooldownSeconds : 0f;
        }
    }
}
