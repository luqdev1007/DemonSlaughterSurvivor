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

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
                _views.Value.Get(entity).Value.SetPosition(_positions.Value.Get(entity).Value);

            foreach (int entity in _orbs.Value)
                _views.Value.Get(entity).Value.SetPosition(_positions.Value.Get(entity).Value);
        }
    }
}
