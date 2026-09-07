using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ExpireDashRequestSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<DashRequest>> _filter = default;

        private readonly EcsPoolInject<DashRequest> _requests = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<InputConfig> _input = default;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref DashRequest request = ref _requests.Value.Get(entity);

                request.Age += _clock.Value.Delta;

                if (request.Age < _input.Value.BufferSeconds)
                    continue;

                _requests.Value.Del(entity);
            }
        }
    }
}
