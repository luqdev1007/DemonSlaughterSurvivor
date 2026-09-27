using System;
using UnityEngine;

namespace Game.Simulation.Services
{
    public readonly struct SwingPose
    {
        public SwingPose(Vector3 position, Vector3 facing)
        {
            Vector3 flat = new Vector3(facing.x, 0f, facing.z);
            float length = flat.magnitude;

            Forward = length > 1e-6f ? flat / length : Vector3.forward;
            Right = new Vector3(Forward.z, 0f, -Forward.x);
            Position = position;
        }

        public Vector3 Position { get; }

        public Vector3 Forward { get; }

        public Vector3 Right { get; }

        public Vector3 ToWorld(Vector3 local)
        {
            return Position + Right * local.x + Vector3.up * local.y + Forward * local.z;
        }

        public Vector3 ToLocal(Vector3 world)
        {
            Vector3 offset = world - Position;

            return new Vector3(Vector3.Dot(offset, Right), offset.y, Vector3.Dot(offset, Forward));
        }
    }

    public static class SwingGeometry
    {
        public const float FullCircleHalfAngle = 180f;

        private const int MaxPolygon = 8;

        public static bool ClipBelow(Vector3 a, Vector3 b, float height, out Vector2 clippedA, out Vector2 clippedB)
        {
            bool aBelow = a.y <= height;
            bool bBelow = b.y <= height;

            if (aBelow == false && bBelow == false)
            {
                clippedA = default;
                clippedB = default;

                return false;
            }

            if (aBelow && bBelow)
            {
                clippedA = new Vector2(a.x, a.z);
                clippedB = new Vector2(b.x, b.z);

                return true;
            }

            float t = (height - a.y) / (b.y - a.y);
            Vector3 middle = a + (b - a) * t;

            if (aBelow)
            {
                clippedA = new Vector2(a.x, a.z);
                clippedB = new Vector2(middle.x, middle.z);

                return true;
            }

            clippedA = new Vector2(middle.x, middle.z);
            clippedB = new Vector2(b.x, b.z);

            return true;
        }

        public static bool CircleHitsBand(
            Vector2 center,
            float radius,
            bool hasFrom,
            Vector2 fromA,
            Vector2 fromB,
            bool hasTo,
            Vector2 toA,
            Vector2 toB,
            float halfAngleDegrees)
        {
            if (hasFrom && hasTo)
            {
                return CircleHitsTriangle(center, radius, fromA, fromB, toB, halfAngleDegrees)
                    || CircleHitsTriangle(center, radius, fromA, toB, toA, halfAngleDegrees)
                    || CircleHitsTriangle(center, radius, fromA, fromB, toA, halfAngleDegrees)
                    || CircleHitsTriangle(center, radius, fromB, toB, toA, halfAngleDegrees);
            }

            if (hasFrom)
                return CircleHitsTriangle(center, radius, fromA, fromB, fromB, halfAngleDegrees);

            if (hasTo)
                return CircleHitsTriangle(center, radius, toA, toB, toB, halfAngleDegrees);

            return false;
        }

        public static bool CircleHitsTriangle(Vector2 center, float radius, Vector2 a, Vector2 b, Vector2 c, float halfAngleDegrees)
        {
            Span<Vector2> polygon = stackalloc Vector2[MaxPolygon];
            Span<Vector2> scratch = stackalloc Vector2[MaxPolygon];

            polygon[0] = a;
            polygon[1] = b;
            polygon[2] = c;

            int count = 3;

            if (halfAngleDegrees < FullCircleHalfAngle)
            {
                float radians = halfAngleDegrees * Mathf.Deg2Rad;
                float sin = Mathf.Sin(radians);
                float cos = Mathf.Cos(radians);

                count = ClipHalfPlane(polygon, count, scratch, new Vector2(-cos, sin));
                count = ClipHalfPlane(polygon, count, scratch, new Vector2(cos, sin));

                if (count == 0)
                    return false;
            }

            return CircleHitsPolygon(center, radius, polygon, count);
        }

        public static bool CircleHitsPolygon(Vector2 center, float radius, ReadOnlySpan<Vector2> polygon, int count)
        {
            float squaredRadius = radius * radius;

            if (count == 1)
                return (polygon[0] - center).sqrMagnitude <= squaredRadius;

            bool anyPositive = false;
            bool anyNegative = false;

            for (int index = 0; index < count; index++)
            {
                Vector2 start = polygon[index];
                Vector2 end = polygon[(index + 1) % count];

                if (SquaredDistanceToSegment(center, start, end) <= squaredRadius)
                    return true;

                float cross = (end.x - start.x) * (center.y - start.y) - (end.y - start.y) * (center.x - start.x);

                if (cross > 0f)
                    anyPositive = true;

                if (cross < 0f)
                    anyNegative = true;
            }

            if (count < 3)
                return false;

            return anyPositive != anyNegative;
        }

        public static float SquaredDistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared <= 1e-12f)
                return (point - start).sqrMagnitude;

            float t = Vector2.Dot(point - start, segment) / lengthSquared;

            if (t < 0f)
                t = 0f;

            if (t > 1f)
                t = 1f;

            return (point - (start + segment * t)).sqrMagnitude;
        }

        private static int ClipHalfPlane(Span<Vector2> polygon, int count, Span<Vector2> scratch, Vector2 normal)
        {
            int written = 0;

            for (int index = 0; index < count; index++)
            {
                Vector2 start = polygon[index];
                Vector2 end = polygon[(index + 1) % count];

                float startSide = Vector2.Dot(normal, start);
                float endSide = Vector2.Dot(normal, end);

                if (endSide >= 0f)
                {
                    if (startSide < 0f)
                        scratch[written++] = start + (end - start) * (startSide / (startSide - endSide));

                    scratch[written++] = end;

                    continue;
                }

                if (startSide >= 0f)
                    scratch[written++] = start + (end - start) * (startSide / (startSide - endSide));
            }

            for (int index = 0; index < written; index++)
                polygon[index] = scratch[index];

            return written;
        }
    }
}
