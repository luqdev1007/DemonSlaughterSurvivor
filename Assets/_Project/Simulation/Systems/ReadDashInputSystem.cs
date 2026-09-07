using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ReadDashInputSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player>> _filter = default;

        private readonly EcsPoolInject<DashRequest> _requests = default;

        private readonly EcsCustomInject<IInputService> _input = default;

        public void Run(IEcsSystems systems)
        {
            if (_input.Value.ConsumeDashPressed() == false)
                return;

            foreach (int entity in _filter.Value)
            {
                if (_requests.Value.Has(entity))
                    continue;

                ref DashRequest request = ref _requests.Value.Add(entity);

                request.Age = 0f;
            }
        }
    }
}
