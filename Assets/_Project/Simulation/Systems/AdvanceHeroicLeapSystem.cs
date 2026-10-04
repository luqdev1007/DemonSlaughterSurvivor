using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceHeroicLeapSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<HeroicLeap>> _leaps = default;

        private readonly EcsPoolInject<HeroicLeap> _leapPool = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Dashing> _dashing = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<WeaponDamage> _weaponDamages = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<Pushed> _pushes = default;

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
            EcsWorld world = _world.Value;

            foreach (int entity in _leaps.Value)
            {
                ref HeroicLeap leap = ref _leapPool.Value.Get(entity);

                if (leap.Owner.Unpack(world, out int owner) == false || _dead.Value.Has(owner) || _dashing.Value.Has(owner))
                {
                    world.DelEntity(entity);

                    continue;
                }

                if (leap.ElapsedTicks == leap.Config.LandingTick)
                    Land(world, ref leap, owner);

                leap.ElapsedTicks++;

                if (leap.ElapsedTicks >= leap.Config.TotalTicks)
                    world.DelEntity(entity);
            }
        }

        private void Land(EcsWorld world, ref HeroicLeap leap, int owner)
        {
            HeroicLeapConfig config = leap.Config;
            Vector3 origin = _positions.Value.Get(owner).Value;
            Vector3 facing = _facings.Value.Get(owner).Value;
            Vector3 forward = PushFromPoint.Direction(Vector3.zero, facing, Vector3.forward);

            float damage = 0f;

            if (leap.Weapon.Unpack(world, out int weapon) && _weaponDamages.Value.Has(weapon))
                damage = _weaponDamages.Value.Get(weapon).Value * leap.DamageScale;

            float cosHalfAngle = Mathf.Cos(config.SectorHalfAngleDegrees * Mathf.Deg2Rad);
            float strikeReach = config.SectorReach + _motionBounds.Value.MaxEnemyBodyRadius;
            float radius = Math.Max(strikeReach, config.PushRadius);
            int pushTicks = Mathf.Max(1, Mathf.RoundToInt(config.PushSeconds / _clock.Value.Delta));
            bool pushes = config.PushSpeed > 0f && config.PushSeconds > 0f;

            _grid.Value.Query(origin, radius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false || _dead.Value.Has(candidate))
                    continue;

                Vector3 position = _positions.Value.Get(candidate).Value;
                float x = position.x - origin.x;
                float z = position.z - origin.z;
                float distance = Mathf.Sqrt(x * x + z * z);
                Vector3 direction = PushFromPoint.Direction(origin, position, facing);

                if (damage > 0f && _bodyRadii.Value.Has(candidate) && distance <= config.SectorReach + _bodyRadii.Value.Get(candidate).Value
                    && Vector3.Dot(direction, forward) >= cosHalfAngle - 1e-6f)
                {
                    int eventEntity = world.NewEntity();

                    ref DamageEvent damageEvent = ref _damageEvents.Value.Add(eventEntity);
                    damageEvent.Target = world.PackEntity(candidate);
                    damageEvent.Source = world.PackEntity(owner);
                    damageEvent.SourcePosition = origin;
                    damageEvent.Amount = damage;
                    damageEvent.Kind = DamageKind.Perk;
                }

                if (pushes && distance <= config.PushRadius && _velocities.Value.Has(candidate))
                    PushFromPoint.Apply(_pushes.Value, candidate, direction, config.PushSpeed, pushTicks);
            }
        }
    }
}
