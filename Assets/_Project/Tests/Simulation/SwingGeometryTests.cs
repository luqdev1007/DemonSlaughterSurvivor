using Game.Configs;
using Game.Simulation.Services;
using NUnit.Framework;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class SwingGeometryTests
    {
        [Test]
        public void SegmentFullyBelowIsKeptWhole()
        {
            Assert.IsTrue(SwingGeometry.ClipBelow(new Vector3(0f, 0.5f, 0f), new Vector3(2f, 1f, 0f), 1.5f, out Vector2 a, out Vector2 b));
            Assert.AreEqual(new Vector2(0f, 0f), a);
            Assert.AreEqual(new Vector2(2f, 0f), b);
        }

        [Test]
        public void SegmentFullyAboveIsDropped()
        {
            Assert.IsFalse(SwingGeometry.ClipBelow(new Vector3(0f, 2f, 0f), new Vector3(2f, 3f, 0f), 1.5f, out _, out _));
        }

        [Test]
        public void SegmentCrossingTheHeightIsCutWhereItCrosses()
        {
            Assert.IsTrue(SwingGeometry.ClipBelow(new Vector3(0f, 0f, 0f), new Vector3(0f, 2f, 4f), 1f, out Vector2 a, out Vector2 b));
            Assert.AreEqual(0f, a.y, 1e-5f);
            Assert.AreEqual(2f, b.y, 1e-5f);
        }

        [Test]
        public void BandBetweenTwoBladePositionsHitsACircleInsideIt()
        {
            Vector2 origin = Vector2.zero;
            Vector2 fromTip = new Vector2(-1.5f, 1.5f);
            Vector2 toTip = new Vector2(1.5f, 1.5f);

            Assert.IsTrue(SwingGeometry.CircleHitsBand(new Vector2(0f, 1.2f), 0.1f, true, origin, fromTip, true, origin, toTip, 180f));
            Assert.IsFalse(SwingGeometry.CircleHitsBand(new Vector2(0f, 3f), 0.4f, true, origin, fromTip, true, origin, toTip, 180f));
        }

        [Test]
        public void HalfAngleCutsTheBladeBehindTheHero()
        {
            Vector2 origin = Vector2.zero;
            Vector2 fromTip = new Vector2(-1.5f, -1.5f);
            Vector2 toTip = new Vector2(1.5f, -1.5f);
            Vector2 behind = new Vector2(0f, -1.2f);

            Assert.IsFalse(SwingGeometry.CircleHitsBand(behind, 0.4f, true, origin, fromTip, true, origin, toTip, 90f));
            Assert.IsTrue(SwingGeometry.CircleHitsBand(behind, 0.4f, true, origin, fromTip, true, origin, toTip, 180f));
        }

        [Test]
        public void HeightCutRemovesAnOverheadPass()
        {
            SwingPose pose = new SwingPose(Vector3.zero, Vector3.forward);

            Vector3[] hands = { new Vector3(0f, 1.8f, 0.2f), new Vector3(0f, 1.8f, 0.2f) };
            Vector3[] tips = { new Vector3(-1.5f, 1.5f, 1.5f), new Vector3(1.5f, 1.5f, 1.5f) };
            Vector3 wolf = new Vector3(0f, 0f, 1.5f);

            Assert.IsFalse(SwingSweep.Hits(hands, tips, 2, pose, 90f, wolf, 0.4f, 1.0f));
            Assert.IsTrue(SwingSweep.Hits(hands, tips, 2, pose, 90f, wolf, 0.4f, 1.77f));
        }

        [Test]
        public void SubstepsCatchAFastTurnThatOneChordMisses()
        {
            SwingBake fine = RotatingBake(240f);
            SwingBake coarse = RotatingBake(60f);

            try
            {
                SwingPose pose = new SwingPose(Vector3.zero, Vector3.forward);
                Vector3 target = new Vector3(Mathf.Sin(49.5f * Mathf.Deg2Rad), 0f, Mathf.Cos(49.5f * Mathf.Deg2Rad)) * 1.8f;

                Vector3[] hands = new Vector3[16];
                Vector3[] tips = new Vector3[16];

                fine.SampleAt(0f, out Vector3 hand, out Vector3 tip);
                int fineCount = SwingSweep.BuildSegments(fine, 0f, 1f / 60f, 0f, 1f / 60f, hand, tip, pose, hands, tips);

                Assert.AreEqual(5, fineCount);
                Assert.IsTrue(SwingSweep.Hits(hands, tips, fineCount, pose, 180f, target, 0.4f, 1.77f));

                coarse.SampleAt(0f, out hand, out tip);
                int coarseCount = SwingSweep.BuildSegments(coarse, 0f, 1f / 60f, 0f, 1f / 60f, hand, tip, pose, hands, tips);

                Assert.AreEqual(2, coarseCount);
                Assert.IsFalse(SwingSweep.Hits(hands, tips, coarseCount, pose, 180f, target, 0.4f, 1.77f));
            }
            finally
            {
                Object.DestroyImmediate(fine);
                Object.DestroyImmediate(coarse);
            }
        }

        private static SwingBake RotatingBake(float sampleRate)
        {
            float length = 1f / 60f;
            int count = Mathf.FloorToInt(length * sampleRate + 1e-3f) + 1;

            Vector3[] hands = new Vector3[count];
            Vector3[] tips = new Vector3[count];

            for (int index = 0; index < count; index++)
            {
                float angle = 99f * (index / (float)(count - 1)) * Mathf.Deg2Rad;

                hands[index] = new Vector3(0f, 0.8f, 0f);
                tips[index] = new Vector3(Mathf.Sin(angle) * 2f, 0.8f, Mathf.Cos(angle) * 2f);
            }

            SwingBake bake = ScriptableObject.CreateInstance<SwingBake>();
            bake.Overwrite(null, sampleRate, length, 0u, hands, tips);

            return bake;
        }
    }
}
