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
        private const float SpreadUnitScale = 1f / 2147483648f;

        private readonly EcsFilterInject<Inc<Player, Dashing, DashStats, Position, PreviousPosition, BodyRadius>> _dashers = default;
        private readonly EcsFilterInject<Inc<Enemy, Position, BodyRadius, Velocity>, Exc<NotPushable>> _targets = default;

        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<DashStats> _dashStats = default;
        private readonly EcsPoolInject<Dashing> _dashes = default;
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
                return;

            if (dash.PushSpeed < 0f)
                throw new InvalidOperationException(
                    $"{nameof(DashAbilityConfig)} '{dash.Id}' has a negative {nameof(DashAbilityConfig.PushSpeed)} " +
                    $"of {dash.PushSpeed}. A negative push would pull enemies into the player instead of clearing them.");

            if (float.IsNaN(dash.PushArcHeight) || dash.PushArcHeight < 0f)
                throw new InvalidOperationException(
                    $"{nameof(DashAbilityConfig)} '{dash.Id}' has a {nameof(DashAbilityConfig.PushArcHeight)} " +
                    $"of {dash.PushArcHeight}. Use 0 for a flat push; a negative arc would sink enemies into the floor.");

            if (float.IsNaN(dash.PushReach) || dash.PushReach < 0f)
                throw new InvalidOperationException(
                    $"{nameof(DashAbilityConfig)} '{dash.Id}' has a {nameof(DashAbilityConfig.PushReach)} of {dash.PushReach}. " +
                    "Use 0 to push only the enemies the body touches.");

            if (float.IsNaN(dash.PushEdgeScale) || dash.PushEdgeScale < 0f || dash.PushEdgeScale > 1f)
                throw new InvalidOperationException(
                    $"{nameof(DashAbilityConfig)} '{dash.Id}' has a {nameof(DashAbilityConfig.PushEdgeScale)} of {dash.PushEdgeScale}. " +
                    "It is the share of the push left at the edge of the reach and must be in [0, 1].");

            if (float.IsNaN(dash.PushForwardTiltDegrees) || dash.PushForwardTiltDegrees < 0f || dash.PushForwardTiltDegrees > 90f)
                throw new InvalidOperationException(
                    $"{nameof(DashAbilityConfig)} '{dash.Id}' has a {nameof(DashAbilityConfig.PushForwardTiltDegrees)} of {dash.PushForwardTiltDegrees}. " +
                    "It tilts the sideways push towards the dash direction and must be in [0, 90]: 0 is straight aside, 90 is straight ahead.");

            if (float.IsNaN(dash.PushSpreadDegrees) || dash.PushSpreadDegrees < 0f)
                throw new InvalidOperationException(
                    $"{nameof(DashAbilityConfig)} '{dash.Id}' has a {nameof(DashAbilityConfig.PushSpreadDegrees)} of {dash.PushSpreadDegrees}. " +
                    "Use 0 for every enemy on one side to fly at the same angle.");

            if (dash.PushForwardTiltDegrees + dash.PushSpreadDegrees >= 90f)
                throw new InvalidOperationException(
                    $"{nameof(DashAbilityConfig)} '{dash.Id}' has a forward tilt of {dash.PushForwardTiltDegrees} and a spread of {dash.PushSpreadDegrees}. " +
                    "Together they must stay below 90 degrees, otherwise an enemy beside the path could be thrown across it to the other side.");

            WaveTimelineConfig waves = _level.Value.Waves;

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
                ref Dashing dash = ref _dashes.Value.Get(dasher);

                if (stats.PushSpeed <= 0f || stats.PushSeconds <= 0f)
                    continue;

                int ticks = Mathf.Max(1, Mathf.RoundToInt(stats.PushSeconds / _clock.Value.Delta));

                Sweep(dasher, previous.Value, position.Value, dash.Direction, dasherRadius.Value, ticks, in stats);
            }

            _wasDashing = dashing;
        }

        private void Sweep(int dasher, Vector3 from, Vector3 to, Vector3 dashDirection, float dasherRadius, int ticks, in DashStats stats)
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

                float reach = contact + stats.PushReach;

                if (squared >= reach * reach)
                    continue;

                Vector3 direction = ResolvePushDirection(
                    dashDirection,
                    to - from,
                    targetPosition.Value - from,
                    ResolvePairHash(dasher, target),
                    stats.PushForwardTiltDegrees,
                    stats.PushSpreadDegrees,
                    out float lateral);

                float scale = ResolvePushScale(lateral, contact, stats.PushReach, stats.PushEdgeScale);

                ref Pushed pushed = ref _pushes.Value.Add(target);
                pushed.Velocity = direction * (stats.PushSpeed * scale);
                pushed.RemainingTicks = ticks;
                pushed.TotalTicks = ticks;
                pushed.ArcHeight = stats.PushArcHeight * scale;

                _pushedThisDash.Add(target);
            }
        }

        public static float ResolvePushScale(float distance, float contact, float reach, float edgeScale)
        {
            if (distance <= contact || reach <= 0f)
                return 1f;

            return Mathf.Lerp(1f, edgeScale, (distance - contact) / reach);
        }

        public static Vector3 ResolvePushDirection(
            Vector3 dashDirection,
            Vector3 step,
            Vector3 offset,
            uint hash,
            float forwardTiltDegrees,
            float spreadDegrees,
            out float lateral)
        {
            Vector3 forward = Flatten(dashDirection);

            if (forward.sqrMagnitude <= CoincidenceEpsilonSquared)
                forward = Flatten(step);

            if (forward.sqrMagnitude <= CoincidenceEpsilonSquared)
                forward = Vector3.forward;

            forward.Normalize();

            Vector3 right = new Vector3(forward.z, 0f, -forward.x);

            float signedLateral = offset.x * right.x + offset.z * right.z;

            lateral = Mathf.Abs(signedLateral);

            float side;

            if (signedLateral * signedLateral > CoincidenceEpsilonSquared)
                side = signedLateral > 0f ? 1f : -1f;
            else
                side = (hash & 0x80000000u) != 0u ? 1f : -1f;

            float unit = (hash & 0x7FFFFFFFu) * SpreadUnitScale;
            float angle = (90f - forwardTiltDegrees + spreadDegrees * (2f * unit - 1f)) * Mathf.Deg2Rad;

            return forward * Mathf.Cos(angle) + right * (side * Mathf.Sin(angle));
        }

        public static uint ResolvePairHash(int dasher, int target)
        {
            int low = dasher < target ? dasher : target;
            int high = dasher < target ? target : dasher;

            uint hash = unchecked((uint)low * 2654435761u) ^ unchecked((uint)high * 2246822519u);

            hash ^= hash >> 15;
            hash = unchecked(hash * 2246822519u);
            hash ^= hash >> 13;
            hash = unchecked(hash * 3266489917u);
            hash ^= hash >> 16;

            return hash;
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
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
    }
}
