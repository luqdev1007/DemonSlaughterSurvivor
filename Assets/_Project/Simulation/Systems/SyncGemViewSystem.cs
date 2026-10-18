using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SyncGemViewSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Gem, Position, View>> _filter = default;
        private readonly EcsFilterInject<Inc<HealOrb, Position, View>> _orbs = default;

        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<GemFlight> _flights = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
                Sync(entity);

            foreach (int entity in _orbs.Value)
                Sync(entity);
        }

        private void Sync(int entity)
        {
            IView view = _views.Value.Get(entity).Value;

            view.SetPosition(_positions.Value.Get(entity).Value);

            if (view is IPickupView pickup == false)
                return;

            float progress = 0f;

            if (_flights.Value.Has(entity))
            {
                ref GemFlight flight = ref _flights.Value.Get(entity);
                progress = flight.TotalTicks > 0 ? (float)flight.ElapsedTicks / flight.TotalTicks : 1f;
            }

            pickup.SetPickupFlight(progress);
        }
    }
}
