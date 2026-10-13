using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class SeparationSnapshotTests
    {
        private const float ArenaRadius = 45f;
        private const float CellSize = 1f;
        private const float TieBreakAngleScale = 6.2831855f / 4294967296f;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SpatialGrid _grid;
        private Vector3[] _initial;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [TestCase(987654321, 600, 10f, 300)]
        [TestCase(12345, 600, 4f, 300)]
        [TestCase(777, 150, 25f, 10)]
        public void StaggeredSeparationMatchesTheReferenceAlgorithmBitForBit(int seed, int count, float side, int minimumChanged)
        {
            Build();

            System.Random random = new System.Random(seed);
            List<int> enemies = new List<int>();

            for (int index = 0; index < count; index++)
            {
                Vector3 position = index % 37 == 0 && enemies.Count > 0
                    ? _world.GetPool<Position>().Get(enemies[random.Next(enemies.Count)]).Value
                    : new Vector3(Next(random, side), 0f, Next(random, side));

                enemies.Add(SpawnEnemy(position, Next(random, 1f), Next(random, 1f), index % 11 == 0 ? 0f : 0.5f));
            }

            int hero = _world.NewEntity();
            _world.GetPool<Player>().Add(hero);
            _world.GetPool<Position>().Add(hero).Value = Vector3.zero;

            foreach (int entity in _world.Filter<Position>().End())
                _grid.Add(entity, _world.GetPool<Position>().Get(entity).Value);

            Vector3[] expected = Reference(enemies);

            _systems.Run();

            int firstTickMismatches = 0;
            int firstTickWaiting = 0;

            for (int index = 0; index < enemies.Count; index++)
            {
                Vector3 actual = _world.GetPool<MoveIntent>().Get(enemies[index]).Value;

                if ((enemies[index] & 1) == 1)
                {
                    if (BitEquals(actual, expected[index]) == false)
                        firstTickMismatches++;
                }
                else if (BitEquals(actual, _initial[index]) == false)
                {
                    firstTickWaiting++;
                }
            }

            Assert.AreEqual(0, firstTickMismatches, "odd ids recompute on the first tick and must match the live-position algorithm bit for bit");
            Assert.AreEqual(0, firstTickWaiting, "even ids have no push yet on the first tick and must keep their intent");

            ResetIntents(enemies);
            _systems.Run();

            int mismatches = 0;
            int changed = 0;

            for (int index = 0; index < enemies.Count; index++)
            {
                if (BitEquals(_world.GetPool<MoveIntent>().Get(enemies[index]).Value, expected[index]) == false)
                    mismatches++;

                if (BitEquals(expected[index], _initial[index]) == false)
                    changed++;
            }

            Assert.AreEqual(0, mismatches, "on the second tick even ids recompute and odd ids reuse their push; both must match bit for bit");
            Assert.GreaterOrEqual(changed, minimumChanged, "too few intents changed for the comparison to mean anything");
        }

        private void ResetIntents(List<int> enemies)
        {
            for (int index = 0; index < enemies.Count; index++)
                _world.GetPool<MoveIntent>().Get(enemies[index]).Value = _initial[index];
        }

        private void Build()
        {
            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            _world = new EcsWorld();
            _grid = new SpatialGrid(_world, ArenaRadius, CellSize);

            _systems = new EcsSystems(_world);
            _systems.Add(new SeparateNeighborsSystem());
            _systems.Inject(_grid, level);
            _systems.Init();
        }

        private int SpawnEnemy(Vector3 position, float intentX, float intentZ, float strength)
        {
            int enemy = _world.NewEntity();

            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<MoveIntent>().Add(enemy).Value = new Vector3(intentX, 0f, intentZ);

            ref Separation separation = ref _world.GetPool<Separation>().Add(enemy);
            separation.Radius = 1f;
            separation.Strength = strength;

            return enemy;
        }

        private Vector3[] Reference(List<int> enemies)
        {
            EcsPool<Position> positions = _world.GetPool<Position>();
            EcsPool<MoveIntent> intents = _world.GetPool<MoveIntent>();
            EcsPool<Separation> separations = _world.GetPool<Separation>();
            EcsPool<Enemy> enemyPool = _world.GetPool<Enemy>();

            Vector3[] result = new Vector3[enemies.Count];
            _initial = new Vector3[enemies.Count];
            List<int> neighbors = new List<int>();

            for (int slot = 0; slot < enemies.Count; slot++)
            {
                int entity = enemies[slot];
                Vector3 intent = intents.Get(entity).Value;

                _initial[slot] = intent;
                result[slot] = intent;

                Separation separation = separations.Get(entity);

                if (separation.Radius <= 0f || separation.Strength <= 0f)
                    continue;

                Vector3 position = positions.Get(entity).Value;

                _grid.Query(position, separation.Radius, 0f, neighbors);

                float pushX = 0f;
                float pushZ = 0f;

                for (int index = 0; index < neighbors.Count; index++)
                {
                    int neighbor = neighbors[index];

                    if (neighbor == entity || enemyPool.Has(neighbor) == false)
                        continue;

                    Vector3 other = positions.Get(neighbor).Value;

                    float deltaX = position.x - other.x;
                    float deltaZ = position.z - other.z;
                    float squared = deltaX * deltaX + deltaZ * deltaZ;

                    if (squared <= 1e-6f)
                    {
                        float angle = TieBreak(entity, neighbor);
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

                if (pushLength <= 1e-5f)
                    continue;

                float clamp = pushLength > 1f ? 1f / pushLength : 1f;
                Vector3 push = new Vector3(pushX * clamp, 0f, pushZ * clamp);
                Vector3 blended = intent + push * separation.Strength;

                result[slot] = blended.normalized * intent.magnitude;
            }

            return result;
        }

        private static float TieBreak(int entity, int neighbor)
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

        private static float Next(System.Random random, float halfExtent)
        {
            return (float)(random.NextDouble() * 2.0 - 1.0) * halfExtent;
        }

        private static bool BitEquals(Vector3 left, Vector3 right)
        {
            return BitConverter.SingleToInt32Bits(left.x) == BitConverter.SingleToInt32Bits(right.x)
                && BitConverter.SingleToInt32Bits(left.y) == BitConverter.SingleToInt32Bits(right.y)
                && BitConverter.SingleToInt32Bits(left.z) == BitConverter.SingleToInt32Bits(right.z);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }
    }
}
