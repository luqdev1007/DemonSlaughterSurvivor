using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SeparateNeighborsSystem : IEcsRunSystem
    {
        private const int NeighborCapacity = 64;
        private const float CoincidenceEpsilonSquared = 1e-6f;
        private const float MinimumPushLength = 1e-5f;
        private const float TieBreakAngleScale = 6.2831855f / 4294967296f;

        private readonly EcsFilterInject<Inc<Enemy, Position, MoveIntent, Separation>> _filter = default;

        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<Separation> _separations = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;

        private readonly List<int> _neighbors = new List<int>(NeighborCapacity);

        public void Run(IEcsSystems systems)
        {
            SpatialGrid grid = _grid.Value;

            foreach (int entity in _filter.Value)
            {
                ref Separation separation = ref _separations.Value.Get(entity);

                if (separation.Radius <= 0f)
                    continue;

                if (separation.Strength <= 0f)
                    continue;

                ref Position position = ref _positions.Value.Get(entity);

                grid.Query(position.Value, separation.Radius, _neighbors);

                float pushX = 0f;
                float pushZ = 0f;

                for (int index = 0; index < _neighbors.Count; index++)
                {
                    int neighbor = _neighbors[index];

                    if (neighbor == entity)
                        continue;

                    if (_enemies.Value.Has(neighbor) == false)
                        continue;

                    ref Position neighborPosition = ref _positions.Value.Get(neighbor);

                    float deltaX = position.Value.x - neighborPosition.Value.x;
                    float deltaZ = position.Value.z - neighborPosition.Value.z;

                    float squared = deltaX * deltaX + deltaZ * deltaZ;

                    if (squared <= CoincidenceEpsilonSquared)
                    {
                        float angle = ResolveTieBreakAngle(entity, neighbor);

                        float sign = entity < neighbor ? 1f : -1f;

                        pushX += Mathf.Cos(angle) * sign;
                        pushZ += Mathf.Sin(angle) * sign;

                        continue;
                    }

                    float distance = Mathf.Sqrt(squared);

                    float weight = 1f - distance / separation.Radius;

                    if (weight <= 0f)
                        continue;

                    float scale = weight / distance;

                    pushX += deltaX * scale;
                    pushZ += deltaZ * scale;
                }

                float pushLength = Mathf.Sqrt(pushX * pushX + pushZ * pushZ);

                if (pushLength <= MinimumPushLength)
                    continue;

                Vector3 push = new Vector3(pushX / pushLength, 0f, pushZ / pushLength);

                ref MoveIntent intent = ref _intents.Value.Get(entity);

                intent.Value = Blend(intent.Value, push, separation.Strength);
            }
        }

        private Vector3 Blend(Vector3 intent, Vector3 push, float strength)
        {
            throw new NotImplementedException();
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
