using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class DebugLevelUpSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player>, Exc<Dead>> _heroes = default;

        private readonly EcsPoolInject<LevelUpEvent> _events = default;

        private readonly EcsCustomInject<IDebugLevelUpInput> _input = default;

        public void Run(IEcsSystems systems)
        {
            if (_input.Value.ConsumePressed() == false)
                return;

            foreach (int hero in _heroes.Value)
            {
                if (_events.Value.Has(hero) == false)
                    _events.Value.Add(hero);

                _events.Value.Get(hero).Count++;
            }
        }
    }
}
