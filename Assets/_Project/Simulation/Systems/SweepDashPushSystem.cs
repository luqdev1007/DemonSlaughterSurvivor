using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SweepDashPushSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CapacityReserve = 16;
        private const float CoincidenceEpsilonSquared = 1e-6f;
        private const float TieBreakAngleScale = 6.2831855f / 4294967296f;

        private readonly EcsFilterInject<Inc<Player, Dashing, DashStats, Position, PreviousPosition, BodyRadius>> _dashers = default;
        private readonly EcsFilterInject<Inc<Enemy, Position, BodyRadius, Velocity>> _targets = default;

        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<DashStats> _dashStats = default;
        private readonly EcsPoolInject<Pushed> _pushes = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;

        private HashSet<int> _pushedThisDash;

        private bool _wasDashing;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);
            DashAbilityConfig dash = character.Dash;

            if (dash == null)
                throw new InvalidOperationException(
                    $"{nameof(CharacterConfig)} '{character.Id}' has no dash ability assigned, " +
                    $"but {nameof(SweepDashPushSystem)} is registered and reads its push numbers.");

            if (dash.PushSpeed < 0f)
                throw new InvalidOperationException(
                    $"{nameof(DashAbilityConfig)} '{dash.Id}' has a negative {nameof(DashAbilityConfig.PushSpeed)} " +
                    $"of {dash.PushSpeed}. A negative push would pull enemies into the player instead of clearing them.");

            WaveTimelineConfig waves = _level.Value.Waves;

            float queryRadius = character.BodyRadius + WaveTimelineValidator.ResolveMaxBodyRadius(waves);
            float cellSize = _level.Value.SpatialCellSize;
            float pushedStep = dash.PushSpeed / 60f;

            if (queryRadius + pushedStep > cellSize)
                throw new InvalidOperationException(
                    $"{nameof(DashAbilityConfig)} '{dash.Id}' {nameof(DashAbilityConfig.PushSpeed)} {dash.PushSpeed} moves a pushed enemy " +
                    $"{pushedStep} per tick, and the contact damage query radius is {queryRadius}; together they exceed " +
                    $"{nameof(LevelConfig)}.{nameof(LevelConfig.SpatialCellSize)} {cellSize}. " +
                    "The spatial index describes the world at the end of the previous tick, so a candidate that moves further than " +
                    "cellSize minus the query radius can leave the cell it is indexed in and be missed silently: " +
                    $"{nameof(DetectContactDamageSystem)} would stop seeing an enemy right after it was pushed. " +
                    $"Either lower {nameof(DashAbilityConfig.PushSpeed)}, " +
                    $"or raise {nameof(LevelConfig)}.{nameof(LevelConfig.SpatialCellSize)}, " +
                    "or give SpatialGrid a segment query so the index stops being read one tick late.");

            _pushedThisDash = new HashSet<int>(WaveTimelineValidator.ResolveMaxLiveCap(waves) + CapacityReserve);
            _wasDashing = false;
        }

        public void Run(IEcsSystems systems)
        {
            bool dashing = false;

            foreach (int dasher in _dashers.Value)
            {
                dashing = true;

                if (_wasDashing == false)
                    _pushedThisDash.Clear();

                ref Position position = ref _positions.Value.Get(dasher);
                ref PreviousPosition previous = ref _previousPositions.Value.Get(dasher);
                ref BodyRadius dasherRadius = ref _bodyRadii.Value.Get(dasher);
                ref DashStats stats = ref _dashStats.Value.Get(dasher);

                if (stats.PushSpeed <= 0f || stats.PushSeconds <= 0f)
                    continue;

                int ticks = Mathf.Max(1, Mathf.RoundToInt(stats.PushSeconds / _clock.Value.Delta));

                Sweep(dasher, previous.Value, position.Value, dasherRadius.Value, stats.PushSpeed, ticks);
            }

            _wasDashing = dashing;
        }

        private void Sweep(int dasher, Vector3 from, Vector3 to, float dasherRadius, float pushSpeed, int ticks)
        {
            foreach (int target in _targets.Value)
            {
                if (_pushedThisDash.Contains(target))
                    continue;

                if (_pushes.Value.Has(target))
                    continue;

                ref Position targetPosition = ref _positions.Value.Get(target);
                ref BodyRadius targetRadius = ref _bodyRadii.Value.Get(target);

                float contact = dasherRadius + targetRadius.Value;

                if (contact <= 0f)
                    continue;

                Vector3 closest = ClosestPointOnSegment(targetPosition.Value, from, to);

                float deltaX = targetPosition.Value.x - closest.x;
                float deltaZ = targetPosition.Value.z - closest.z;

                float squared = deltaX * deltaX + deltaZ * deltaZ;

                if (squared >= contact * contact)
                    continue;

                Vector3 direction;

                if (squared <= CoincidenceEpsilonSquared)
                {
                    float angle = ResolveTieBreakAngle(dasher, target);

                    direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                }
                else
                {
                    float distance = Mathf.Sqrt(squared);

                    direction = new Vector3(deltaX / distance, 0f, deltaZ / distance);
                }

                ref Pushed pushed = ref _pushes.Value.Add(target);
                pushed.Velocity = direction * pushSpeed;
                pushed.RemainingTicks = ticks;
                pushed.TotalTicks = ticks;

                _pushedThisDash.Add(target);
            }
        }

        private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector3 segment = to - from;
            segment.y = 0f;

            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared <= 1e-8f)
                return new Vector3(from.x, 0f, from.z);

            Vector3 offset = point - from;
            offset.y = 0f;

            float t = Vector3.Dot(offset, segment) / lengthSquared;

            if (t < 0f)
                t = 0f;

            if (t > 1f)
                t = 1f;

            Vector3 closest = from + segment * t;
            closest.y = 0f;

            return closest;
        }

        private static float ResolveTieBreakAngle(int dasher, int target)
        {
            int low = dasher < target ? dasher : target;
            int high = dasher < target ? target : dasher;

            uint hash = unchecked((uint)low * 2654435761u) ^ unchecked((uint)high * 2246822519u);

            hash ^= hash >> 15;
            hash = unchecked(hash * 2246822519u);
            hash ^= hash >> 13;
            hash = unchecked(hash * 3266489917u);
            hash ^= hash >> 16;

            return hash * TieBreakAngleScale;
        }
    }
}
