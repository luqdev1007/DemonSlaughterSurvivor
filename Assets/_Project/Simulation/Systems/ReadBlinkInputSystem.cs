using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ReadBlinkInputSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, BlinkStats>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<BlinkRequest> _requests = default;

        private readonly EcsCustomInject<IInputService> _input = default;

        public void Run(IEcsSystems systems)
        {
            if (_filter.Value.GetEntitiesCount() == 0)
                return;

            if (_input.Value.ConsumeDashPressed() == false)
                return;

            foreach (int hero in _filter.Value)
            {
                ref BlinkRequest request = ref _requests.Value.Has(hero) ? ref _requests.Value.Get(hero) : ref _requests.Value.Add(hero);
                request.Age = 0f;
            }
        }
    }
}
