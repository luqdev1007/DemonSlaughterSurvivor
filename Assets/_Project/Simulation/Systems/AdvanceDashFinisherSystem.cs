using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceDashFinisherSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DashFinisher>> _finishers = default;

        private readonly EcsPoolInject<DashFinisher> _finisherPool = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _finishers.Value)
            {
                ref DashFinisher finisher = ref _finisherPool.Value.Get(entity);

                finisher.RemainingTicks--;

                if (finisher.Owner.Unpack(world, out _) == false || finisher.RemainingTicks <= 0)
                    world.DelEntity(entity);
            }
        }
    }
}
