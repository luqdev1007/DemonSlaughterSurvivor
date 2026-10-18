using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SyncAirViewSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<BackleapStats, View>> _filter = default;

        private readonly EcsPoolInject<BackleapStats> _stats = default;
        private readonly EcsPoolInject<Backleaping> _backleaps = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                if (_views.Value.Get(entity).Value is IAirView view == false)
                    continue;

                if (_backleaps.Value.Has(entity) == false)
                {
                    view.SetAir(0, 1f, false);

                    continue;
                }

                BackleapPhase phase = _backleaps.Value.Get(entity).Phase;
                bool wings = phase == BackleapPhase.Leap || phase == BackleapPhase.Hover;

                view.SetAir((int)phase, _stats.Value.Get(entity).Config.LeapAnimationSpeed, wings);
            }
        }
    }
}
