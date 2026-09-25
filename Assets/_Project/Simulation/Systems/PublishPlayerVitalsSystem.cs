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

        private readonly EcsCustomInject<IPlayerVitalsSink> _sink = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Health health = ref _healths.Value.Get(entity);
                ref MaxHealth maxHealth = ref _maxHealths.Value.Get(entity);

                _sink.Value.Publish(Mathf.Max(0f, health.Current), maxHealth.Value);
            }
        }
    }
}
