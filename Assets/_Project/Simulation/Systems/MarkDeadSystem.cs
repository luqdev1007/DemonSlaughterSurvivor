using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class MarkDeadSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Health>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<Dead> _deads = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Health health = ref _healths.Value.Get(entity);

                if (health.Current > 0f)
                    continue;

                _deads.Value.Add(entity);
            }
        }
    }
}
