using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PublishRuneStatusSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player>> _heroes = default;

        private readonly EcsPoolInject<ActiveRune> _active = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IRuneStatusSink> _sink = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                IRuneAuraView aura = _views.Value.Has(hero) ? _views.Value.Get(hero).Value as IRuneAuraView : null;

                if (_active.Value.Has(hero) == false)
                {
                    if (_sink.Value != null)
                        _sink.Value.Clear();

                    if (aura != null)
                        aura.SetRune(false, Color.clear, null, 0f, 0f);

                    return;
                }

                ref ActiveRune active = ref _active.Value.Get(hero);

                float remaining = active.RemainingTicks * delta;
                float total = active.TotalTicks * delta;

                if (_sink.Value != null)
                    _sink.Value.Publish(active.Config.DisplayName, active.Config.Color, remaining, total);

                if (aura != null)
                    aura.SetRune(true, active.Config.Color, active.Config.Glyph, total > 0f ? remaining / total : 0f, remaining);

                return;
            }
        }
    }
}
