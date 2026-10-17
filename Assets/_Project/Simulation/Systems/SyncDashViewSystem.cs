using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SyncDashViewSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<DashStats, View>> _filter = default;

        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<Dashing> _dashes = default;
        private readonly EcsPoolInject<DashCharge> _charges = default;
        private readonly EcsPoolInject<DashStats> _stats = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value == null)
                    continue;

                view.Value.SetDashing(_dashes.Value.Has(entity));

                if (view.Value is not IDashChargeView chargeView)
                    continue;

                ref DashStats stats = ref _stats.Value.Get(entity);
                bool charging = _charges.Value.Has(entity);
                float strength = charging ? DashChargeRules.Strength(in stats, _charges.Value.Get(entity).Ticks, _clock.Value.Delta) : 0f;

                float dashStrength = _dashes.Value.Has(entity) ? _dashes.Value.Get(entity).Strength : 0f;

                chargeView.SetDashCharge(charging && strength > 0f, strength, stats.ChargeRunAnimation, dashStrength);
            }
        }
    }
}
