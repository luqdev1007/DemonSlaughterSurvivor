using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PublishBossVitalsSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Boss, Health, MaxHealth, Position>, Exc<Dead>> _filter = default;
        private readonly EcsFilterInject<Inc<Player, Position>> _heroes = default;

        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<Position> _positions = default;

        private readonly EcsCustomInject<IBossVitalsSink> _sink = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Health health = ref _healths.Value.Get(entity);
                ref MaxHealth maxHealth = ref _maxHealths.Value.Get(entity);

                Vector3 position = _positions.Value.Get(entity).Value;

                _sink.Value.Publish(true, _bosses.Value.Get(entity).Config.DisplayName, Mathf.Max(0f, health.Current), maxHealth.Value, position, DistanceToHero(position));

                return;
            }

            _sink.Value.Publish(false, null, 0f, 0f, Vector3.zero, 0f);
        }

        private float DistanceToHero(Vector3 position)
        {
            foreach (int hero in _heroes.Value)
            {
                Vector3 offset = _positions.Value.Get(hero).Value - position;
                offset.y = 0f;

                return offset.magnitude;
            }

            return 0f;
        }
    }
}
