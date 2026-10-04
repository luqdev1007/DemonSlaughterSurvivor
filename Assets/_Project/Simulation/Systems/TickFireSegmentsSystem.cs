using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickFireSegmentsSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<FireSegment>> _segments = default;

        private readonly EcsPoolInject<FireSegment> _segmentPool = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _segments.Value)
            {
                ref FireSegment segment = ref _segmentPool.Value.Get(entity);

                segment.RemainingTicks--;

                if (segment.RemainingTicks > 0)
                    continue;

                if (_views.Value.Has(entity))
                {
                    ref View view = ref _views.Value.Get(entity);

                    if (view.Value != null)
                        _viewFactory.Value.Release(view.Value);

                    view.Value = null;
                }

                world.DelEntity(entity);
            }
        }
    }
}
