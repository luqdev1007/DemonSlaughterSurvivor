using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SyncPushLiftViewSystem : IEcsRunSystem
    {
        private const int SettleTicks = 2;

        private readonly EcsFilterInject<Inc<Enemy, Pushed, View>> _pushed = default;
        private readonly EcsFilterInject<Inc<Enemy, Lifted, View>> _lifted = default;

        private readonly EcsPoolInject<Pushed> _pushes = default;
        private readonly EcsPoolInject<Lifted> _liftedPool = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _pushed.Value)
            {
                ref Pushed pushed = ref _pushes.Value.Get(entity);

                if (pushed.ArcHeight <= 0f)
                    continue;

                ref View view = ref _views.Value.Get(entity);

                if (view.Value == null)
                    continue;

                view.Value.SetLift(SyncLeapLiftViewSystem.Arc(pushed.ArcHeight, pushed.TotalTicks - pushed.RemainingTicks, pushed.TotalTicks));

                if (_liftedPool.Value.Has(entity) == false)
                    _liftedPool.Value.Add(entity);

                _liftedPool.Value.Get(entity).SettleTicks = SettleTicks;
            }

            foreach (int entity in _lifted.Value)
            {
                if (_pushes.Value.Has(entity) && _pushes.Value.Get(entity).ArcHeight > 0f)
                    continue;

                ref Lifted lifted = ref _liftedPool.Value.Get(entity);
                ref View view = ref _views.Value.Get(entity);

                if (view.Value != null)
                    view.Value.SetLift(0f);

                lifted.SettleTicks -= 1;

                if (lifted.SettleTicks <= 0)
                    _liftedPool.Value.Del(entity);
            }
        }
    }
}
