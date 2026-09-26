using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Game.Simulation.Tests
{
    public sealed class SpatialGridQueryTests
    {
        private const float ArenaRadius = 10f;
        private const int Seed = 987654321;
        private const int EntityCount = 600;
        private const int QueryCount = 3000;
        private const int AllocationRepeats = 400;
        private const int DetectableBytesPerQuery = 8;

        private EcsWorld _world;
        private EcsPool<Position> _positions;
        private List<int> _entities;
        private List<int> _result;
        private HashSet<int> _expected;

        [SetUp]
        public void SetUp()
        {
            _world = new EcsWorld();
            _positions = _world.GetPool<Position>();
            _entities = new List<int>();
            _result = new List<int>(EntityCount);
            _expected = new HashSet<int>();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Destroy();
            _world = null;
        }

        [TestCase(0.75f)]
        [TestCase(1.0f)]
        [TestCase(1.5f)]
        public void MatchesBruteForceWithoutMovement(float cellSize)
        {
            System.Random random = new System.Random(Seed);
            SpatialGrid grid = new SpatialGrid(_world, ArenaRadius, cellSize);

            SpawnEntities(random, grid, cellSize);

            int mismatches = 0;

            for (int query = 0; query < QueryCount; query++)
            {
                Vector3 center = NextPoint(random, cellSize);
                float radius = Range(random, 0.3f, 4f);
                float slack = Range(random, 0f, 1.5f);

                grid.Query(center, radius, slack, _result);

                mismatches += CountMismatches(center, radius);
            }

            Assert.AreEqual(0, mismatches);
        }

        [TestCase(0.75f)]
        [TestCase(1.0f)]
        [TestCase(1.5f)]
        public void SlackCoversCandidatesMovedAfterRebuild(float cellSize)
        {
            System.Random random = new System.Random(Seed);

            int mismatches = 0;
            int moved = 0;

            for (int round = 0; round < 20; round++)
            {
                EcsWorld world = new EcsWorld();

                try
                {
                    _positions = world.GetPool<Position>();
                    _entities.Clear();

                    SpatialGrid grid = new SpatialGrid(world, ArenaRadius, cellSize);

                    SpawnEntities(random, grid, cellSize, world);

                    float slack = Range(random, 0f, 1.5f);

                    moved += MoveEntities(random, slack * 0.999f);

                    for (int query = 0; query < QueryCount / 20; query++)
                    {
                        Vector3 center = NextPoint(random, cellSize);
                        float radius = Range(random, 0.3f, 4f);

                        grid.Query(center, radius, slack, _result);

                        mismatches += CountMismatches(center, radius);
                    }
                }
                finally
                {
                    world.Destroy();
                }
            }

            Assert.Greater(moved, 0);
            Assert.AreEqual(0, mismatches);
        }

        [Test]
        public void MovementBeyondSlackCanLoseCandidates()
        {
            System.Random random = new System.Random(Seed);
            SpatialGrid grid = new SpatialGrid(_world, ArenaRadius, 1f);

            SpawnEntities(random, grid, 1f);

            MoveEntities(random, 2.5f);

            int missing = 0;

            for (int query = 0; query < QueryCount; query++)
            {
                Vector3 center = NextPoint(random, 1f);
                float radius = Range(random, 0.3f, 1f);

                grid.Query(center, radius, 0f, _result);

                missing += CountMismatches(center, radius);
            }

            Assert.Greater(missing, 0);
        }

        [Test]
        public void ReturnsNoDuplicatesAtTheEdgeWithSeveralRings()
        {
            System.Random random = new System.Random(Seed);
            SpatialGrid grid = new SpatialGrid(_world, ArenaRadius, 1f);

            SpawnEntities(random, grid, 1f);

            Vector3[] centers =
            {
                new Vector3(ArenaRadius + 2f, 0f, ArenaRadius + 2f),
                new Vector3(-ArenaRadius - 2f, 0f, -ArenaRadius - 2f),
                new Vector3(ArenaRadius + 50f, 0f, 0f),
                new Vector3(-ArenaRadius - 1.5f, 0f, ArenaRadius - 0.5f),
                Vector3.zero,
            };

            HashSet<int> seen = new HashSet<int>();

            for (int index = 0; index < centers.Length; index++)
            {
                for (int radiusStep = 1; radiusStep <= 6; radiusStep++)
                {
                    grid.Query(centers[index], radiusStep * 0.9f, 1.5f, _result);

                    seen.Clear();

                    for (int item = 0; item < _result.Count; item++)
                        Assert.IsTrue(seen.Add(_result[item]), $"Entity {_result[item]} returned twice for center {centers[index]}.");

                    Assert.AreEqual(0, CountMismatches(centers[index], radiusStep * 0.9f));
                }
            }
        }

        [Test]
        public void RadiusWiderThanTheGridWalksEveryCellOnce()
        {
            System.Random random = new System.Random(Seed);
            SpatialGrid grid = new SpatialGrid(_world, ArenaRadius, 1f);

            SpawnEntities(random, grid, 1f);

            grid.Query(Vector3.zero, 1000f, 0f, _result);

            Assert.AreEqual(_entities.Count, _result.Count);
            Assert.AreEqual(0, CountMismatches(Vector3.zero, 1000f));

            grid.Query(Vector3.zero, float.MaxValue, float.MaxValue, _result);

            Assert.AreEqual(_entities.Count, _result.Count);
        }

        [Test]
        public void RejectsNegativeAndNaNArguments()
        {
            SpatialGrid grid = new SpatialGrid(_world, ArenaRadius, 1f);

            Assert.Throws<ArgumentOutOfRangeException>(() => grid.Query(Vector3.zero, -0.1f, 0f, _result));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.Query(Vector3.zero, float.NaN, 0f, _result));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.Query(Vector3.zero, 1f, -0.1f, _result));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.Query(Vector3.zero, 1f, float.NaN, _result));
            Assert.Throws<ArgumentNullException>(() => grid.Query(Vector3.zero, 1f, 0f, null));
        }

        [Test]
        public void RepeatedQueriesDoNotAllocate()
        {
            System.Random random = new System.Random(Seed);
            SpatialGrid grid = new SpatialGrid(_world, ArenaRadius, 1f);

            SpawnEntities(random, grid, 1f);

            Vector3[] centers = new Vector3[256];
            float[] radii = new float[256];
            float[] slacks = new float[256];

            for (int index = 0; index < centers.Length; index++)
            {
                centers[index] = NextPoint(random, 1f);
                radii[index] = Range(random, 0.3f, 4f);
                slacks[index] = Range(random, 0f, 1.5f);
            }

            for (int index = 0; index < centers.Length; index++)
                grid.Query(centers[index], radii[index], slacks[index], _result);

            int queries = AllocationRepeats * centers.Length;
            long budget = (long)queries * DetectableBytesPerQuery;

            object[] sink = new object[queries];

            long controlGrowth = MeasureHeapGrowth(out int controlCollections, () =>
            {
                for (int index = 0; index < sink.Length; index++)
                    sink[index] = new object();
            });

            Assert.IsTrue(
                controlGrowth >= budget || controlCollections > 0,
                $"One object per query grew the heap by {controlGrowth} bytes with {controlCollections} collections, " +
                "so this measurement could not detect an allocation per query.");

            long growth = MeasureHeapGrowth(out int collections, () =>
            {
                for (int repeat = 0; repeat < AllocationRepeats; repeat++)
                {
                    for (int index = 0; index < centers.Length; index++)
                        grid.Query(centers[index], radii[index], slacks[index], _result);
                }
            });

            Assert.AreEqual(0, collections);
            Assert.Less(growth, budget);
        }

        private static long MeasureHeapGrowth(out int collections, Action action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            int collectionsBefore = GC.CollectionCount(0);
            long before = Profiler.GetMonoUsedSizeLong();

            action();

            long after = Profiler.GetMonoUsedSizeLong();

            collections = GC.CollectionCount(0) - collectionsBefore;

            return after - before;
        }

        private void SpawnEntities(System.Random random, SpatialGrid grid, float cellSize)
        {
            SpawnEntities(random, grid, cellSize, _world);
        }

        private void SpawnEntities(System.Random random, SpatialGrid grid, float cellSize, EcsWorld world)
        {
            for (int index = 0; index < EntityCount; index++)
            {
                int entity = world.NewEntity();

                Vector3 point = NextPoint(random, cellSize);

                ref Position position = ref _positions.Add(entity);
                position.Value = point;

                grid.Add(entity, point);

                _entities.Add(entity);
            }
        }

        private int MoveEntities(System.Random random, float maxDistance)
        {
            int moved = 0;

            for (int index = 0; index < _entities.Count; index++)
            {
                float angle = Range(random, 0f, 6.2831855f);
                float distance = Range(random, 0f, maxDistance);

                ref Position position = ref _positions.Get(_entities[index]);
                position.Value += new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);

                if (distance > 0f)
                    moved++;
            }

            return moved;
        }

        private int CountMismatches(Vector3 center, float radius)
        {
            _expected.Clear();

            float squaredRadius = radius * radius;

            for (int index = 0; index < _entities.Count; index++)
            {
                int entity = _entities[index];

                ref Position position = ref _positions.Get(entity);

                float deltaX = position.Value.x - center.x;
                float deltaZ = position.Value.z - center.z;

                if (deltaX * deltaX + deltaZ * deltaZ > squaredRadius)
                    continue;

                _expected.Add(entity);
            }

            int mismatches = 0;

            for (int index = 0; index < _result.Count; index++)
            {
                if (_expected.Remove(_result[index]) == false)
                    mismatches++;
            }

            return mismatches + _expected.Count;
        }

        private static Vector3 NextPoint(System.Random random, float cellSize)
        {
            int kind = random.Next(6);

            float extent = ArenaRadius + 2f * cellSize;

            switch (kind)
            {
                case 0:
                    return new Vector3(OnBoundary(random, cellSize, extent), 0f, OnBoundary(random, cellSize, extent));
                case 1:
                    return new Vector3(-Range(random, 0f, extent), 0f, -Range(random, 0f, extent));
                case 2:
                    return new Vector3(Range(random, -0.01f, 0.01f), 0f, Range(random, -0.01f, 0.01f));
                case 3:
                    return new Vector3(Range(random, extent, extent + 6f) * Sign(random), 0f, Range(random, -extent - 6f, extent + 6f));
                default:
                    return new Vector3(Range(random, -extent, extent), 0f, Range(random, -extent, extent));
            }
        }

        private static float OnBoundary(System.Random random, float cellSize, float extent)
        {
            int cells = (int)Math.Ceiling(extent * 2f / cellSize);

            return -extent + random.Next(cells + 1) * cellSize;
        }

        private static float Sign(System.Random random)
        {
            return random.Next(2) == 0 ? -1f : 1f;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
