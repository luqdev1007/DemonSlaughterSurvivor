using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ApplyDashVelocitySystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Dashing, Velocity>> _filter = default;

        private readonly EcsPoolInject<Dashing> _dashes = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Dashing dashing = ref _dashes.Value.Get(entity);
                ref Velocity velocity = ref _velocities.Value.Get(entity);

                velocity.Value = dashing.Direction * dashing.Speed;
            }
        }
    }
}
