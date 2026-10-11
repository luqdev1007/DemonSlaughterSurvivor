using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceEnemyChargeSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<EnemyAction, EnemyCharge, Facing>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<EnemyCharge> _charges = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Rushing> _rushing = default;
        private readonly EcsPoolInject<Boss> _bosses = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                ref EnemyCharge charge = ref _charges.Value.Get(entity);

                if (charge.CooldownTicks > 0)
                    charge.CooldownTicks--;

                ref EnemyAction action = ref _actions.Value.Get(entity);

                if (EnemyActionPhases.IsCharge(action.Phase) == false)
                    continue;

                action.PhaseTicks--;

                if (action.PhaseTicks > 0)
                    continue;

                ChargeAbilityConfig config = charge.Config;

                switch (action.Phase)
                {
                    case EnemyActionPhase.ChargeWindup:
                        charge.Direction = ResolveDirection(_facings.Value.Get(entity).Value);
                        EnemyActionPhases.Enter(ref action, EnemyActionPhase.ChargeLock, config.LockSeconds, delta);
                        break;
                    case EnemyActionPhase.ChargeLock:
                        charge.HasHit = false;
                        _rushing.Value.Add(entity);
                        EnemyActionPhases.Enter(ref action, EnemyActionPhase.ChargeRush, config.RushSeconds, delta);
                        break;
                    case EnemyActionPhase.ChargeRush:
                        _rushing.Value.Del(entity);
                        EnemyActionPhases.Enter(ref action, EnemyActionPhase.ChargeRecover, config.RecoverSeconds, delta);
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
