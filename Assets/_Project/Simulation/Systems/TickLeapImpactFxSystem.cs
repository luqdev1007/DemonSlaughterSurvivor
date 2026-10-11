using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickLeapImpactFxSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<LeapImpactFx>> _impacts = default;

        private readonly EcsPoolInject<LeapImpactFx> _impactPool = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _impacts.Value)
            {
                ref LeapImpactFx impact = ref _impactPool.Value.Get(entity);

                impact.RemainingTicks--;

                if (impact.RemainingTicks > 0)
                    continue;

                if (_views.Value.Has(entity))
                {
                    ref View view = ref _views.Value.Get(entity);

                    _viewFactory.Value.Release(view.Value);

                    view.Value = null;
                }

                world.DelEntity(entity);
            }
        }
    }
}
