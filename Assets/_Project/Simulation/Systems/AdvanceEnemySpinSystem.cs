using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceEnemySpinSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<EnemyAction, EnemySpin>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<EnemySpin> _spins = default;
        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<BossRandom> _randoms = default;

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
                        spin.WanderAngle = 0f;
                        spin.WanderTarget = 0f;
                        spin.WanderTicks = 0;
                        spin.DashTicks = 0;
                        spin.DashDelayTicks = RollDashDelay(entity, config, delta);
                        _statModifiers.Value.Add(entity, StatId.MoveSpeed, StatOp.More, config.MoveSpeedMultiplier - 1f, config.Id);
                        EnemyActionPhases.Enter(ref action, EnemyActionPhase.Spinning, config.SpinSeconds, delta);
                        break;
                    case EnemyActionPhase.Spinning:
                        _statModifiers.Value.RemoveBySource(entity, config.Id);
                        spin.DashTicks = 0;
                        spin.DashDelayTicks = 0;
                        EnemyActionPhases.Enter(ref action, EnemyActionPhase.SpinRecover, config.RecoverSeconds, delta);
                        break;
                    default:
                        EnemyActionPhases.Finish(ref action, ResolveGlobalCooldown(entity), delta);
                        break;
                }
            }
        }

        private int RollDashDelay(int entity, SpinAbilityConfig config, float delta)
        {
            if (config.DashChance <= 0f || _randoms.Value.Has(entity) == false)
                return 0;

            ref BossRandom random = ref _randoms.Value.Get(entity);

            if (SimulationRandom.NextUnit(ref random.State) >= config.DashChance)
                return 0;

            float fraction = Mathf.Lerp(config.DashEarliestFraction, config.DashLatestFraction, SimulationRandom.NextUnit(ref random.State));

            return EnemyActionPhases.ToTicks(config.SpinSeconds * fraction, delta);
        }

        private float ResolveGlobalCooldown(int entity)
        {
            return _bosses.Value.Has(entity) ? _bosses.Value.Get(entity).Config.GlobalAbilityCooldownSeconds : 0f;
        }
    }
}
