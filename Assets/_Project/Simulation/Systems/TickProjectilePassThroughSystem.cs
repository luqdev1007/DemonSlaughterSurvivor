using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickProjectilePassThroughSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<ProjectilePassThrough>> _filter = default;

        private readonly EcsPoolInject<ProjectilePassThrough> _passThroughs = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref ProjectilePassThrough passThrough = ref _passThroughs.Value.Get(entity);

                passThrough.RemainingTicks -= 1;

                if (passThrough.RemainingTicks > 0)
                    continue;

                _passThroughs.Value.Del(entity);
            }
        }
    }
}
