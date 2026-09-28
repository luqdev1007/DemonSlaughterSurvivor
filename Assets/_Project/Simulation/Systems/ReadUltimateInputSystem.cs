using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ReadUltimateInputSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<UltimateRequest> _requests = default;

        private readonly EcsCustomInject<IInputService> _input = default;

        public void Run(IEcsSystems systems)
        {
            if (_input.Value.ConsumeUltimatePressed() == false)
                return;

            foreach (int entity in _filter.Value)
            {
                if (_requests.Value.Has(entity))
                    continue;

                ref UltimateRequest request = ref _requests.Value.Add(entity);

                request.Age = 0f;
            }
        }
    }
}
