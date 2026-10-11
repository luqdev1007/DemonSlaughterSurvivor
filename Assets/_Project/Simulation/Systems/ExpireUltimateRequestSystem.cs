using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ExpireUltimateRequestSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<UltimateRequest>> _filter = default;

        private readonly EcsPoolInject<UltimateRequest> _requests = default;
        private readonly EcsPoolInject<Dashing> _dashing = default;
        private readonly EcsPoolInject<SpecialAttack> _specialAttacks = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<InputConfig> _input = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                if (_dashing.Value.Has(entity) || _specialAttacks.Value.Has(entity))
                    continue;

                ref UltimateRequest request = ref _requests.Value.Get(entity);

                request.Age += _clock.Value.Delta;

                if (request.Age < _input.Value.BufferSeconds)
                    continue;

                _requests.Value.Del(entity);
            }
        }
    }
}
