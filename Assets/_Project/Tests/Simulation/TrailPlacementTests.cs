using Game.Simulation.Services;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class TrailPlacementTests
    {
        private const float Spacing = 0.6f;

        [Test]
        public void AcceleratingDashOf8MetresGets13EvenlySpacedPoints()
        {
            float[] steps = { 0.05f, 0.12f, 0.2f, 0.3f, 0.42f, 0.55f, 0.66f, 0.74f, 0.8f, 0.83f, 0.84f, 0.84f, 0.84f, 0.81f };
            float total = 0f;

            foreach (float step in steps)
                total += step;

            List<Vector3> points = Walk(StraightPath(steps, Vector3.right));

            Assert.AreEqual(8f, total, 1e-4f, "the fixture path is 8 m long");
            Assert.AreEqual(13, points.Count, "8 m / 0.6 m");
            Assert.AreEqual(Spacing, points[0].x, 1e-4f, "the first point is one spacing from the start");

            for (int index = 1; index < points.Count; index++)
                Assert.AreEqual(Spacing, points[index].x - points[index - 1].x, 1e-4f, $"gap {index}");
        }

        [Test]
        public void RemainderIsCarriedBetweenTicks()
        {
            float carried = 0f;
            Vector3[] buffer = new Vector3[8];

            int first = TrailPlacement.Place(Vector3.zero, new Vector3(0.5f, 0f, 0f), Spacing, ref carried, buffer);

            Assert.AreEqual(0, first);
            Assert.AreEqual(0.5f, carried, 1e-5f);

            int second = TrailPlacement.Place(new Vector3(0.5f, 0f, 0f), new Vector3(1f, 0f, 0f), Spacing, ref carried, buffer);

            Assert.AreEqual(1, second);
            Assert.AreEqual(0.6f, buffer[0].x, 1e-5f);
            Assert.AreEqual(0.4f, carried, 1e-5f);
        }

        [Test]
        public void BentPathKeepsTheSpacingAlongThePath()
        {
            List<Vector3[]> path = new List<Vector3[]>
            {
                new[] { Vector3.zero, new Vector3(1f, 0f, 0f) },
                new[] { new Vector3(1f, 0f, 0f), new Vector3(1f, 0f, 0.9f) },
                new[] { new Vector3(1f, 0f, 0.9f), new Vector3(1.33f, 0f, 1.34f) },
            };

            List<Vector3> points = Walk(path);
            float travelled = 0f;
            int next = 0;
            float expected = Spacing;
            List<float> along = new List<float>();

            foreach (Vector3[] leg in path)
            {
                float length = Vector3.Distance(leg[0], leg[1]);

                while (next < points.Count && Vector3.Distance(leg[0], points[next]) <= length + 1e-4f && OnLeg(leg, points[next]))
                {
                    along.Add(travelled + Vector3.Distance(leg[0], points[next]));
                    next++;
                }

                travelled += length;
            }

            Assert.AreEqual(points.Count, along.Count);
            Assert.AreEqual(Mathf.FloorToInt(travelled / Spacing), points.Count);

            foreach (float distance in along)
            {
                Assert.AreEqual(expected, distance, 1e-4f);
                expected += Spacing;
            }
        }

        private static bool OnLeg(Vector3[] leg, Vector3 point)
        {
            float direct = Vector3.Distance(leg[0], leg[1]);
            float through = Vector3.Distance(leg[0], point) + Vector3.Distance(point, leg[1]);

            return Mathf.Abs(through - direct) < 1e-4f;
        }

        private static List<Vector3[]> StraightPath(float[] steps, Vector3 direction)
        {
            List<Vector3[]> path = new List<Vector3[]>();
            Vector3 at = Vector3.zero;

            foreach (float step in steps)
            {
                Vector3 next = at + direction * step;
                path.Add(new[] { at, next });
                at = next;
            }

            return path;
        }

        private static List<Vector3> Walk(List<Vector3[]> path)
        {
            float carried = 0f;
            Vector3[] buffer = new Vector3[8];
            List<Vector3> points = new List<Vector3>();

            foreach (Vector3[] leg in path)
            {
                int count = TrailPlacement.Place(leg[0], leg[1], Spacing, ref carried, buffer);

                for (int index = 0; index < count; index++)
                    points.Add(buffer[index]);
            }

            return points;
        }
    }
}
