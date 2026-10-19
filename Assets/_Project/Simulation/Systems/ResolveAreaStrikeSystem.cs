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
    public sealed class ResolveAreaStrikeSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;
        private const float FullCircleDegrees = 180f;
        private const float PushSpeedTolerance = 1e-4f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<AreaStrike>> _strikes = default;

        private readonly EcsPoolInject<AreaStrike> _strikePool = default;
        private readonly EcsPoolInject<AreaStrikeEvent> _strikeEvents = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<Pushed> _pushes = default;
        private readonly EcsPoolInject<NotPushable> _notPushables = default;

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

            foreach (int entity in _strikes.Value)
            {
                ref AreaStrike strike = ref _strikePool.Value.Get(entity);

                if (strike.Owner.Unpack(world, out int owner) == false || _dead.Value.Has(owner)
                    || (strike.Anchored && strike.Anchor.Unpack(world, out _) == false))
                {
                    world.DelEntity(entity);

                    continue;
                }

                if (strike.DelayTicks > 0)
                {
                    strike.DelayTicks--;

                    continue;
                }

                Resolve(world, in strike, owner);

                world.DelEntity(entity);
            }
        }

        private void Resolve(EcsWorld world, in AreaStrike strike, int owner)
        {
            if (strike.PushSpeed > _motionBounds.Value.PushSpeed + PushSpeedTolerance)
                throw new InvalidOperationException(
                    $"An area strike pushes at {strike.PushSpeed} m/s, faster than the {_motionBounds.Value.PushSpeed} m/s " +
                    $"the contact damage query budgets for. Report the push speed through {nameof(PerkBehaviourConfig)}." +
                    $"{nameof(PerkBehaviourConfig.MaxPushSpeed)}, or pushed enemies would slip out of the spatial index query.");

            Vector3 origin = strike.Origin;
            Vector3 facing = strike.Forward;

            if (strike.FollowOwner)
            {
                origin = _positions.Value.Get(owner).Value;
                facing = _facings.Value.Get(owner).Value;
            }

            Vector3 forward = PushFromPoint.Direction(Vector3.zero, facing, Vector3.forward);
            bool fullCircle = strike.HalfAngleDegrees >= FullCircleDegrees;
            float cosHalfAngle = Mathf.Cos(strike.HalfAngleDegrees * Mathf.Deg2Rad);
            float radius = strike.Reach + _motionBounds.Value.MaxEnemyBodyRadius;
            int pushTicks = Mathf.Max(1, Mathf.RoundToInt(strike.PushSeconds / _clock.Value.Delta));
            bool pushes = strike.PushSpeed > 0f && strike.PushSeconds > 0f;
            int hits = 0;

            _grid.Value.Query(origin, radius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false || _dead.Value.Has(candidate) || _bodyRadii.Value.Has(candidate) == false)
                    continue;

                Vector3 position = _positions.Value.Get(candidate).Value;
                float x = position.x - origin.x;
                float z = position.z - origin.z;
                float distance = Mathf.Sqrt(x * x + z * z);

                if (distance > strike.Reach + _bodyRadii.Value.Get(candidate).Value)
                    continue;

                Vector3 direction = PushFromPoint.Direction(origin, position, forward);

                if (fullCircle == false && Vector3.Dot(direction, forward) < cosHalfAngle - 1e-6f)
                    continue;

                hits++;

                if (strike.Damage > 0f)
                {
                    ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
                    damageEvent.Target = world.PackEntity(candidate);
                    damageEvent.Source = world.PackEntity(owner);
                    damageEvent.SourcePosition = origin;
                    damageEvent.Amount = strike.Damage;
                    damageEvent.Kind = strike.Kind;
                    damageEvent.IsSlash = strike.IsSlash;
                    damageEvent.IsMagic = strike.IsMagic;
                    damageEvent.Element = strike.Element;
                    damageEvent.SourcePerk = strike.SourcePerk;
                }

                if (pushes && _velocities.Value.Has(candidate) && _notPushables.Value.Has(candidate) == false)
                {
                    PushFromPoint.Apply(_pushes.Value, candidate, direction, strike.PushSpeed, pushTicks);
                    _pushes.Value.Get(candidate).ArcHeight = strike.PushArcHeight;
                }
            }

            ref AreaStrikeEvent strikeEvent = ref _strikeEvents.Value.Add(world.NewEntity());
            strikeEvent.Owner = world.PackEntity(owner);
            strikeEvent.Origin = origin;
            strikeEvent.Forward = forward;
            strikeEvent.Reach = strike.Reach;
            strikeEvent.HalfAngleDegrees = strike.HalfAngleDegrees;
            strikeEvent.Hits = hits;
        }
    }
}
