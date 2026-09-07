using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceDashSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Dashing, DashStats>> _filter = default;

        private readonly EcsPoolInject<Dashing> _dashes = default;
        private readonly EcsPoolInject<DashStats> _stats = default;
        private readonly EcsPoolInject<DashCooldown> _cooldowns = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Dashing dashing = ref _dashes.Value.Get(entity);

                dashing.Remaining -= _clock.Value.Delta;

                if (dashing.Remaining > 0f)
                    continue;

                ref DashStats stats = ref _stats.Value.Get(entity);
                ref DashCooldown cooldown = ref _cooldowns.Value.Add(entity);

                cooldown.Remaining = stats.Cooldown;

                _dashes.Value.Del(entity);
            }
        }
    }
}
