using Game.Configs;
using Game.Simulation.Services;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class CounterSpinCoverageTests
    {
        private const float Tick = 1f / 60f;
        private const int Angles = 360;
        private const float DistanceStep = 0.05f;
        private const float FullCircleFrom = 0.80f;
        private const float FullCircleTo = 1.55f;
        private const float ReportTo = 2.30f;
        private const int SegmentCapacity = 64;

        [Test]
        public void SpinCoversTheFullCircleUpTo155ForEveryEnemy()
        {
            StringBuilder failures = new StringBuilder();

            foreach (CounterStrikeConfig counter in CounterStrikes())
            {
                foreach (EnemyConfig enemy in Enemies())
                {
                    for (float distance = FullCircleFrom; distance <= FullCircleTo + 1e-4f; distance += DistanceStep)
                    {
                        bool[] hit = Sweep(counter.Swing, distance, enemy.BodyRadius, enemy.BodyHeight);
                        int missed = CountMissed(hit);

                        if (missed > 0)
                            failures.Append($"{counter.name} / {enemy.Id} at {Format(distance)} m: {missed} of {Angles} angles missed, sectors {DescribeGaps(hit)}\n");
                    }
                }
            }

            Assert.IsEmpty(failures.ToString(), "Spin must hit every angle up to 1.55 m:\n" + failures);
        }

        [Test]
        public void SpinSectorsBeyond155AreReported()
        {
            StringBuilder report = new StringBuilder();

            foreach (CounterStrikeConfig counter in CounterStrikes())
            {
                foreach (EnemyConfig enemy in Enemies())
                {
                    for (float distance = FullCircleTo + DistanceStep; distance <= ReportTo + 1e-4f; distance += DistanceStep)
                    {
                        bool[] hit = Sweep(counter.Swing, distance, enemy.BodyRadius, enemy.BodyHeight);

                        report.Append($"{counter.name} / {enemy.Id} at {Format(distance)} m: missed {CountMissed(hit)} of {Angles}, gaps {DescribeGaps(hit)}\n");
                    }
                }
            }

            TestContext.WriteLine(report.ToString());
            Assert.Pass(report.ToString());
        }

        private static bool[] Sweep(SwingVariant variant, float distance, float radius, float height)
        {
            SwingBake bake = variant.Bake;
            int ticks = Mathf.Max(1, Mathf.RoundToInt(bake.ClipLength / variant.PlaybackSpeed / Tick));
            float speed = bake.ClipLength / (ticks * Tick);

            SwingPose pose = new SwingPose(Vector3.zero, Vector3.forward);
            Vector3[] hands = new Vector3[SegmentCapacity];
            Vector3[] tips = new Vector3[SegmentCapacity];
            bool[] hit = new bool[Angles];

            for (int angle = 0; angle < Angles; angle++)
            {
                float radians = angle * Mathf.Deg2Rad;
                Vector3 target = new Vector3(Mathf.Sin(radians) * distance, 0f, Mathf.Cos(radians) * distance);

                bake.SampleAt(0f, out Vector3 previousHand, out Vector3 previousTip);
                previousHand = pose.ToWorld(previousHand);
                previousTip = pose.ToWorld(previousTip);

                float from = 0f;

                for (int tick = 0; tick < ticks && hit[angle] == false; tick++)
                {
                    float to = tick == ticks - 1 ? bake.ClipLength : Mathf.Min(from + Tick * speed, bake.ClipLength);

                    int count = SwingSweep.BuildSegments(bake, from, to, variant.WindowStart, variant.WindowEnd, previousHand, previousTip, pose, hands, tips);

                    if (count > 0 && SwingSweep.Hits(hands, tips, count, pose, variant.HalfAngleDegrees, target, radius, height))
                        hit[angle] = true;

                    bake.SampleAt(to, out previousHand, out previousTip);
                    previousHand = pose.ToWorld(previousHand);
                    previousTip = pose.ToWorld(previousTip);
                    from = to;
                }
            }

            return hit;
        }

        private static int CountMissed(bool[] hit)
        {
            int missed = 0;

            for (int angle = 0; angle < hit.Length; angle++)
            {
                if (hit[angle] == false)
                    missed++;
            }

            return missed;
        }

        private static string DescribeGaps(bool[] hit)
        {
            StringBuilder text = new StringBuilder();
            int start = -1;

            for (int index = 0; index <= Angles; index++)
            {
                bool missed = index < Angles && hit[index] == false;

                if (missed && start < 0)
                    start = index;

                if (missed == false && start >= 0)
                {
                    text.Append($"[{Signed(start)}..{Signed(index - 1)}] ");
                    start = -1;
                }
            }

            return text.Length == 0 ? "none" : text.ToString().TrimEnd();
        }

        private static int Signed(int angle)
        {
            return angle > 180 ? angle - 360 : angle;
        }

        private static string Format(float value)
        {
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }

        private static IEnumerable<CounterStrikeConfig> CounterStrikes()
        {
            int count = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CounterStrikeConfig)))
            {
                count++;

                yield return AssetDatabase.LoadAssetAtPath<CounterStrikeConfig>(AssetDatabase.GUIDToAssetPath(guid));
            }

            Assert.Greater(count, 0, "no counter strike config to check");
        }

        private static IEnumerable<EnemyConfig> Enemies()
        {
            int count = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(EnemyConfig)))
            {
                count++;

                yield return AssetDatabase.LoadAssetAtPath<EnemyConfig>(AssetDatabase.GUIDToAssetPath(guid));
            }

            Assert.Greater(count, 0, "no enemy config to check");
        }
    }
}
