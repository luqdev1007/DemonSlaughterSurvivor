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

        public void Query(Vector3 center, float radius, List<int> result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (radius > _cellSize)
                throw new InvalidOperationException(
                    $"Spatial grid query radius {radius} is greater than cell size {_cellSize}. " +
                    "The query walks one ring of cells around the center, which covers radius <= cellSize only; " +
                    "a wider radius would silently drop entities. " +
                    "Widen the walk to ceil(radius / cellSize) rings or raise the cell size in LevelConfig.");

            result.Clear();

            if (radius < 0f)
                return;

            int centerColumn = ResolveCell(center.x, _minX, _columns);
            int centerRow = ResolveCell(center.z, _minZ, _rows);

            int minColumn = Math.Max(centerColumn - 1, 0);
            int maxColumn = Math.Min(centerColumn + 1, _columns - 1);
            int minRow = Math.Max(centerRow - 1, 0);
            int maxRow = Math.Min(centerRow + 1, _rows - 1);

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
