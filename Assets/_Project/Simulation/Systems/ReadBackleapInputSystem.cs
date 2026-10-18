using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ReadBackleapInputSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, BackleapStats>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<BackleapStats> _stats = default;
        private readonly EcsPoolInject<BackleapRequest> _requests = default;

        private readonly EcsCustomInject<IInputService> _input = default;
        private readonly EcsCustomInject<IDashInput> _dashInput = default;

        public void Run(IEcsSystems systems)
        {
            if (_filter.Value.GetEntitiesCount() == 0)
                return;

            bool pressed = _input.Value.ConsumeDashPressed();
            bool held = _dashInput.Value.IsDashHeld;

            foreach (int hero in _filter.Value)
            {
                _stats.Value.Get(hero).Held = held;

                if (pressed == false)
                    continue;

                ref BackleapRequest request = ref _requests.Value.Has(hero) ? ref _requests.Value.Get(hero) : ref _requests.Value.Add(hero);
                request.Age = 0f;
            }
        }
    }
}
