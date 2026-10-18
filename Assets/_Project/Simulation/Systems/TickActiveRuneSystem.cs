using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class TickActiveRuneSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<ActiveRune>> _active = default;

        private readonly EcsPoolInject<ActiveRune> _activePool = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<UltimateCharge> _charges = default;
        private readonly EcsPoolInject<HealEvent> _healEvents = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _active.Value)
            {
                ref ActiveRune active = ref _activePool.Value.Get(hero);
                RuneConfig config = active.Config;

                if (config.RegenFraction > 0f && _dead.Value.Has(hero) == false)
                {
                    active.PulseTicks--;

                    if (active.PulseTicks <= 0)
                    {
                        active.PulseTicks = Mathf.Max(1, Mathf.RoundToInt(config.RegenIntervalSeconds / _clock.Value.Delta));
                        Regenerate(world, hero, config.RegenFraction);
                    }
                }

                active.RemainingTicks--;

                if (active.RemainingTicks > 0)
                    continue;

                _statModifiers.Value.RemoveBySource(hero, config.ModifierSource);
                _activePool.Value.Del(hero);
            }
        }

        private void Regenerate(EcsWorld world, int hero, float fraction)
        {
            if (_maxHealths.Value.Has(hero))
            {
                ref HealEvent heal = ref _healEvents.Value.Add(world.NewEntity());
                heal.Target = world.PackEntity(hero);
                heal.Amount = _maxHealths.Value.Get(hero).Value * fraction;
            }

            if (_charges.Value.Has(hero))
            {
                ref UltimateCharge charge = ref _charges.Value.Get(hero);
                charge.Value = Mathf.Min(charge.Max, charge.Value + charge.Max * fraction);
            }
        }
    }
}
