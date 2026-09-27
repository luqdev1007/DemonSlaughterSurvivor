using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickWeaponCooldownSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<WeaponReady>> _filter = default;

        private readonly EcsPoolInject<WeaponReady> _readies = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                ref WeaponReady ready = ref _readies.Value.Get(entity);

                if (ready.Remaining <= 0f)
                    continue;

                ready.Remaining -= delta;

                if (ready.Remaining < 0f)
                    ready.Remaining = 0f;
            }
        }
    }
}
