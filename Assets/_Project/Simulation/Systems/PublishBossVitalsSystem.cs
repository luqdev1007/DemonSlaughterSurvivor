using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PublishBossVitalsSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Boss, Health, MaxHealth>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;

        private readonly EcsCustomInject<IBossVitalsSink> _sink = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Health health = ref _healths.Value.Get(entity);
                ref MaxHealth maxHealth = ref _maxHealths.Value.Get(entity);

                _sink.Value.Publish(true, _bosses.Value.Get(entity).Config.DisplayName, Mathf.Max(0f, health.Current), maxHealth.Value);

                return;
            }

            _sink.Value.Publish(false, null, 0f, 0f);
        }
    }
}
