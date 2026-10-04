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
    public sealed class StartHeroicLeapSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<HeroicLeapCooldown>> _cooldowns = default;
        private readonly EcsFilterInject<Inc<Player, Position, Facing>, Exc<Dead, Dashing, SpecialAttack>> _heroes = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;
        private readonly EcsFilterInject<Inc<Weapon, OwnerLink>> _weapons = default;

        private readonly EcsPoolInject<HeroicLeapCooldown> _cooldownPool = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<HeroicLeap> _leaps = default;
        private readonly EcsPoolInject<LeapStarted> _started = default;
        private readonly EcsPoolInject<SpecialAttack> _specialAttacks = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
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
                ref HeroicLeapCooldown cooldown = ref _cooldownPool.Value.Get(entity);

                if (cooldown.RemainingTicks > 0)
                    cooldown.RemainingTicks--;
            }

            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out HeroicLeapConfig config, out int level) == false)
                    continue;

                if (_cooldownPool.Value.Has(hero) == false)
                    _cooldownPool.Value.Add(hero);

                ref HeroicLeapCooldown cooldown = ref _cooldownPool.Value.Get(hero);

                if (cooldown.RemainingTicks > 0)
                    continue;

                if (IsSurrounded(hero, config) == false)
                    continue;

                if (TryFindWeapon(world, hero, out int weapon) == false)
                    continue;

                HeroicLeapLevel entry = config.Level(level);

                Leap(world, hero, weapon, config, entry);

                cooldown.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(entry.CooldownSeconds / _clock.Value.Delta));
            }
        }

        private bool IsSurrounded(int hero, HeroicLeapConfig config)
        {
            Vector3 origin = _positions.Value.Get(hero).Value;
            float squaredRadius = config.SurroundRadius * config.SurroundRadius;

            _grid.Value.Query(origin, config.SurroundRadius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

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

                if (count >= config.SurroundCount)
                    return true;
            }

            return false;
        }

        private bool TryFindWeapon(EcsWorld world, int hero, out int weapon)
        {
            foreach (int entity in _weapons.Value)
            {
                if (_ownerLinks.Value.Get(entity).Owner.Unpack(world, out int owner) == false || owner != hero)
                    continue;

                weapon = entity;

                return true;
            }

            weapon = -1;

            return false;
        }

        private void Leap(EcsWorld world, int hero, int weapon, HeroicLeapConfig config, HeroicLeapLevel entry)
        {
            int entity = world.NewEntity();

            ref HeroicLeap leap = ref _leaps.Value.Add(entity);
            leap.Owner = world.PackEntity(hero);
            leap.Weapon = world.PackEntity(weapon);
            leap.Config = config;
            leap.DamageScale = entry.DamageShare;
            leap.ElapsedTicks = 0;

            _started.Value.Add(entity);

            ref SpecialAttack special = ref _specialAttacks.Value.Add(hero);
            special.Attack = world.PackEntity(entity);
            special.LocksMovement = config.LocksMovement;
        }
    }
}
