using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ReapDeadEnemiesSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Enemy, Dead>> _filter = default;

        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _filter.Value)
            {
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
