using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SyncBerserkViewSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<UltimateCharge, View>> _filter = default;

        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<BerserkMode> _modes = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value == null)
                    continue;

                view.Value.SetBerserk(_modes.Value.Has(entity));
            }
        }
    }
}
