using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickDashCooldownSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<DashCooldown>> _filter = default;

        private readonly EcsPoolInject<DashCooldown> _cooldowns = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref DashCooldown cooldown = ref _cooldowns.Value.Get(entity);

                cooldown.Remaining -= _clock.Value.Delta;

                if (cooldown.Remaining > 0f)
                    continue;

                _cooldowns.Value.Del(entity);
            }
        }
    }
}
