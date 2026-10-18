using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PublishRuneStatusSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player>> _heroes = default;

        private readonly EcsPoolInject<ActiveRune> _active = default;

        private readonly EcsCustomInject<IRuneStatusSink> _sink = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            if (_sink.Value == null)
                return;

            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                if (_active.Value.Has(hero) == false)
                {
                    _sink.Value.Clear();

                    return;
                }

                ref ActiveRune active = ref _active.Value.Get(hero);

                _sink.Value.Publish(active.Config.DisplayName, active.Config.Color, active.RemainingTicks * delta, active.TotalTicks * delta);

                return;
            }
        }
    }
}
