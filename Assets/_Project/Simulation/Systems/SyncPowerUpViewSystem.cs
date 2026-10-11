using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SyncPowerUpViewSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<UltimateCharge, View>> _filter = default;

        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<PoweringUp> _poweringUps = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value == null)
                    continue;

                if (_poweringUps.Value.Has(entity) == false)
                {
                    view.Value.SetPowerUp(false, 0f);

                    continue;
                }

                ref PoweringUp powerUp = ref _poweringUps.Value.Get(entity);

                float progress = (float)(powerUp.TotalTicks - powerUp.RemainingTicks + 1) / powerUp.TotalTicks;

                view.Value.SetPowerUp(true, progress);
            }
        }
    }
}
