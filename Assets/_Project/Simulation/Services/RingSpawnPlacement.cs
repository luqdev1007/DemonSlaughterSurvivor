using UnityEngine;

namespace Game.Simulation.Services
{
    public sealed class RingSpawnPlacement
    {
        public Vector3 Resolve(Vector3 center, float ringRadius, float arenaRadius, float angleUnit)
        {
            float angle = angleUnit * Mathf.PI * 2f;

            float x = center.x + Mathf.Cos(angle) * ringRadius;
            float z = center.z + Mathf.Sin(angle) * ringRadius;

            Vector3 point = new Vector3(x, 0f, z);

            if (arenaRadius <= 0f)
                return point;

            float distance = point.magnitude;

            if (distance <= arenaRadius)
                return point;

            return point * (arenaRadius / distance);
        }

        public Vector3 Resolve(Vector3 center, float ringRadius, float arenaRadius, float angleUnit, float minDistance)
        {
            Vector3 point = Resolve(center, ringRadius, arenaRadius, angleUnit);

            if (IsFarEnough(center, point, minDistance))
                return point;

            Vector3 inward = new Vector3(-center.x, 0f, -center.z);

            if (inward.sqrMagnitude <= Mathf.Epsilon)
                inward = new Vector3(Mathf.Cos(angleUnit * Mathf.PI * 2f), 0f, Mathf.Sin(angleUnit * Mathf.PI * 2f));

            Vector3 mirrored = center + inward.normalized * ringRadius;
            mirrored.y = 0f;

            if (arenaRadius > 0f && mirrored.magnitude > arenaRadius)
                mirrored *= arenaRadius / mirrored.magnitude;

            return mirrored;
        }

        private static bool IsFarEnough(Vector3 center, Vector3 point, float minDistance)
        {
            float deltaX = point.x - center.x;
            float deltaZ = point.z - center.z;

            return deltaX * deltaX + deltaZ * deltaZ >= minDistance * minDistance;
        }
    }
}
