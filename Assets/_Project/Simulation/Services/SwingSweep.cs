using Game.Configs;
using System;
using UnityEngine;

namespace Game.Simulation.Services
{
    public static class SwingSweep
    {
        public static int BuildSegments(
            SwingBake bake,
            float fromTime,
            float toTime,
            float windowStart,
            float windowEnd,
            Vector3 previousHand,
            Vector3 previousTip,
            in SwingPose pose,
            Vector3[] hands,
            Vector3[] tips)
        {
            float start = Math.Max(fromTime, windowStart);
            float end = Math.Min(toTime, windowEnd);

            if (start >= end)
                return 0;

            int steps = Math.Max(1, Mathf.CeilToInt((end - start) * bake.SampleRate - 1e-4f));

            if (steps + 1 > hands.Length || steps + 1 > tips.Length)
                throw new InvalidOperationException(
                    $"Swing segment buffer holds {hands.Length} segments, but one tick of '{bake.name}' needs {steps + 1}. " +
                    "The playback speed or the bake sample rate grew; enlarge the buffer where it is created.");

            if (start == fromTime)
            {
                hands[0] = previousHand;
                tips[0] = previousTip;
            }
            else
            {
                bake.SampleAt(start, out Vector3 hand, out Vector3 tip);

                hands[0] = pose.ToWorld(hand);
                tips[0] = pose.ToWorld(tip);
            }

            for (int step = 1; step <= steps; step++)
            {
                float time = start + (end - start) * step / steps;

                bake.SampleAt(time, out Vector3 hand, out Vector3 tip);

                hands[step] = pose.ToWorld(hand);
                tips[step] = pose.ToWorld(tip);
            }

            return steps + 1;
        }

        public static bool Hits(
            Vector3[] hands,
            Vector3[] tips,
            int count,
            in SwingPose pose,
            float halfAngleDegrees,
            Vector3 targetPosition,
            float targetRadius,
            float targetHeight)
        {
            if (count <= 0)
                return false;

            Vector3 target = pose.ToLocal(targetPosition);
            Vector2 center = new Vector2(target.x, target.z);

            bool hasFrom = SwingGeometry.ClipBelow(pose.ToLocal(hands[0]), pose.ToLocal(tips[0]), targetHeight, out Vector2 fromA, out Vector2 fromB);

            if (count == 1)
                return SwingGeometry.CircleHitsBand(center, targetRadius, hasFrom, fromA, fromB, false, default, default, halfAngleDegrees);

            for (int index = 1; index < count; index++)
            {
                bool hasTo = SwingGeometry.ClipBelow(pose.ToLocal(hands[index]), pose.ToLocal(tips[index]), targetHeight, out Vector2 toA, out Vector2 toB);

                if (SwingGeometry.CircleHitsBand(center, targetRadius, hasFrom, fromA, fromB, hasTo, toA, toB, halfAngleDegrees))
                    return true;

                hasFrom = hasTo;
                fromA = toA;
                fromB = toB;
            }

            return false;
        }
    }
}
