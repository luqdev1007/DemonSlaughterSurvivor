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
    }
}
