using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class StorePreviousPositionSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, Position, PreviousPosition>> _filter = default;

        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Position position = ref _positions.Value.Get(entity);
                ref PreviousPosition previous = ref _previousPositions.Value.Get(entity);

                previous.Value = position.Value;
            }
        }
    }
}
