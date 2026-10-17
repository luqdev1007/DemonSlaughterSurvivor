using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class TriggerCounterStrikeSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<CounterStrikeCooldown>> _cooldowns = default;
        private readonly EcsFilterInject<Inc<Player, Position>, Exc<Dead, Dashing, SpecialAttack, CounterStrikeArmed>> _heroes = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<CounterStrikeCooldown> _cooldownPool = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<CounterStrikeArmed> _armed = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private List<int> _candidates;

        public void Init(IEcsSystems systems)
        {
            _candidates = new List<int>(WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + CandidateCapacityReserve);
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _cooldowns.Value)
            {
                ref CounterStrikeCooldown cooldown = ref _cooldownPool.Value.Get(entity);

                if (cooldown.RemainingTicks > 0)
                    cooldown.RemainingTicks--;
            }

            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out CounterStrikeConfig counter, out int level) == false)
                    continue;

                if (_cooldownPool.Value.Has(hero) == false)
                    _cooldownPool.Value.Add(hero);

                ref CounterStrikeCooldown heroCooldown = ref _cooldownPool.Value.Get(hero);

                if (heroCooldown.RemainingTicks > 0)
                    continue;

                if (IsSurrounded(hero, counter) == false)
                    continue;

                float delta = _clock.Value.Delta;

                ref CounterStrikeArmed armed = ref _armed.Value.Add(hero);
                armed.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(counter.ArmedSeconds / delta));
                armed.Level = level;

                heroCooldown.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(counter.Level(level).CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero) / delta));
            }
        }

        private bool IsSurrounded(int hero, CounterStrikeConfig config)
        {
            Vector3 origin = _positions.Value.Get(hero).Value;
            float squaredRadius = config.TriggerRadius * config.TriggerRadius;

            _grid.Value.Query(origin, config.TriggerRadius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            int count = 0;

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false || _dead.Value.Has(candidate))
                    continue;

                Vector3 position = _positions.Value.Get(candidate).Value;
                float x = position.x - origin.x;
                float z = position.z - origin.z;

                if (x * x + z * z > squaredRadius)
                    continue;

                count++;

                if (count >= config.MinEnemies)
                    return true;
            }

            return false;
        }
    }
}
