using Game.Simulation.Services;
using Leopotam.EcsLite;
using NUnit.Framework;
using System;

namespace Game.Simulation.Tests
{
    public sealed class SpatialGridConstructionTests
    {
        [Test]
        public void RejectsNonPositiveCellSize()
        {
            EcsWorld world = new EcsWorld();

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialGrid(world, 10f, 0f));
            }
            finally
            {
                world.Destroy();
            }
        }

        [Test]
        public void CoversArenaWithMarginCells()
        {
            EcsWorld world = new EcsWorld();

            try
            {
                SpatialGrid grid = new SpatialGrid(world, 45f, 1f);

                Assert.AreEqual(94, grid.Columns);
                Assert.AreEqual(94, grid.Rows);
            }
            finally
            {
                world.Destroy();
            }
        }
    }
}
