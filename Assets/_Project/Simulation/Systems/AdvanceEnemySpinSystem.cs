using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceEnemySpinSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<EnemyAction, EnemySpin>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<EnemySpin> _spins = default;
        private readonly EcsPoolInject<Boss> _bosses = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                ref EnemySpin spin = ref _spins.Value.Get(entity);

                if (spin.CooldownTicks > 0)
                    spin.CooldownTicks--;

                ref EnemyAction action = ref _actions.Value.Get(entity);

                if (EnemyActionPhases.IsSpin(action.Phase) == false)
                    continue;

                action.PhaseTicks--;

                if (action.PhaseTicks > 0)
                    continue;

                SpinAbilityConfig config = spin.Config;

                switch (action.Phase)
                {
                    case EnemyActionPhase.SpinRaise:
                        spin.PulseTicks = 0;
                        spin.PulseTotalTicks = EnemyActionPhases.ToTicks(config.TickSeconds, delta);
                        _statModifiers.Value.Add(entity, StatId.MoveSpeed, StatOp.More, config.MoveSpeedMultiplier - 1f, config.Id);
                        EnemyActionPhases.Enter(ref action, EnemyActionPhase.Spinning, config.SpinSeconds, delta);
                        break;
                    case EnemyActionPhase.Spinning:
                        _statModifiers.Value.RemoveBySource(entity, config.Id);
                        EnemyActionPhases.Enter(ref action, EnemyActionPhase.SpinRecover, config.RecoverSeconds, delta);
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
    }
}
