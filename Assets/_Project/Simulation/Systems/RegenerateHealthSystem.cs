using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class RegenerateHealthSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Recovery, Health, MaxHealth>, Exc<Dead>> _heroes = default;

        private readonly EcsPoolInject<Recovery> _recoveries = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<HealEvent> _healEvents = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                float regen = _recoveries.Value.Get(hero).Regen;

                if (regen <= 0f)
                    continue;

                if (_healths.Value.Get(hero).Current >= _maxHealths.Value.Get(hero).Value)
                    continue;

                ref HealEvent heal = ref _healEvents.Value.Add(world.NewEntity());
                heal.Target = world.PackEntity(hero);
                heal.Amount = regen * _clock.Value.Delta;
                heal.LifeSteal = false;
            }
        }
    }
}
