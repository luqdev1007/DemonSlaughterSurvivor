using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceEnemyHeavyStrikeSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<EnemyAction, EnemyHeavyStrike, Facing>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<EnemyHeavyStrike> _strikes = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Boss> _bosses = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                ref EnemyHeavyStrike strike = ref _strikes.Value.Get(entity);

                if (strike.CooldownTicks > 0)
                    strike.CooldownTicks--;

                ref EnemyAction action = ref _actions.Value.Get(entity);

                if (EnemyActionPhases.IsHeavyStrike(action.Phase) == false)
                    continue;

                action.PhaseTicks--;

                if (action.PhaseTicks > 0)
                    continue;

                HeavyStrikeAbilityConfig config = strike.Config;

                switch (action.Phase)
                {
                    case EnemyActionPhase.StrikeWindup:
                        strike.Direction = ResolveDirection(_facings.Value.Get(entity).Value);
                        EnemyActionPhases.Enter(ref action, EnemyActionPhase.StrikeLock, config.LockSeconds, delta);
                        break;
                    case EnemyActionPhase.StrikeLock:
                        strike.ImpactPending = true;
                        EnemyActionPhases.Enter(ref action, EnemyActionPhase.StrikeRecover, config.RecoverSeconds, delta);
                        break;
                    default:
                        EnemyActionPhases.Finish(ref action, ResolveGlobalCooldown(entity), delta);
                        break;
                }
            }
        }

        private float ResolveGlobalCooldown(int entity)
        {
            return _bosses.Value.Has(entity) ? _bosses.Value.Get(entity).Config.GlobalAbilityCooldownSeconds : 0f;
        }

        private static Vector3 ResolveDirection(Vector3 facing)
        {
            facing.y = 0f;

            return facing.sqrMagnitude > Mathf.Epsilon ? facing.normalized : Vector3.forward;
        }
    }
}
