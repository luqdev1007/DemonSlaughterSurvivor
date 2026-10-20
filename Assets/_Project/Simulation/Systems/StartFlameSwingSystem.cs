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
    public sealed class StartFlameSwingSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<FlameSwingCooldown>> _cooldowns = default;
        private readonly EcsFilterInject<Inc<Player, Position, Facing>, Exc<Dead, Dashing, SpecialAttack, PoweringUp>> _heroes = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<FlameSwingCooldown> _cooldownPool = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<FlameSwing> _swings = default;
        private readonly EcsPoolInject<FlameSwingStarted> _started = default;
        private readonly EcsPoolInject<SpecialAttack> _specialAttacks = default;
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
                ref FlameSwingCooldown cooldown = ref _cooldownPool.Value.Get(entity);

                if (cooldown.RemainingTicks > 0)
                    cooldown.RemainingTicks--;
            }

            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out FlameSwingConfig config, out int level) == false)
                    continue;

                if (_cooldownPool.Value.Has(hero) == false)
                    _cooldownPool.Value.Add(hero);

                ref FlameSwingCooldown cooldown = ref _cooldownPool.Value.Get(hero);

                if (cooldown.RemainingTicks > 0)
                    continue;

                if (CountEnemiesAhead(hero, config) < config.MinEnemies)
                    continue;

                Start(world, hero, config, level);

                float seconds = config.Level(level).CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero);

                cooldown.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(seconds / _clock.Value.Delta));
            }
        }

        private int CountEnemiesAhead(int hero, FlameSwingConfig config)
        {
            Vector3 origin = _positions.Value.Get(hero).Value;
            Vector3 facing = _facings.Value.Get(hero).Value;
            facing.y = 0f;

            if (facing.sqrMagnitude <= Mathf.Epsilon)
                return 0;

            facing.Normalize();

            float cosHalfAngle = Mathf.Cos(config.TriggerHalfAngleDegrees * Mathf.Deg2Rad);
            float slack = _motionBounds.Value.CandidateSlack(_clock.Value.Delta);

            _grid.Value.Query(origin, config.TriggerRange + _motionBounds.Value.MaxEnemyBodyRadius, slack, _candidates);

            int count = 0;

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false || _dead.Value.Has(candidate) || _bodyRadii.Value.Has(candidate) == false)
                    continue;

                Vector3 offset = _positions.Value.Get(candidate).Value - origin;
                offset.y = 0f;

                float distance = offset.magnitude;

                if (distance > config.TriggerRange + _bodyRadii.Value.Get(candidate).Value)
                    continue;

                if (distance > Mathf.Epsilon && Vector3.Dot(offset / distance, facing) < cosHalfAngle)
                    continue;

                count++;
            }

            return count;
        }

        private void Start(EcsWorld world, int hero, FlameSwingConfig config, int level)
        {
            int entity = world.NewEntity();

            ref FlameSwing swing = ref _swings.Value.Add(entity);
            swing.Owner = world.PackEntity(hero);
            swing.Config = config;
            swing.Level = level;
            swing.ElapsedTicks = 0;

            _started.Value.Add(entity);

            ref SpecialAttack special = ref _specialAttacks.Value.Add(hero);
            special.Attack = world.PackEntity(entity);
            special.LocksMovement = config.LocksMovement;
        }
    }
}
