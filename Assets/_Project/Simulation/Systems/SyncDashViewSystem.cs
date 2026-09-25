using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SyncDashViewSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<DashStats, View>> _filter = default;

        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<Dashing> _dashes = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value == null)
                    continue;

                view.Value.SetDashing(_dashes.Value.Has(entity));
            }
        }
    }
}
