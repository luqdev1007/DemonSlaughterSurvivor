using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class MoveSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Position, Velocity>> _filter = default;

        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var position = ref _positions.Value.Get(entity);
                ref var velocity = ref _velocities.Value.Get(entity);

                position.Value += velocity.Value * _clock.Value.Delta;
            }
        }
    }
}
