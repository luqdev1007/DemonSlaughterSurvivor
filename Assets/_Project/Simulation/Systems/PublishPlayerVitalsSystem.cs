using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PublishPlayerVitalsSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, Health, MaxHealth>> _filter = default;

        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<UltimateCharge> _charges = default;
        private readonly EcsPoolInject<BerserkMode> _modes = default;

        private readonly EcsCustomInject<IPlayerVitalsSink> _sink = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Health health = ref _healths.Value.Get(entity);
                ref MaxHealth maxHealth = ref _maxHealths.Value.Get(entity);

                bool active = _modes.Value.Has(entity);
                float fraction = active ? ResolveModeFraction(entity) : ResolveChargeFraction(entity);

                _sink.Value.Publish(Mathf.Max(0f, health.Current), maxHealth.Value, fraction, active);
            }
        }

        private float ResolveModeFraction(int entity)
        {
            ref BerserkMode mode = ref _modes.Value.Get(entity);

            if (mode.TotalTicks <= 0)
                return 0f;

            return Mathf.Clamp01((float)mode.RemainingTicks / mode.TotalTicks);
        }

        private float ResolveChargeFraction(int entity)
        {
            if (_charges.Value.Has(entity) == false)
                return 0f;

            ref UltimateCharge charge = ref _charges.Value.Get(entity);

            if (charge.Max <= 0f)
                return 0f;

            return Mathf.Clamp01(charge.Value / charge.Max);
        }
    }
}
