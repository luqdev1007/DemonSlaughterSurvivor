using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class RebuildSpatialGridSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Position>> _filter = default;

        private readonly EcsPoolInject<Position> _positions = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;

        public void Run(IEcsSystems systems)
        {
            SpatialGrid grid = _grid.Value;

            grid.Clear();

            foreach (int entity in _filter.Value)
            {
                ref Position position = ref _positions.Value.Get(entity);

                grid.Add(entity, position.Value);
            }
        }
    }
}
