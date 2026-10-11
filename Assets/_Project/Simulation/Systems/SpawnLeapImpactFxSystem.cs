using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SpawnLeapImpactFxSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<LeapLandedEvent>> _landed = default;

        private readonly EcsPoolInject<LeapLandedEvent> _landedEvents = default;
        private readonly EcsPoolInject<LeapImpactFx> _impacts = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _landed.Value)
            {
                ref LeapLandedEvent landed = ref _landedEvents.Value.Get(entity);

                if (landed.Config == null || landed.Config.ImpactFxPrefab == null || landed.Config.ImpactFxSeconds <= 0f)
                    continue;

                int fx = world.NewEntity();

                _impacts.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(landed.Config.ImpactFxSeconds / _clock.Value.Delta));
                _positions.Value.Add(fx).Value = landed.Position;
                _notIndexed.Value.Add(fx);
                _views.Value.Add(fx).Value = _viewFactory.Value.Create(landed.Config.ImpactFxPrefab, landed.Position);
            }
        }
    }
}
