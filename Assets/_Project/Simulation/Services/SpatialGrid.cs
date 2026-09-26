using Game.Simulation.Components;
using Leopotam.EcsLite;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Services
{
    public sealed class SpatialGrid
    {
        private const int MarginCells = 2;

        private readonly EcsPool<Position> _positions;

        private readonly float _cellSize;
        private readonly float _minX;
        private readonly float _minZ;
        private readonly int _columns;
        private readonly int _rows;
        private readonly List<int>[] _cells;

        private int _count;

        public SpatialGrid(EcsWorld world, float arenaRadius, float cellSize)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            if (arenaRadius <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(arenaRadius),
                    arenaRadius,
                    "Spatial grid arena radius must be greater than zero.");

            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(cellSize),
                    cellSize,
                    "Spatial grid cell size must be greater than zero.");

            _positions = world.GetPool<Position>();

            _cellSize = cellSize;

            float halfExtent = arenaRadius + cellSize * MarginCells;

            _minX = -halfExtent;
            _minZ = -halfExtent;

            int side = (int)Math.Ceiling(halfExtent * 2f / cellSize);

            _columns = side;
            _rows = side;

            _cells = new List<int>[_columns * _rows];

            for (int index = 0; index < _cells.Length; index++)
                _cells[index] = new List<int>();
        }

        public float CellSize => _cellSize;

        public int Columns => _columns;

        public int Rows => _rows;

        public int Count => _count;

        public void Clear()
        {
            for (int index = 0; index < _cells.Length; index++)
                _cells[index].Clear();

            _count = 0;
        }

        public void Add(int entity, Vector3 position)
        {
            int column = ResolveCell(position.x, _minX, _columns);
            int row = ResolveCell(position.z, _minZ, _rows);

            _cells[row * _columns + column].Add(entity);

            _count++;
        }

        public void Query(Vector3 center, float radius, float candidateSlack, List<int> result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (float.IsNaN(radius) || radius < 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(radius),
                    radius,
                    "Spatial grid query radius must be a non-negative number. " +
                    "A negative or NaN radius matches nothing, so the caller would silently see an empty neighbourhood.");

            if (float.IsNaN(candidateSlack) || candidateSlack < 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(candidateSlack),
                    candidateSlack,
                    "Spatial grid candidate slack must be a non-negative number. " +
                    "It is the largest distance a candidate may have moved since the last rebuild; " +
                    "pass zero when no position was written after the rebuild.");

            result.Clear();

            int rings = ResolveRings(radius + candidateSlack);

            int centerColumn = ResolveCell(center.x, _minX, _columns);
            int centerRow = ResolveCell(center.z, _minZ, _rows);

            int minColumn = Math.Max(centerColumn - rings, 0);
            int maxColumn = Math.Min(centerColumn + rings, _columns - 1);
            int minRow = Math.Max(centerRow - rings, 0);
            int maxRow = Math.Min(centerRow + rings, _rows - 1);

            float squaredRadius = radius * radius;

            for (int row = minRow; row <= maxRow; row++)
            {
                int rowOffset = row * _columns;

                for (int column = minColumn; column <= maxColumn; column++)
                {
                    List<int> cell = _cells[rowOffset + column];

                    for (int index = 0; index < cell.Count; index++)
                    {
                        int entity = cell[index];

                        ref Position position = ref _positions.Get(entity);

                        float deltaX = position.Value.x - center.x;
                        float deltaZ = position.Value.z - center.z;

                        if (deltaX * deltaX + deltaZ * deltaZ > squaredRadius)
                            continue;

                        result.Add(entity);
                    }
                }
            }
        }

        private int ResolveRings(float reach)
        {
            int widest = Math.Max(_columns, _rows);

            double rings = Math.Ceiling(reach / (double)_cellSize);

            if (rings < 1d)
                return 1;

            if (rings > widest)
                return widest;

            return (int)rings;
        }

        private int ResolveCell(float value, float origin, int count)
        {
            int index = (int)Math.Floor((value - origin) / _cellSize);

            if (index < 0)
                return 0;

            if (index >= count)
                return count - 1;

            return index;
        }
    }
}
