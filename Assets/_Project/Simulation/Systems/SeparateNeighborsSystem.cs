using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SeparateNeighborsSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int NonEnemyCapacityReserve = 16;
        private const float CoincidenceEpsilonSquared = 1e-6f;
        private const float MinimumPushLength = 1e-5f;
        private const float TieBreakAngleScale = 6.2831855f / 4294967296f;

        private readonly EcsFilterInject<Inc<Enemy, Position, MoveIntent, Separation>> _filter = default;

        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<Separation> _separations = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private List<GridHit> _neighbors;
        private int _tick;

        public void Init(IEcsSystems systems)
        {
            int capacity = WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + NonEnemyCapacityReserve;

            _neighbors = new List<GridHit>(capacity);
        }

        public void Run(IEcsSystems systems)
        {
            SpatialGrid grid = _grid.Value;

            _tick++;

            int parity = _tick & 1;

            foreach (int entity in _filter.Value)
            {
                ref Separation separation = ref _separations.Value.Get(entity);

                if (separation.Radius <= 0f)
                    continue;

                if (separation.Strength <= 0f)
                    continue;

                if ((entity & 1) == parity)
                    separation.Push = ResolvePush(grid, entity, separation.Radius);

                if (separation.Push == Vector3.zero)
                    continue;

                ref MoveIntent intent = ref _intents.Value.Get(entity);

                intent.Value = Blend(intent.Value, separation.Push, separation.Strength);
            }
        }

        private Vector3 ResolvePush(SpatialGrid grid, int entity, float radius)
        {
            ref Position position = ref _positions.Value.Get(entity);

            grid.QuerySnapshot(position.Value, radius, _neighbors);

            float pushX = 0f;
            float pushZ = 0f;

            for (int index = 0; index < _neighbors.Count; index++)
            {
                GridHit hit = _neighbors[index];
                int neighbor = hit.Entity;

                if (neighbor == entity)
                    continue;

                if (_enemies.Value.Has(neighbor) == false)
                    continue;

                float deltaX = -hit.DeltaX;
                float deltaZ = -hit.DeltaZ;

                float squared = hit.SquaredDistance;

                if (squared <= CoincidenceEpsilonSquared)
                {
                    float angle = ResolveTieBreakAngle(entity, neighbor);

                    float sign = entity < neighbor ? 1f : -1f;

                    pushX += Mathf.Cos(angle) * sign;
                    pushZ += Mathf.Sin(angle) * sign;

                    continue;
                }

                float distance = Mathf.Sqrt(squared);

                float weight = 1f - distance / radius;

                if (weight <= 0f)
                    continue;

                float scale = weight / distance;

                pushX += deltaX * scale;
                pushZ += deltaZ * scale;
            }

            float pushLength = Mathf.Sqrt(pushX * pushX + pushZ * pushZ);

            if (pushLength <= MinimumPushLength)
                return Vector3.zero;

            float clamp = pushLength > 1f ? 1f / pushLength : 1f;

            return new Vector3(pushX * clamp, 0f, pushZ * clamp);
        }

        private Vector3 Blend(Vector3 intent, Vector3 push, float strength)
        {
            Vector3 blended = intent + push * strength;

            return blended.normalized * intent.magnitude;
        }

        private static float ResolveTieBreakAngle(int entity, int neighbor)
        {
            int low = entity < neighbor ? entity : neighbor;
            int high = entity < neighbor ? neighbor : entity;

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
