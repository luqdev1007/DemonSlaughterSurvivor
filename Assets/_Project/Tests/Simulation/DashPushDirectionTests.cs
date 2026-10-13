using Game.Simulation.Systems;
using NUnit.Framework;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class DashPushDirectionTests
    {
        private const float Tilt = 30f;
        private const float Spread = 15f;
        private const float Tolerance = 1e-4f;

        private const uint MiddleOfSpread = 0x40000000u;

        [Test]
        public void EnemyRightOfThePathFliesRightAndTiltedForward()
        {
            Vector3 direction = Resolve(Vector3.forward, new Vector3(1.2f, 0f, 3f), MiddleOfSpread, 0f, out float lateral);

            AssertDirection(new Vector3(Mathf.Sin(60f * Mathf.Deg2Rad), 0f, Mathf.Cos(60f * Mathf.Deg2Rad)), direction);
            Assert.AreEqual(1.2f, lateral, Tolerance, "the lateral distance is measured from the dash line, not from the hero");
        }

        [Test]
        public void EnemyLeftOfThePathFliesLeftAndTiltedForward()
        {
            Vector3 direction = Resolve(Vector3.forward, new Vector3(-0.5f, 0f, 1f), MiddleOfSpread, 0f, out float lateral);

            AssertDirection(new Vector3(-Mathf.Sin(60f * Mathf.Deg2Rad), 0f, Mathf.Cos(60f * Mathf.Deg2Rad)), direction);
            Assert.AreEqual(0.5f, lateral, Tolerance);
        }

        [Test]
        public void EnemyAheadOnTheLineIsSentAsideNotForward()
        {
            Vector3 right = Resolve(Vector3.forward, new Vector3(0f, 0f, 2f), 0x80000000u | MiddleOfSpread, 0f, out float lateral);
            Vector3 left = Resolve(Vector3.forward, new Vector3(0f, 0f, 2f), MiddleOfSpread, 0f, out _);

            Assert.Greater(right.x, 0.8f, "the hash bit picks the right side");
            Assert.Less(left.x, -0.8f, "the other hash bit picks the left side");
            Assert.AreEqual(0f, lateral, Tolerance, "an enemy on the line takes the full push");
        }

        [TestCase(0x00000000u, 45f)]
        [TestCase(0x7FFFFFFFu, 75f)]
        [TestCase(MiddleOfSpread, 60f)]
        public void SpreadRotatesTheAngleFromTheDashWithinItsBounds(uint hash, float expectedDegrees)
        {
            Vector3 direction = Resolve(Vector3.forward, new Vector3(1f, 0f, 0f), hash, Spread, out _);

            Assert.AreEqual(expectedDegrees, Vector3.Angle(Vector3.forward, direction), 1e-2f);
        }

        [Test]
        public void DiagonalDashNeverThrowsAnEnemyBackwardOrAcrossThePath()
        {
            Vector3 dash = new Vector3(1f, 0f, 1f).normalized;
            Vector3 right = new Vector3(dash.z, 0f, -dash.x);
            Vector3 offset = dash * 1.5f + right * 0.7f;

            for (int target = 1; target < 500; target++)
            {
                uint hash = SweepDashPushSystem.ResolvePairHash(0, target);
                Vector3 direction = SweepDashPushSystem.ResolvePushDirection(dash, dash * 0.4f, offset, hash, Tilt, Spread, out _);

                Assert.AreEqual(1f, direction.magnitude, Tolerance, "the direction is a unit vector");
                Assert.AreEqual(0f, direction.y, Tolerance);
                Assert.Greater(Vector3.Dot(direction, dash), 0f, $"target {target} must not fly backward against the dash");
                Assert.Greater(Vector3.Dot(direction, right), 0f, $"target {target} must stay on its own side of the path");
            }
        }

        [Test]
        public void SpreadVariesBetweenEnemies()
        {
            float smallest = 180f;
            float largest = 0f;

            for (int target = 1; target < 200; target++)
            {
                uint hash = SweepDashPushSystem.ResolvePairHash(0, target);
                Vector3 direction = SweepDashPushSystem.ResolvePushDirection(Vector3.forward, Vector3.forward, Vector3.right, hash, Tilt, Spread, out _);
                float angle = Vector3.Angle(Vector3.forward, direction);

                smallest = Mathf.Min(smallest, angle);
                largest = Mathf.Max(largest, angle);
            }

            Assert.Less(smallest, 48f);
            Assert.Greater(largest, 72f);
            Assert.GreaterOrEqual(smallest, 45f - Tolerance);
            Assert.LessOrEqual(largest, 75f + Tolerance);
        }

        [Test]
        public void ZeroDashDirectionFallsBackToTheStep()
        {
            Vector3 direction = SweepDashPushSystem.ResolvePushDirection(Vector3.zero, Vector3.right, new Vector3(1f, 0f, -1f), MiddleOfSpread, Tilt, 0f, out float lateral);

            Assert.Greater(direction.x, 0f, "tilted along the step");
            Assert.Less(direction.z, 0f, "towards the side the enemy stands on");
            Assert.AreEqual(1f, lateral, Tolerance);
        }

        private static Vector3 Resolve(Vector3 dash, Vector3 offset, uint hash, float spread, out float lateral)
        {
            return SweepDashPushSystem.ResolvePushDirection(dash, dash * 0.4f, offset, hash, Tilt, spread, out lateral);
        }

        private static void AssertDirection(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance);
            Assert.AreEqual(expected.y, actual.y, Tolerance);
            Assert.AreEqual(expected.z, actual.z, Tolerance);
        }
    }
}
